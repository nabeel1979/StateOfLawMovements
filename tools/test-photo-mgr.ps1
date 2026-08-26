# يجهّز مسؤول حركة محلياً على حركة فيها طلبات، ثم يفحص صفحة تفاصيل الطلب
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5248'
$cs   = 'Server=DESKTOP-C4GD19I\N;Database=StateOfLawMovements;Trusted_Connection=True;TrustServerCertificate=True'

function Q($sql) {
    $c = New-Object System.Data.SqlClient.SqlConnection $cs
    $c.Open()
    $cmd = $c.CreateCommand(); $cmd.CommandText = $sql
    $dt = New-Object System.Data.DataTable
    $dt.Load($cmd.ExecuteReader())
    $c.Close()
    return , $dt
}

function E($sql) {
    $c = New-Object System.Data.SqlClient.SqlConnection $cs
    $c.Open()
    $cmd = $c.CreateCommand(); $cmd.CommandText = $sql
    $n = $cmd.ExecuteNonQuery()
    $c.Close()
    return $n
}

# حركة فيها طلبات
$t = Q "SELECT TOP 1 MovementId, COUNT(*) c FROM JoinRequests GROUP BY MovementId ORDER BY c DESC"
if ($t.Rows.Count -eq 0) { Write-Host 'NO join requests in local DB'; exit 0 }
$mv = $t.Rows[0].Item('MovementId')
Write-Host "movement with requests: $mv (count=$($t.Rows[0].Item('c')))"

# هاش BCrypt لكلمة Test@12345 مولّد بالمكتبة نفسها
$pass = 'Admin@2024'
$h = Q "SELECT PasswordHash FROM Users WHERE Email = 'admin@qanoon.iq'"
$hash = $h.Rows[0].Item('PasswordHash')

$email = 'phototest@qanoon.iq'
E "DELETE FROM Users WHERE Email = '$email'" | Out-Null
E @"
INSERT INTO Users (FullName, Email, PasswordHash, Role, MovementId, IsActive, MustChangePassword, CreatedAt)
VALUES (N'photo tester', '$email', '$hash', 2, $mv, 1, 0, GETUTCDATE())
"@ | Out-Null
Write-Host "manager created for movement $mv"

# دخول
$p = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable s -UseBasicParsing -TimeoutSec 40
$tok = ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($p.Content).Groups[1].Value
$a = Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -WebSession $s -UseBasicParsing -TimeoutSec 40 `
     -MaximumRedirection 5 -Body @{ __RequestVerificationToken = $tok; Email = $email; Password = $pass }
Write-Host "LOGIN -> $($a.StatusCode) final=$($a.BaseResponse.ResponseUri.AbsolutePath)"

$reqs = Invoke-WebRequest -Uri "$base/Manager/JoinRequests" -WebSession $s -UseBasicParsing -TimeoutSec 40
$ids = ([regex]'/Manager/RequestDetails/(\d+)').Matches($reqs.Content) |
       ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
Write-Host "request ids: $($ids -join ',')"

foreach ($id in ($ids | Select-Object -First 5)) {
    $r = Invoke-WebRequest -Uri "$base/Manager/RequestDetails/$id" -WebSession $s -UseBasicParsing -TimeoutSec 40
    $c = $r.Content
    $ok = ($c -match 'photo-box') -and ($c -match 'imgViewerStage')
    $kind = if ($c -match 'photo-frame') { 'PHOTO' } elseif ($c -match 'photo-none') { 'placeholder' } else { 'MISSING' }
    # سلامة البنية: عدد div المفتوحة والمغلقة في الصفحة
    $open  = ([regex]'<div\b').Matches($c).Count
    $close = ([regex]'</div>').Matches($c).Count
    Write-Host ("req {0}: viewer={1} photo={2} divs open={3} close={4} balanced={5}" -f `
                $id, $ok, $kind, $open, $close, ($open -eq $close))
}

# صفحة العضو من جهة المسؤول
$mem = Invoke-WebRequest -Uri "$base/Manager/Members" -WebSession $s -UseBasicParsing -TimeoutSec 40
Write-Host "manager members -> $($mem.StatusCode) zoomable=$($mem.Content -match 'data-zoom=')"
$eids = ([regex]'/Manager/EditMember/(\d+)').Matches($mem.Content) |
        ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
foreach ($id in ($eids | Select-Object -First 2)) {
    $r = Invoke-WebRequest -Uri "$base/Manager/EditMember/$id" -WebSession $s -UseBasicParsing -TimeoutSec 40
    Write-Host ("edit {0}: {1} zoomable={2}" -f $id, $r.StatusCode, ($r.Content -match 'data-zoom='))
}

E "DELETE FROM Users WHERE Email = '$email'" | Out-Null
Write-Host 'test manager removed'
