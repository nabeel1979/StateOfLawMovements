$ErrorActionPreference = 'Stop'

$sqlPath = Join-Path $PSScriptRoot 'members-import.sql'
$lines   = Get-Content -LiteralPath $sqlPath -Encoding UTF8

$inserts = @($lines | Where-Object { $_ -like 'INSERT INTO Members*' })
Write-Output "INSERT COUNT: $($inserts.Count)"

$bad = 0
foreach ($ln in $inserts) {
    # Quote parity: every literal must be closed.
    $quotes = ($ln.ToCharArray() | Where-Object { $_ -eq "'" }).Count
    if ($quotes % 2 -ne 0) { Write-Output "ODD QUOTE COUNT: $ln"; $bad++ }

    if ($ln -notmatch ", 1, '\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}'\);$") {
        Write-Output "BAD TAIL: ...$($ln.Substring([Math]::Max(0,$ln.Length-60)))"
        $bad++
    }
    if ($ln -notmatch "VALUES \(N'\d{8}', N'") {
        Write-Output "BAD HEAD: $($ln.Substring(0, [Math]::Min(160, $ln.Length)))"
        $bad++
    }
}

# Serial uniqueness across the generated script.
$serials = $inserts | ForEach-Object {
    if ($_ -match "VALUES \(N'(\d{8})'") { $matches[1] }
}
$dupSerial = $serials | Group-Object | Where-Object { $_.Count -gt 1 }
Write-Output "SERIALS: $($serials.Count)  UNIQUE: $(($serials | Select-Object -Unique).Count)  DUPES: $(@($dupSerial).Count)"

# Phone uniqueness.
$phones = $inserts | ForEach-Object {
    if ($_ -match "VALUES \(N'\d{8}', N'[^']*', N'(\d+)'") { $matches[1] }
}
$dupPhone = $phones | Group-Object | Where-Object { $_.Count -gt 1 }
Write-Output "PHONES : $($phones.Count)  UNIQUE: $(($phones | Select-Object -Unique).Count)  DUPES: $(@($dupPhone).Count)"

Write-Output "PROBLEMS: $bad"
