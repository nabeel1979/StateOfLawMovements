# ═══════════════════════════════════════════════════════════════
#  سكريبت النشر على السيرفر - ائتلاف دولة القانون
#  يُنفَّذ هذا السكريبت على السيرفر (37.239.44.94) بصلاحية Administrator
# ═══════════════════════════════════════════════════════════════

$backendPath  = "E:\all site\StateOfLawMovements\Backend_StateOfLawMovements"
$frontendPath = "E:\all site\StateOfLawMovements\Frontend_StateOfLawMovements"
$zipPath      = "C:\deploy.zip"
$backendPool  = "Backend_StateOfLawMovements"
$frontendPool = "Frontend_StateOfLawMovements"

Write-Host "═══ بدء النشر ═══" -ForegroundColor Cyan

# ── 1. إيقاف الـ App Pools ──────────────────────────────────────
Write-Host "1. إيقاف App Pools..." -ForegroundColor Yellow
Import-Module WebAdministration -ErrorAction SilentlyContinue

foreach ($pool in @($backendPool, $frontendPool)) {
    try {
        if ((Get-WebAppPoolState -Name $pool -ErrorAction SilentlyContinue).Value -eq "Started") {
            Stop-WebAppPool -Name $pool
            Write-Host "   تم إيقاف: $pool" -ForegroundColor Green
        }
    } catch { Write-Host "   $pool غير موجود أو متوقف مسبقاً" -ForegroundColor Gray }
}
Start-Sleep -Seconds 3

# ── 2. إنشاء المجلدات ───────────────────────────────────────────
Write-Host "2. إنشاء المجلدات..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $backendPath  | Out-Null
New-Item -ItemType Directory -Force -Path $frontendPath | Out-Null
New-Item -ItemType Directory -Force -Path "$backendPath\logs"  | Out-Null
New-Item -ItemType Directory -Force -Path "$frontendPath\logs" | Out-Null
New-Item -ItemType Directory -Force -Path "$backendPath\wwwroot\uploads\members"  | Out-Null
New-Item -ItemType Directory -Force -Path "$frontendPath\wwwroot\uploads\members" | Out-Null

# ── 3. فك ضغط الملفات ─────────────────────────────────────────
Write-Host "3. فك الضغط إلى Backend..." -ForegroundColor Yellow
Expand-Archive -Path $zipPath -DestinationPath $backendPath -Force
Write-Host "   تم فك الضغط في Backend" -ForegroundColor Green

Write-Host "   نسخ إلى Frontend..." -ForegroundColor Yellow
Copy-Item -Path "$backendPath\*" -Destination $frontendPath -Recurse -Force
Write-Host "   تم النسخ إلى Frontend" -ForegroundColor Green

# ── 4. ضبط الصلاحيات ─────────────────────────────────────────
Write-Host "4. ضبط صلاحيات المجلدات..." -ForegroundColor Yellow
foreach ($path in @($backendPath, $frontendPath)) {
    $acl = Get-Acl $path
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "IIS AppPool\$backendPool", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
    )
    try { $acl.AddAccessRule($rule); Set-Acl $path $acl } catch {}
}

# ── 5. إنشاء App Pools ────────────────────────────────────────
Write-Host "5. إنشاء/تحديث App Pools..." -ForegroundColor Yellow
foreach ($pool in @($backendPool, $frontendPool)) {
    if (-not (Get-WebAppPool -Name $pool -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $pool
        Write-Host "   تم إنشاء Pool: $pool" -ForegroundColor Green
    }
    Set-ItemProperty "IIS:\AppPools\$pool" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty "IIS:\AppPools\$pool" -Name "startMode" -Value "AlwaysRunning"
    Set-ItemProperty "IIS:\AppPools\$pool" -Name "processModel.idleTimeout" -Value ([TimeSpan]::Zero)
}

# ── 6. إنشاء مواقع IIS ───────────────────────────────────────
Write-Host "6. إنشاء مواقع IIS..." -ForegroundColor Yellow

# Backend site
$backendDomain = "Backend_StateOfLawMovements.gcc.iq"
if (-not (Get-Website -Name $backendDomain -ErrorAction SilentlyContinue)) {
    New-Website -Name $backendDomain `
        -PhysicalPath $backendPath `
        -ApplicationPool $backendPool `
        -HostHeader $backendDomain `
        -Port 80 -Force
    Write-Host "   تم إنشاء موقع Backend: $backendDomain" -ForegroundColor Green
} else {
    Set-ItemProperty "IIS:\Sites\$backendDomain" -Name physicalPath -Value $backendPath
    Write-Host "   تم تحديث موقع Backend" -ForegroundColor Green
}

# Frontend site
$frontendDomain1 = "Frontend_StateOfLawMovements.gcc.iq"
$frontendDomain2 = "StateOfLawMovements.gcc.iq"
if (-not (Get-Website -Name $frontendDomain1 -ErrorAction SilentlyContinue)) {
    New-Website -Name $frontendDomain1 `
        -PhysicalPath $frontendPath `
        -ApplicationPool $frontendPool `
        -HostHeader $frontendDomain1 `
        -Port 80 -Force
    # إضافة binding ثاني
    New-WebBinding -Name $frontendDomain1 -Protocol "http" -Port 80 -HostHeader $frontendDomain2
    Write-Host "   تم إنشاء موقع Frontend: $frontendDomain1 + $frontendDomain2" -ForegroundColor Green
} else {
    Set-ItemProperty "IIS:\Sites\$frontendDomain1" -Name physicalPath -Value $frontendPath
    Write-Host "   تم تحديث موقع Frontend" -ForegroundColor Green
}

# ── 7. تشغيل App Pools ────────────────────────────────────────
Write-Host "7. تشغيل App Pools..." -ForegroundColor Yellow
foreach ($pool in @($backendPool, $frontendPool)) {
    try { Start-WebAppPool -Name $pool; Write-Host "   تم تشغيل: $pool" -ForegroundColor Green }
    catch { Write-Host "   خطأ في تشغيل $pool" -ForegroundColor Red }
}

Write-Host ""
Write-Host "═══ تم النشر بنجاح! ═══" -ForegroundColor Cyan
Write-Host "Backend  : http://$backendDomain" -ForegroundColor White
Write-Host "Frontend : http://$frontendDomain1" -ForegroundColor White
Write-Host "Frontend : http://$frontendDomain2" -ForegroundColor White
Write-Host ""
Write-Host "تأكد من تثبيت .NET 9 Hosting Bundle على السيرفر!" -ForegroundColor Yellow
