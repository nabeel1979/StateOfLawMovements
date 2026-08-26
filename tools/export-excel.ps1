$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$file = Get-ChildItem -LiteralPath $root -Filter *.xlsx |
        Sort-Object Length -Descending |
        Select-Object -First 1

$outCsv = Join-Path $PSScriptRoot 'members_raw.csv'
$log    = Join-Path $PSScriptRoot 'export-excel.log'

"FILE: $($file.FullName)" | Out-File -FilePath $log -Encoding UTF8

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

try {
    $wb = $excel.Workbooks.Open($file.FullName, $null, $true)
    $ws = $wb.Worksheets.Item(1)

    $used = $ws.UsedRange
    $lastRow = $used.Row + $used.Rows.Count - 1
    $lastCol = 24

    "SHEET: $($ws.Name)  lastRow=$lastRow" | Out-File -FilePath $log -Append -Encoding UTF8

    # Read the whole block in one COM call (fast), then index the 2D array.
    $rng = $ws.Range($ws.Cells.Item(8, 1), $ws.Cells.Item($lastRow, $lastCol))
    $vals = $rng.Value2

    $rowsOut = New-Object System.Collections.Generic.List[object]

    for ($r = 1; $r -le ($lastRow - 8 + 1); $r++) {
        $o = New-Object PSObject
        $o | Add-Member -NotePropertyName 'Row' -NotePropertyValue ($r + 7)
        for ($c = 1; $c -le $lastCol; $c++) {
            $v = $vals[$r, $c]
            if ($null -eq $v) {
                $s = ''
            }
            elseif ($v -is [double]) {
                # Heuristic: Excel date serials land in a plausible range.
                if ($v -gt 20000 -and $v -lt 60000) {
                    $s = [DateTime]::FromOADate($v).ToString('yyyy-MM-dd')
                }
                else {
                    $s = $v.ToString([System.Globalization.CultureInfo]::InvariantCulture)
                }
            }
            elseif ($v -is [DateTime]) {
                $s = $v.ToString('yyyy-MM-dd')
            }
            else {
                $s = [string]$v
            }
            $o | Add-Member -NotePropertyName ('C' + $c) -NotePropertyValue ($s.Trim())
        }
        $rowsOut.Add($o) | Out-Null
    }

    $rowsOut | Export-Csv -LiteralPath $outCsv -NoTypeInformation -Encoding UTF8
    "WROTE: $outCsv  rows=$($rowsOut.Count)" | Out-File -FilePath $log -Append -Encoding UTF8

    $wb.Close($false)
}
catch {
    "ERROR: $($_.Exception.Message)" | Out-File -FilePath $log -Append -Encoding UTF8
    throw
}
finally {
    $excel.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
}
