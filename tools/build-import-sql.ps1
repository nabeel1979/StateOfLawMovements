$ErrorActionPreference = 'Stop'

# Builds the INSERT script for the members sheet. Emits members-import.sql plus
# a human-readable report of what was skipped and why.

$MovementId    = 1                                  # حركة القرار الوطني
$ExistingSerials = @('64352555','27496443','41799123','72200241')
$BaseCreatedUtc  = [datetime]::ParseExact('2026-08-26 05:00:00', 'yyyy-MM-dd HH:mm:ss', $null)

$csv      = Join-Path $PSScriptRoot 'members_raw.csv'
$sqlOut   = Join-Path $PSScriptRoot 'members-import.sql'
$reportOut= Join-Path $PSScriptRoot 'import-report.txt'

function Q([string]$s) {
    if ($null -eq $s) { return 'NULL' }
    $t = $s.Trim()
    if ($t.Length -eq 0) { return 'NULL' }
    # Cells filled with only punctuation are placeholders, not data.
    if ($t -match '^[\/\.\-_\\\s]+$') { return 'NULL' }
    return "N'" + ($t -replace "'", "''") + "'"
}

function Cut([string]$s, [int]$max) {
    if ([string]::IsNullOrWhiteSpace($s)) { return $s }
    $t = $s.Trim()
    if ($t.Length -le $max) { return $t }
    return $t.Substring(0, $max)
}

function Clean-Phone([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return '' }
    $d = ($s -replace '[^\d]', '')
    if ($d.StartsWith('964')) { $d = '0' + $d.Substring(3) }
    if ($d.Length -eq 10 -and $d.StartsWith('7')) { $d = '0' + $d }
    return $d
}

function Parse-Date([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return $null }
    $t = $s.Trim()
    if ($t -match '^[\/\.\-_\\\s]+$') { return $null }
    $formats = @('yyyy-MM-dd','dd/MM/yyyy','d/M/yyyy','dd/M/yyyy','d/MM/yyyy','MM/dd/yyyy')
    $d = [datetime]::MinValue
    foreach ($f in $formats) {
        if ([datetime]::TryParseExact($t, $f, [System.Globalization.CultureInfo]::InvariantCulture, 'None', [ref]$d)) {
            return $d
        }
    }
    return $null
}

$rows = Import-Csv -LiteralPath $csv -Encoding UTF8 |
        Where-Object { [int]$_.Row -ge 9 -and -not [string]::IsNullOrWhiteSpace($_.C3) }

$report = New-Object System.Collections.Generic.List[string]
$stmts  = New-Object System.Collections.Generic.List[string]

$usedSerials = New-Object System.Collections.Generic.HashSet[string]
foreach ($s in $ExistingSerials) { $usedSerials.Add($s) | Out-Null }

$rnd = New-Object System.Random 20260826
function New-Serial {
    do { $s = $rnd.Next(10000000, 99999999).ToString() }
    while ($usedSerials.Contains($s))
    $usedSerials.Add($s) | Out-Null
    return $s
}

$i = 0
$skipped = 0

foreach ($r in $rows) {
    $excelRow = $r.Row
    $name  = Cut $r.C3 200
    $phone = Clean-Phone $r.C7

    if ($phone.Length -eq 0) {
        $report.Add("SKIPPED row $excelRow - no phone number - '$name'")
        $skipped++
        continue
    }
    if ($phone.Length -ne 11 -or -not $phone.StartsWith('07')) {
        $report.Add("WARNING row $excelRow - phone '$phone' is not 11 digits starting with 07 - imported as-is - '$name'")
    }

    # Arabic literals are compared by code point: Windows PowerShell reads this
    # file as ANSI, which would corrupt inline Arabic strings.
    $g   = $r.C4.Trim()
    $DAL = [char]0x0630   # ذ
    $KAF = [char]0x0643   # ك
    $NUN = [char]0x0646   # ن
    $THA = [char]0x062B   # ث

    $gender = $null
    if ($g.Contains($DAL) -and $g.Contains($KAF))      { $gender = 1 }   # ذكر
    elseif ($g.Contains($NUN) -and $g.Contains($THA))  { $gender = 2 }   # أنثى
    if ($null -eq $gender -and -not [string]::IsNullOrWhiteSpace($r.C4)) {
        $report.Add("WARNING row $excelRow - unrecognised gender '$($r.C4)' - stored as NULL")
    }

    $birth = Parse-Date $r.C5
    if ($null -eq $birth) {
        $report.Add("WARNING row $excelRow - birth date '$($r.C5)' could not be parsed - stored as NULL")
    }

    $svcStart = Parse-Date $r.C17
    $svcYears = $null
    if ($null -ne $svcStart) {
        $svcYears = [int][Math]::Floor((([datetime]::UtcNow - $svcStart).TotalDays) / 365.25)
        if ($svcYears -lt 0) { $svcYears = 0 }
    }

    $serial  = New-Serial
    $created = $BaseCreatedUtc.AddMinutes($i)
    $i++

    # Precomputed literals: building these inline inside the @() array made the
    # parser split the concatenation into separate elements.
    $litBirth    = if ($birth)    { "'{0}'" -f $birth.ToString('yyyy-MM-dd') }    else { 'NULL' }
    $litSvcStart = if ($svcStart) { "'{0}'" -f $svcStart.ToString('yyyy-MM-dd') } else { 'NULL' }
    $litSvcYears = if ($null -ne $svcYears) { "$svcYears" } else { 'NULL' }
    $litGender   = if ($null -ne $gender)   { "$gender" }   else { 'NULL' }
    $litSerial   = "N'{0}'" -f $serial
    $litPhone    = "N'{0}'" -f $phone
    $litCreated  = "'{0}'" -f $created.ToString('yyyy-MM-ddTHH:mm:ss')

    $cols = @(
        'SerialNumber','FullName','Phone','Email','BirthDate','Gender',
        'Province','District','SubDistrict','Address',
        'EducationLevel','Specialization',
        'Occupation','JobTitle','WorkPlace','ServiceStartDate','ServiceYears',
        'Skills','Experiences','TrainingCourses','Languages','BenefitField','Notes',
        'MovementId','CreatedAt'
    )

    $vals = @(
        $litSerial,
        (Q $name),
        $litPhone,
        'NULL',
        $litBirth,
        $litGender,
        (Q (Cut $r.C8  100)),
        (Q (Cut $r.C9  100)),
        (Q (Cut $r.C10 100)),
        (Q (Cut $r.C11 500)),
        (Q (Cut $r.C12 100)),
        (Q (Cut $r.C13 200)),
        (Q (Cut $r.C14 200)),
        (Q (Cut $r.C15 200)),
        (Q (Cut $r.C16 200)),
        $litSvcStart,
        $litSvcYears,
        (Q (Cut $r.C19 500)),
        (Q (Cut $r.C20 500)),
        (Q (Cut $r.C21 500)),
        (Q (Cut $r.C22 200)),
        (Q (Cut $r.C23 500)),
        (Q (Cut $r.C24 500)),
        "$MovementId",
        $litCreated
    )

    if ($vals.Count -ne $cols.Count) {
        throw "row $excelRow : produced $($vals.Count) values for $($cols.Count) columns"
    }

    $stmts.Add("-- excel row $excelRow")
    $stmts.Add("INSERT INTO Members (" + ($cols -join ', ') + ") VALUES (" + ($vals -join ', ') + ");")
}

$sql = New-Object System.Collections.Generic.List[string]
$sql.Add('SET NOCOUNT OFF;')
$sql.Add('SET XACT_ABORT ON;')
$sql.Add('BEGIN TRANSACTION;')
$sql.AddRange($stmts)
$sql.Add('COMMIT TRANSACTION;')

$sql | Out-File -LiteralPath $sqlOut -Encoding UTF8

$report.Insert(0, "PREPARED INSERTS : $i")
$report.Insert(1, "SKIPPED ROWS     : $skipped")
$report.Insert(2, "TARGET MovementId: $MovementId")
$report.Insert(3, '')
$report | Out-File -LiteralPath $reportOut -Encoding UTF8

Write-Output "inserts=$i skipped=$skipped"
