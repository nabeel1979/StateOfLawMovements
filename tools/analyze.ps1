$ErrorActionPreference = 'Stop'

$csv = Join-Path $PSScriptRoot 'members_raw.csv'
$out = Join-Path $PSScriptRoot 'analyze.txt'

$rows = Import-Csv -LiteralPath $csv -Encoding UTF8 | Where-Object { [int]$_.Row -ge 9 }

# Approximate SQL Server's Arabic_CI_AI collation so duplicate detection matches
# what the unique index will actually reject.
function Normalize-Ar([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return '' }
    $t = $s -replace '[\u064B-\u065F\u0670\u0640]', ''
    $t = $t -replace '[\u0622\u0623\u0625\u0627]', [string][char]0x0627
    $t = $t -replace '[\u0649]', [string][char]0x064A
    $t = $t -replace '[\u0629]', [string][char]0x0647
    $t = $t -replace '\s+', ' '
    return $t.Trim()
}

function Clean-Phone([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return '' }
    $d = ($s -replace '[^\d]', '')
    if ($d.StartsWith('964')) { $d = '0' + $d.Substring(3) }
    if ($d.Length -eq 10 -and $d.StartsWith('7')) { $d = '0' + $d }
    return $d
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("TOTAL DATA ROWS: $($rows.Count)")

$noName  = @($rows | Where-Object { [string]::IsNullOrWhiteSpace($_.C3) })
$noPhone = @($rows | Where-Object { (Clean-Phone $_.C7).Length -eq 0 })
$lines.Add("BLANK NAME  : $($noName.Count)   rows: $(($noName.Row | Select-Object -First 40) -join ',')")
$lines.Add("BLANK PHONE : $($noPhone.Count)  rows: $(($noPhone.Row | Select-Object -First 40) -join ',')")

# Phone shape
$badPhone = @($rows | Where-Object {
    $p = Clean-Phone $_.C7
    $p.Length -gt 0 -and -not ($p.Length -eq 11 -and $p.StartsWith('07'))
})
$lines.Add("ODD PHONE FORMAT : $($badPhone.Count)")
foreach ($b in ($badPhone | Select-Object -First 40)) {
    $lines.Add("   row $($b.Row): raw='$($b.C7)' clean='$(Clean-Phone $b.C7)' name='$($b.C3)'")
}

# Duplicate names (normalized)
$dupName = $rows | Where-Object { -not [string]::IsNullOrWhiteSpace($_.C3) } |
    Group-Object { Normalize-Ar $_.C3 } | Where-Object { $_.Count -gt 1 }
$lines.Add("DUPLICATE NAME GROUPS : $(@($dupName).Count)  (extra rows: $((@($dupName) | ForEach-Object { $_.Count - 1 } | Measure-Object -Sum).Sum))")
foreach ($g in (@($dupName) | Select-Object -First 60)) {
    $lines.Add("   [$($g.Count)x] '$($g.Name)' rows: $(($g.Group.Row) -join ',')")
}

# Duplicate phones (cleaned)
$dupPhone = $rows | Where-Object { (Clean-Phone $_.C7).Length -gt 0 } |
    Group-Object { Clean-Phone $_.C7 } | Where-Object { $_.Count -gt 1 }
$lines.Add("DUPLICATE PHONE GROUPS : $(@($dupPhone).Count)  (extra rows: $((@($dupPhone) | ForEach-Object { $_.Count - 1 } | Measure-Object -Sum).Sum))")
foreach ($g in (@($dupPhone) | Select-Object -First 60)) {
    $lines.Add("   [$($g.Count)x] '$($g.Name)' rows: $(($g.Group.Row) -join ',') names: $(($g.Group.C3) -join ' // ')")
}

# Birth date parseability
$badDate = @($rows | Where-Object {
    $v = $_.C5
    if ([string]::IsNullOrWhiteSpace($v) -or $v -eq '/' -or $v -eq '.') { return $false }
    $d = [datetime]::MinValue
    -not [datetime]::TryParseExact($v, 'yyyy-MM-dd', $null, 'None', [ref]$d)
})
$lines.Add("UNPARSEABLE BIRTHDATE : $($badDate.Count)")
foreach ($b in ($badDate | Select-Object -First 40)) { $lines.Add("   row $($b.Row): '$($b.C5)'") }

$blankDate = @($rows | Where-Object { [string]::IsNullOrWhiteSpace($_.C5) -or $_.C5 -eq '/' -or $_.C5 -eq '.' })
$lines.Add("BLANK BIRTHDATE : $($blankDate.Count)")

# Gender values
$lines.Add("GENDER VALUES:")
foreach ($g in ($rows | Group-Object C4 | Sort-Object Count -Descending)) {
    $lines.Add("   '$($g.Name)' -> $($g.Count)")
}

# Length overflow check
$limits = @{
    C3='200'; C8='100'; C9='100'; C10='100'; C11='500'; C12='100'; C13='200';
    C14='200'; C15='200'; C16='200'; C19='500'; C20='500'; C21='500'; C22='200'; C23='500'; C24='500'
}
$lines.Add("LENGTH OVERFLOWS:")
foreach ($k in ($limits.Keys | Sort-Object)) {
    $max = [int]$limits[$k]
    $over = @($rows | Where-Object { ([string]$_.$k).Length -gt $max })
    if ($over.Count -gt 0) {
        $lines.Add("   $k (max $max): $($over.Count) rows -> $(($over.Row | Select-Object -First 20) -join ',')")
    }
}

# Province values (sanity)
$lines.Add("PROVINCE VALUES:")
foreach ($g in ($rows | Group-Object C8 | Sort-Object Count -Descending | Select-Object -First 30)) {
    $lines.Add("   '$($g.Name)' -> $($g.Count)")
}

$lines | Out-File -LiteralPath $out -Encoding UTF8
Write-Output "wrote $out"
