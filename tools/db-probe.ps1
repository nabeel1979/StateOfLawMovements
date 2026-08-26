$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ssh-lib.ps1')

$body = @'
try {
    $cn = Get-Conn
    Say "CONNECTED"

    $cols = Invoke-Query $cn "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Members' ORDER BY ORDINAL_POSITION"
    Say "MEMBERS COLUMNS ($($cols.Rows.Count)):"
    foreach ($r in $cols.Rows) {
        Say ("   " + $r.Item('COLUMN_NAME') + " : " + $r.Item('DATA_TYPE') + "(" + $r.Item('CHARACTER_MAXIMUM_LENGTH') + ") null=" + $r.Item('IS_NULLABLE'))
    }

    $dt = Invoke-Query $cn "SELECT Id, Name, IsActive FROM Movements ORDER BY Id"
    Say "MOVEMENTS ($($dt.Rows.Count)):"
    foreach ($r in $dt.Rows) {
        Say ("   Id=" + $r.Item('Id') + " | Active=" + $r.Item('IsActive') + " | " + $r.Item('Name'))
    }

    $dt2 = Invoke-Query $cn "SELECT COUNT(*) AS C FROM Members"
    Say "TOTAL MEMBERS: $($dt2.Rows[0]['C'])"

    $dt3 = Invoke-Query $cn "SELECT MovementId, COUNT(*) AS C FROM Members GROUP BY MovementId"
    Say "MEMBERS PER MOVEMENT:"
    foreach ($r in $dt3.Rows) { Say ("   MovementId=" + $r.Item('MovementId') + " -> " + $r.Item('C')) }

    $dt4 = Invoke-Query $cn "SELECT Id, SerialNumber, FullName, Phone, MovementId FROM Members ORDER BY Id"
    Say "EXISTING MEMBERS:"
    foreach ($r in $dt4.Rows) {
        Say ("   #" + $r.Item('Id') + " | " + $r.Item('SerialNumber') + " | " + $r.Item('Phone') + " | mov=" + $r.Item('MovementId') + " | " + $r.Item('FullName'))
    }

    $dt5 = Invoke-Query $cn "SELECT Id, FullName, Role FROM Users ORDER BY Id"
    Say "USERS:"
    foreach ($r in $dt5.Rows) { Say ("   #" + $r.Item('Id') + " | Role=" + $r.Item('Role') + " | " + $r.Item('FullName')) }

    $cn.Close()
}
catch {
    Say "ERROR: $($_.Exception.Message)"
}
Emit
'@

$s = New-Session
try {
    $out = Invoke-Remote -SessionId $s.SessionId -Script ((Get-RemotePrelude) + "`n" + $body) -TimeoutSec 240
    $out | Out-File -LiteralPath (Join-Path $PSScriptRoot 'db-probe.txt') -Encoding UTF8
    Write-Output "wrote db-probe.txt"
}
finally {
    Remove-SSHSession -SessionId $s.SessionId | Out-Null
}

