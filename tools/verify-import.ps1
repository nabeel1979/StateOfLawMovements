$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ssh-lib.ps1')

$body = @'
try {
    $cn = Get-Conn

    $dt = Invoke-Query $cn @"
SELECT Id, SerialNumber, FullName, Phone, BirthDate, Gender, Province, District,
       EducationLevel, Specialization, Occupation, JobTitle, BenefitField, CreatedAt
FROM Members
WHERE MovementId = 1 AND CreatedAt >= '2026-08-26T05:00:00'
ORDER BY CreatedAt ASC
"@
    Say "IMPORTED ROWS: $($dt.Rows.Count)"
    foreach ($r in $dt.Rows) {
        $b = if ($r.Item('BirthDate') -is [DBNull]) { '-' } else { ([datetime]$r.Item('BirthDate')).ToString('yyyy-MM-dd') }
        Say ("#" + $r.Item('Id') + " | " + $r.Item('SerialNumber') + " | " + $r.Item('Phone') +
             " | b=" + $b + " | g=" + $r.Item('Gender') +
             " | " + $r.Item('Province') + "/" + $r.Item('District') +
             " | " + $r.Item('EducationLevel') + " | " + $r.Item('BenefitField') +
             " | " + $r.Item('FullName'))
    }

    $nulls = Invoke-Query $cn @"
SELECT
  SUM(CASE WHEN BirthDate      IS NULL THEN 1 ELSE 0 END) AS NoBirth,
  SUM(CASE WHEN Gender         IS NULL THEN 1 ELSE 0 END) AS NoGender,
  SUM(CASE WHEN Province       IS NULL THEN 1 ELSE 0 END) AS NoProvince,
  SUM(CASE WHEN EducationLevel IS NULL THEN 1 ELSE 0 END) AS NoEdu,
  SUM(CASE WHEN BenefitField   IS NULL THEN 1 ELSE 0 END) AS NoBenefit
FROM Members WHERE MovementId = 1 AND CreatedAt >= '2026-08-26T05:00:00'
"@
    $n = $nulls.Rows[0]
    Say ""
    Say ("NULL COUNTS -> birth=" + $n.Item('NoBirth') + " gender=" + $n.Item('NoGender') +
         " province=" + $n.Item('NoProvince') + " edu=" + $n.Item('NoEdu') + " benefit=" + $n.Item('NoBenefit'))

    $tot = Invoke-Query $cn "SELECT COUNT(*) AS C FROM Members WHERE MovementId = 1"
    Say ("TOTAL IN MOVEMENT 1: " + $tot.Rows[0].Item('C'))

    $cn.Close()
}
catch { Say "ERROR: $($_.Exception.Message)" }
Emit
'@

$s = New-Session
try {
    $out = Invoke-Remote -SessionId $s.SessionId -Script ((Get-RemotePrelude) + "`n" + $body) -TimeoutSec 300
    $out | Out-File -LiteralPath (Join-Path $PSScriptRoot 'verify-import.txt') -Encoding UTF8
    Write-Output 'wrote verify-import.txt'
}
finally { Remove-SSHSession -SessionId $s.SessionId | Out-Null }

