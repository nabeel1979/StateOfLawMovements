$ErrorActionPreference = 'Stop'

# Locate the workbook without embedding non-ASCII literals in this script.
$root = Split-Path -Parent $PSScriptRoot
$file = Get-ChildItem -LiteralPath $root -Filter *.xlsx |
        Sort-Object Length -Descending |
        Select-Object -First 1

if (-not $file) { throw "No xlsx found in $root" }
Write-Output "FILE: $($file.FullName)"

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

try {
    $wb = $excel.Workbooks.Open($file.FullName, $null, $true)

    foreach ($ws in $wb.Worksheets) {
        $used = $ws.UsedRange
        $r0 = $used.Row
        $c0 = $used.Column
        $rN = $r0 + $used.Rows.Count - 1
        $cN = $c0 + $used.Columns.Count - 1

        Write-Output "=== SHEET: $($ws.Name) | rows=$($used.Rows.Count) cols=$($used.Columns.Count) | r=$r0..$rN c=$c0..$cN ==="

        $maxR = [Math]::Min($rN, $r0 + 24)

        for ($r = $r0; $r -le $maxR; $r++) {
            $parts = @()
            for ($c = $c0; $c -le $cN; $c++) {
                $v = $ws.Cells.Item($r, $c).Text
                if ($null -eq $v) { $v = '' }
                $parts += ('[' + $c + ']' + $v)
            }
            $joined = $parts -join ' | '
            Write-Output "R$r :: $joined"
        }
        Write-Output ''
    }

    $wb.Close($false)
}
finally {
    $excel.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
}
