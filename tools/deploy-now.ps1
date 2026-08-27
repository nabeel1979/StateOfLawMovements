# نشر سريع: يطلب كلمة مرور SSH بشكل آمن ثم يبني وينشر على الخادم.
# الاستخدام: powershell -File tools\deploy-now.ps1            (العنوان المحلي)
#            powershell -File tools\deploy-now.ps1 -Public    (العنوان العام)
param([switch]$Public, [switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

$env:QANOON_SSH_HOST = if ($Public) { '37.239.44.94' } else { '192.168.0.75' }
Write-Host "target server: $($env:QANOON_SSH_HOST)" -ForegroundColor Cyan

if (-not $SkipBuild) {
    Write-Host 'building...' -ForegroundColor Yellow
    dotnet build (Join-Path $projectRoot 'src\QanoonCoalition.Web\QanoonCoalition.Web.csproj') -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }
}

# كلمة المرور تُقرأ مخفية ولا تُطبع ولا تُحفظ في أي ملف
$sec = Read-Host -Prompt 'SSH password' -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
try   { $env:QANOON_SSH_PASS = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }

if ([string]::IsNullOrWhiteSpace($env:QANOON_SSH_PASS)) { throw 'no password entered' }

try {
    & (Join-Path $PSScriptRoot 'deploy-dll.ps1')
}
finally {
    # لا تُترك كلمة المرور في بيئة الجلسة بعد الانتهاء
    Remove-Item Env:\QANOON_SSH_PASS -ErrorAction SilentlyContinue
}
