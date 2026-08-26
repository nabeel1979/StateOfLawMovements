$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ssh-lib.ps1')

$projectRoot = Split-Path -Parent $PSScriptRoot
$srcRoot     = Join-Path $projectRoot 'src\QanoonCoalition.Web'

# اسم الملف على السيرفر -> مساره المحلي، ومساره النسبي داخل مجلد الموقع
$payload = @(
    @{ Name = 'QanoonCoalition.Web.dll'; Local = Join-Path $srcRoot 'bin\Release\net8.0\QanoonCoalition.Web.dll'; Relative = 'QanoonCoalition.Web.dll' },
    @{ Name = 'app.css';                 Local = Join-Path $srcRoot 'wwwroot\css\app.css';                       Relative = 'wwwroot\css\app.css' }
)

# Stage under an ASCII path; SFTP from the Arabic project path is unreliable.
$stageLocal = Join-Path $env:TEMP 'qanoon_stage'
New-Item -ItemType Directory -Force -Path $stageLocal | Out-Null

foreach ($item in $payload) {
    if (-not (Test-Path -LiteralPath $item.Local)) { throw "missing: $($item.Local)" }
    $f = Get-Item -LiteralPath $item.Local
    Write-Output ("LOCAL {0}: {1} bytes, {2} UTC" -f $item.Name, $f.Length, $f.LastWriteTimeUtc.ToString('yyyy-MM-dd HH:mm:ss'))
    Copy-Item -LiteralPath $item.Local -Destination (Join-Path $stageLocal $item.Name) -Force
}

$manifest = ($payload | ForEach-Object { "'$($_.Name)|$($_.Relative)'" }) -join ','

Import-Module Posh-SSH -ErrorAction Stop
$sec  = ConvertTo-SecureString $SshPass -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($SshUser, $sec)

$ssh  = New-SSHSession  -ComputerName $SshHost -Port $SshPort -Credential $cred -AcceptKey -Force
$sftp = New-SFTPSession -ComputerName $SshHost -Port $SshPort -Credential $cred -AcceptKey -Force

# Remote work goes through base64-encoded scripts: nested quoting in a plain
# `powershell -Command "..."` over SSH silently produced no output.
$header = @"
`$ErrorActionPreference = 'Stop'
`$ProgressPreference = 'SilentlyContinue'
Import-Module WebAdministration
`$pools = @('Backend_StateOfLawMovements','Frontend_StateOfLawMovements')
`$sites = @(
    'E:\all site\StateOfLawMovements\Backend_StateOfLawMovements',
    'E:\all site\StateOfLawMovements\Frontend_StateOfLawMovements'
)
`$stageDir = 'C:\qanoon_deploy'
`$payload = @($manifest)
"@

function Step([string]$label, [string]$script, [int]$timeout = 240) {
    Write-Output "--- $label ---"
    Invoke-Remote -SessionId $ssh.SessionId -Script ($header + "`n" + $script) -TimeoutSec $timeout
}

try {
    Step 'prepare staging' @'
New-Item -ItemType Directory -Force -Path 'C:\qanoon_deploy' | Out-Null
Write-Output 'staging ready'
'@

    Write-Output '--- uploading files ---'
    foreach ($item in $payload) {
        Set-SFTPItem -SessionId $sftp.SessionId -Path (Join-Path $stageLocal $item.Name) `
            -Destination '/C:/qanoon_deploy/' -Force
        Write-Output ("sent " + $item.Name)
    }

    Step 'verify upload' @'
foreach ($entry in $payload) {
    $name = $entry.Split('|')[0]
    $f = Get-Item -LiteralPath (Join-Path $stageDir $name)
    Write-Output ("uploaded " + $name + ": " + $f.Length + " bytes, " + $f.LastWriteTimeUtc.ToString('yyyy-MM-dd HH:mm:ss') + " UTC")
}
'@

    Step 'stop app pools' @'
foreach ($p in $pools) {
    $st = (Get-WebAppPoolState -Name $p).Value
    if ($st -eq 'Started' -or $st -eq 'Starting') { Stop-WebAppPool -Name $p -ErrorAction SilentlyContinue }
}

$deadline = (Get-Date).AddSeconds(45)
while ((Get-Date) -lt $deadline) {
    $pending = $pools | Where-Object { (Get-WebAppPoolState -Name $_).Value -ne 'Stopped' }
    if (-not $pending) { break }
    Start-Sleep -Seconds 2
}

# A pool stuck in Stopping keeps the assembly locked, which used to make the
# copy fail and leave that site down. Kill its worker process instead.
$pending = $pools | Where-Object { (Get-WebAppPoolState -Name $_).Value -ne 'Stopped' }
if ($pending) {
    Write-Output ("forcing worker processes for: " + ($pending -join ', '))
    foreach ($p in $pending) {
        Get-ChildItem "IIS:\AppPools\$p\WorkerProcesses" -ErrorAction SilentlyContinue | ForEach-Object {
            Stop-Process -Id $_.processId -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Seconds 4
}
foreach ($p in $pools) { Write-Output ($p + " -> " + (Get-WebAppPoolState -Name $p).Value) }
Start-Sleep -Seconds 2
'@

    Step 'copy files into sites' @'
$failed = @()
foreach ($s in $sites) {
    foreach ($entry in $payload) {
        $parts = $entry.Split('|')
        $src  = Join-Path $stageDir $parts[0]
        $dest = Join-Path $s $parts[1]
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null

        $ok = $false
        for ($i = 0; $i -lt 6 -and -not $ok; $i++) {
            try { Copy-Item -LiteralPath $src -Destination $dest -Force; $ok = $true }
            catch { Start-Sleep -Seconds 3 }
        }
        if (-not $ok) { $failed += ($parts[1] + " @ " + $s) }

        $f = Get-Item -LiteralPath $dest
        Write-Output ($parts[1] + " @ " + $s + " -> copied=" + $ok + ", " + $f.Length + " bytes, " + $f.LastWriteTimeUtc.ToString('yyyy-MM-dd HH:mm:ss') + " UTC")
    }
}
if ($failed) { Write-Output ("COPY FAILED: " + ($failed -join '; ')) }
'@

    # Pools must come back up even if a copy failed, otherwise the site stays down.
    Step 'start app pools' @'
$deadline = (Get-Date).AddSeconds(60)
while ((Get-Date) -lt $deadline) {
    $notUp = $pools | Where-Object { (Get-WebAppPoolState -Name $_).Value -ne 'Started' }
    if (-not $notUp) { break }
    foreach ($p in $notUp) { Start-WebAppPool -Name $p -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 3
}
foreach ($p in $pools) { Write-Output ($p + " -> " + (Get-WebAppPoolState -Name $p).Value) }
'@

    Step 'cleanup' @'
Remove-Item -Recurse -Force 'C:\qanoon_deploy' -ErrorAction SilentlyContinue
Write-Output 'cleaned'
'@
}
finally {
    Remove-SFTPSession -SessionId $sftp.SessionId | Out-Null
    Remove-SSHSession  -SessionId $ssh.SessionId  | Out-Null
    Remove-Item -LiteralPath $stageLocal -Recurse -Force -ErrorAction SilentlyContinue
}
