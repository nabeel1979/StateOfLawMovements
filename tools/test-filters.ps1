$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5248'

# تسجيل دخول: نقرأ رمز مكافحة التزييف من صفحة الدخول ثم نرسل بيانات المدير المزروع
$login = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable sess -UseBasicParsing -TimeoutSec 30
$token = ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($login.Content).Groups[1].Value
if (-not $token) { throw 'antiforgery token not found' }

$body = @{
    __RequestVerificationToken = $token
    Email    = 'admin@qanoon.iq'
    Password = 'Admin@2024'
}
$auth = Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -Body $body -WebSession $sess `
        -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5
Write-Output "LOGIN -> $($auth.StatusCode)  final=$($auth.BaseResponse.ResponseUri.AbsolutePath)"

$cases = @(
    @{ n = 'no filters';            u = '/Admin/Members' },
    @{ n = 'name contains';         u = '/Admin/Members?filters[0].Field=name&filters[0].Op=contains&filters[0].Val=a' },
    @{ n = 'name notcontains';      u = '/Admin/Members?filters[0].Field=name&filters[0].Op=notcontains&filters[0].Val=zzz' },
    @{ n = 'email empty';           u = '/Admin/Members?filters[0].Field=email&filters[0].Op=empty' },
    @{ n = 'email notempty';        u = '/Admin/Members?filters[0].Field=email&filters[0].Op=notempty' },
    @{ n = 'phone startswith';      u = '/Admin/Members?filters[0].Field=phone&filters[0].Op=startswith&filters[0].Val=07' },
    @{ n = 'serial endswith';       u = '/Admin/Members?filters[0].Field=serial&filters[0].Op=endswith&filters[0].Val=1' },
    @{ n = 'province eq';           u = '/Admin/Members?filters[0].Field=province&filters[0].Op=eq&filters[0].Val=bagdad' },
    @{ n = 'gender eq male';        u = '/Admin/Members?filters[0].Field=gender&filters[0].Op=eq&filters[0].Val=1' },
    @{ n = 'gender empty';          u = '/Admin/Members?filters[0].Field=gender&filters[0].Op=empty' },
    @{ n = 'birthdate before';      u = '/Admin/Members?filters[0].Field=birthdate&filters[0].Op=before&filters[0].Val=2000-01-01' },
    @{ n = 'birthdate after';       u = '/Admin/Members?filters[0].Field=birthdate&filters[0].Op=after&filters[0].Val=1990-01-01' },
    @{ n = 'birthdate on';          u = '/Admin/Members?filters[0].Field=birthdate&filters[0].Op=on&filters[0].Val=1995-05-05' },
    @{ n = 'birthdate empty';       u = '/Admin/Members?filters[0].Field=birthdate&filters[0].Op=empty' },
    @{ n = 'createdat on';          u = '/Admin/Members?filters[0].Field=createdat&filters[0].Op=on&filters[0].Val=2026-08-26' },
    @{ n = 'createdat before';      u = '/Admin/Members?filters[0].Field=createdat&filters[0].Op=before&filters[0].Val=2030-01-01' },
    @{ n = 'two filters AND';       u = '/Admin/Members?match=All&filters[0].Field=phone&filters[0].Op=startswith&filters[0].Val=07&filters[1].Field=email&filters[1].Op=empty' },
    @{ n = 'two filters OR';        u = '/Admin/Members?match=Any&filters[0].Field=name&filters[0].Op=contains&filters[0].Val=x&filters[1].Field=phone&filters[1].Op=contains&filters[1].Val=07' },
    @{ n = 'three filters mixed';   u = '/Admin/Members?match=All&filters[0].Field=gender&filters[0].Op=notempty&filters[1].Field=createdat&filters[1].Op=before&filters[1].Val=2030-01-01&filters[2].Field=notes&filters[2].Op=empty' },
    @{ n = 'bad field ignored';     u = '/Admin/Members?filters[0].Field=nope&filters[0].Op=contains&filters[0].Val=x' },
    @{ n = 'bad date ignored';      u = '/Admin/Members?filters[0].Field=birthdate&filters[0].Op=on&filters[0].Val=notadate' },
    @{ n = 'bad gender ignored';    u = '/Admin/Members?filters[0].Field=gender&filters[0].Op=eq&filters[0].Val=99' },
    @{ n = 'sparse index';          u = '/Admin/Members?filters[2].Field=name&filters[2].Op=contains&filters[2].Val=a' },
    @{ n = 'export with filters';   u = '/Admin/ExportMembers?filters[0].Field=phone&filters[0].Op=startswith&filters[0].Val=07' },
    @{ n = 'manager list';          u = '/Manager/Members' }
)

foreach ($c in $cases) {
    try {
        $r = Invoke-WebRequest -Uri ($base + $c.u) -WebSession $sess -UseBasicParsing -TimeoutSec 40
        $count = ''
        $mm = ([regex]'الإجمالي: (\d+)|(\d+) عضو').Match($r.Content)
        if ($mm.Success) { $count = " total=" + ($mm.Groups[1].Value + $mm.Groups[2].Value) }
        Write-Output ("OK   {0,-22} {1}{2} len={3}" -f $c.n, $r.StatusCode, $count, $r.RawContentLength)
    }
    catch {
        $sc = $null
        if ($_.Exception.Response) { $sc = $_.Exception.Response.StatusCode.value__ }
        Write-Output ("FAIL {0,-22} status={1} :: {2}" -f $c.n, $sc, $_.Exception.Message.Split([char]10)[0])
    }
}
