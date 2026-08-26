$ErrorActionPreference = 'Stop'

$csv = Join-Path $PSScriptRoot 'members_raw.csv'
$out = Join-Path $PSScriptRoot 'real-rows.txt'

$rows = Import-Csv -LiteralPath $csv -Encoding UTF8 |
        Where-Object { [int]$_.Row -ge 9 -and -not [string]::IsNullOrWhiteSpace($_.C3) }

$labels = @(
    'Timestamp/C1','Seq/C2','FullName','Gender','BirthDate','Age(calc)','Phone',
    'Province','District','SubDistrict','Address','Education','Specialization',
    'Occupation','JobTitle','WorkPlace','ServiceStart','ServiceYears(calc)',
    'Skills','Experiences','Courses','Languages','BenefitField','Notes'
)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("REAL ROWS: $($rows.Count)")
$lines.Add('')

foreach ($r in $rows) {
    $lines.Add("--- Excel row $($r.Row) ---")
    for ($c = 1; $c -le 24; $c++) {
        $v = [string]$r.("C$c")
        if (-not [string]::IsNullOrWhiteSpace($v)) {
            $lines.Add("   $($labels[$c-1]) = $v")
        }
    }
    $lines.Add('')
}

$lines | Out-File -LiteralPath $out -Encoding UTF8
Write-Output "wrote $out"
