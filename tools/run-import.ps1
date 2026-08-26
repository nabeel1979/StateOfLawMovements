$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ssh-lib.ps1')

$sqlLocal  = Join-Path $PSScriptRoot 'members-import.sql'
$staged    = Join-Path $env:TEMP 'members-import.sql'
$remoteDir = 'C:\qanoon_import'
$remoteSql = "$remoteDir\members-import.sql"

# Stage under an ASCII path: SFTP transfers from the Arabic project path have
# failed before.
Copy-Item -LiteralPath $sqlLocal -Destination $staged -Force

Import-Module Posh-SSH -ErrorAction Stop
$sec  = ConvertTo-SecureString $SshPass -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($SshUser, $sec)

$ssh = New-SSHSession -ComputerName $SshHost -Port $SshPort -Credential $cred -AcceptKey -Force
$sftp = New-SFTPSession -ComputerName $SshHost -Port $SshPort -Credential $cred -AcceptKey -Force

try {
    Invoke-SSHCommand -SessionId $ssh.SessionId `
        -Command "powershell -NoProfile -Command `"New-Item -ItemType Directory -Force -Path '$remoteDir' | Out-Null; 'ok'`"" | ForEach-Object { $_.Output }

    Set-SFTPItem -SessionId $sftp.SessionId -Path $staged -Destination '/C:/qanoon_import/' -Force
    Write-Output 'uploaded'

    $body = @"
try {
    `$sqlText = [System.IO.File]::ReadAllText('$remoteSql', [System.Text.Encoding]::UTF8)
    Say "SQL LENGTH: `$(`$sqlText.Length)"

    `$cn = Get-Conn

    `$before = Invoke-Query `$cn "SELECT COUNT(*) AS C FROM Members WHERE MovementId = 1"
    Say "MEMBERS BEFORE: `$(`$before.Rows[0].Item('C'))"

    `$cmd = `$cn.CreateCommand()
    `$cmd.CommandText = `$sqlText
    `$cmd.CommandTimeout = 300
    `$affected = `$cmd.ExecuteNonQuery()
    Say "ROWS AFFECTED: `$affected"

    `$after = Invoke-Query `$cn "SELECT COUNT(*) AS C FROM Members WHERE MovementId = 1"
    Say "MEMBERS AFTER: `$(`$after.Rows[0].Item('C'))"

    `$sample = Invoke-Query `$cn "SELECT TOP 6 Id, SerialNumber, FullName, Phone, CreatedAt FROM Members WHERE MovementId = 1 ORDER BY CreatedAt ASC"
    Say "FIRST BY CreatedAt:"
    foreach (`$r in `$sample.Rows) {
        Say ("   #" + `$r.Item('Id') + " | " + `$r.Item('SerialNumber') + " | " + `$r.Item('Phone') + " | " + `$r.Item('CreatedAt') + " | " + `$r.Item('FullName'))
    }

    `$cn.Close()
}
catch {
    Say "SQL ERROR: `$(`$_.Exception.Message)"
}
Emit
"@

    $out = Invoke-Remote -SessionId $ssh.SessionId -Script ((Get-RemotePrelude) + "`n" + $body) -TimeoutSec 600
    $out | Out-File -LiteralPath (Join-Path $PSScriptRoot 'run-import.txt') -Encoding UTF8
    Write-Output 'wrote run-import.txt'

    Invoke-SSHCommand -SessionId $ssh.SessionId `
        -Command "powershell -NoProfile -Command `"Remove-Item -Recurse -Force '$remoteDir' -ErrorAction SilentlyContinue; 'cleaned'`"" | ForEach-Object { $_.Output }
}
finally {
    Remove-SFTPSession -SessionId $sftp.SessionId | Out-Null
    Remove-SSHSession  -SessionId $ssh.SessionId  | Out-Null
    Remove-Item -LiteralPath $staged -Force -ErrorAction SilentlyContinue
}

