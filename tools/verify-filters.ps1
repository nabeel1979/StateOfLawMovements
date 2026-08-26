$ErrorActionPreference = 'Stop'

# نتحقق من صحة الفلترة بمقارنة أعداد مجمّعة فقط - بلا أي بيانات شخصية
$cs = "Server=DESKTOP-C4GD19I\N;Database=StateOfLawMovements;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=10;"
Add-Type -AssemblyName System.Data

function SqlCount([string]$where) {
    $cn = New-Object System.Data.SqlClient.SqlConnection $cs
    $cn.Open()
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM Members WHERE $where"
    $n = [int]$cmd.ExecuteScalar()
    $cn.Close()
    return $n
}

$base = 'http://localhost:5248'
$login = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable sess -UseBasicParsing -TimeoutSec 30
$token = ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($login.Content).Groups[1].Value
Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -WebSession $sess -UseBasicParsing -TimeoutSec 30 `
    -Body @{ __RequestVerificationToken = $token; Email = 'admin@qanoon.iq'; Password = 'Admin@2024' } | Out-Null

function AppCount([string]$query) {
    $r = Invoke-WebRequest -Uri "$base/Admin/Members?$query" -WebSession $sess -UseBasicParsing -TimeoutSec 40
    # نطابق على وسم الشارة نفسه: الحروف العربية في هذا الملف تتشوّه بترميز ANSI
    $m = ([regex]'badge bg-primary fs-6"[^>]*>[^0-9<]*(\d+)').Match($r.Content)
    if (-not $m.Success) { return -1 }
    return [int]$m.Groups[1].Value
}

$cases = @(
    @{ n = 'all';                q = '';                                                                          w = '1=1' },
    @{ n = 'email IS NULL';      q = 'filters[0].Field=email&filters[0].Op=empty';                                w = "(Email IS NULL OR Email = '')" },
    @{ n = 'email NOT NULL';     q = 'filters[0].Field=email&filters[0].Op=notempty';                             w = "(Email IS NOT NULL AND Email <> '')" },
    @{ n = 'phone LIKE 07%';     q = 'filters[0].Field=phone&filters[0].Op=startswith&filters[0].Val=07';         w = "Phone LIKE '07%'" },
    @{ n = 'gender = 1';         q = 'filters[0].Field=gender&filters[0].Op=eq&filters[0].Val=1';                 w = 'Gender = 1' },
    @{ n = 'gender IS NULL';     q = 'filters[0].Field=gender&filters[0].Op=empty';                               w = 'Gender IS NULL' },
    @{ n = 'birth < 2000';       q = 'filters[0].Field=birthdate&filters[0].Op=before&filters[0].Val=2000-01-01'; w = "BirthDate < '2000-01-01'" },
    @{ n = 'birth >= 1990';      q = 'filters[0].Field=birthdate&filters[0].Op=after&filters[0].Val=1989-12-31';  w = "BirthDate >= '1990-01-01'" },
    @{ n = 'notes empty';        q = 'filters[0].Field=notes&filters[0].Op=empty';                                w = "(Notes IS NULL OR Notes = '')" },
    @{ n = 'AND phone+gender';   q = 'match=All&filters[0].Field=phone&filters[0].Op=startswith&filters[0].Val=07&filters[1].Field=gender&filters[1].Op=eq&filters[1].Val=1';  w = "Phone LIKE '07%' AND Gender = 1" },
    @{ n = 'OR phone+gender2';   q = 'match=Any&filters[0].Field=phone&filters[0].Op=startswith&filters[0].Val=07&filters[1].Field=gender&filters[1].Op=eq&filters[1].Val=2';  w = "(Phone LIKE '07%' OR Gender = 2)" },
    @{ n = 'name notcontains';   q = 'filters[0].Field=name&filters[0].Op=notcontains&filters[0].Val=zzzz';       w = "NOT (FullName IS NOT NULL AND FullName LIKE '%zzzz%')" }
)

$fail = 0
foreach ($c in $cases) {
    $expected = SqlCount $c.w
    $actual   = AppCount $c.q
    $mark = if ($expected -eq $actual) { 'PASS' } else { 'FAIL'; }
    if ($expected -ne $actual) { $fail++ }
    Write-Output ("{0}  {1,-20} sql={2}  app={3}" -f $mark, $c.n, $expected, $actual)
}
Write-Output ""
Write-Output "MISMATCHES: $fail"
