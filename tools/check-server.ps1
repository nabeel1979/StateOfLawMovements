$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ssh-lib.ps1')

$body = @'
Import-Module WebAdministration
$sites = @(
    'E:\all site\StateOfLawMovements\Backend_StateOfLawMovements',
    'E:\all site\StateOfLawMovements\Frontend_StateOfLawMovements'
)
foreach ($s in $sites) {
    $p = Join-Path $s 'QanoonCoalition.Web.dll'
    if (Test-Path -LiteralPath $p) {
        $f = Get-Item -LiteralPath $p
        Write-Output ("DLL " + $s + " -> " + $f.Length + " bytes, " + $f.LastWriteTimeUtc.ToString('yyyy-MM-dd HH:mm:ss') + " UTC")
    } else {
        Write-Output ("DLL MISSING: " + $p)
    }
}
foreach ($pool in @('Backend_StateOfLawMovements','Frontend_StateOfLawMovements')) {
    Write-Output ("POOL " + $pool + " -> " + (Get-WebAppPoolState -Name $pool).Value)
}
Write-Output ("STAGING EXISTS: " + (Test-Path 'C:\qanoon_deploy'))
'@

$s = New-Session
try {
    Invoke-Remote -SessionId $s.SessionId -Script $body -TimeoutSec 180
}
finally { Remove-SSHSession -SessionId $s.SessionId | Out-Null }
