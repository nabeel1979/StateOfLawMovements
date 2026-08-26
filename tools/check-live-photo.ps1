# Post-deploy check on the live site
$ErrorActionPreference = 'Continue'
$base = 'https://stateoflawmovements.gcc.iq'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$p = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable s -UseBasicParsing -TimeoutSec 60
$tok = ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($p.Content).Groups[1].Value
$a = Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -WebSession $s -UseBasicParsing -TimeoutSec 60 `
     -MaximumRedirection 5 -Body @{ __RequestVerificationToken = $tok; Email = 'admin@qanoon.iq'; Password = 'Admin@2024' }
Write-Host "LOGIN -> $($a.StatusCode) final=$($a.BaseResponse.ResponseUri.AbsolutePath)"

$css = Invoke-WebRequest -Uri "$base/css/app.css" -UseBasicParsing -TimeoutSec 60
Write-Host ("app.css: {0} bytes, has viewer styles = {1}" -f $css.RawContentLength, ($css.Content -match 'img-viewer-stage'))

$list = (Invoke-WebRequest -Uri "$base/Admin/Members" -WebSession $s -UseBasicParsing -TimeoutSec 60).Content
Write-Host ("members list: viewer={0} thumbsZoomable={1}" -f ($list -match 'imgViewerStage'), ($list -match 'data-zoom='))

$ids = ([regex]'/Admin/MemberDetails/(\d+)').Matches($list) | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
$withPhoto = 0; $without = 0
foreach ($id in ($ids | Select-Object -First 8)) {
    $d = (Invoke-WebRequest -Uri "$base/Admin/MemberDetails/$id" -WebSession $s -UseBasicParsing -TimeoutSec 60).Content
    if ($d -match 'photo-frame') {
        $withPhoto++
        $src = ([regex]'data-zoom="([^"]+)"').Match($d).Groups[1].Value
        $img = try { (Invoke-WebRequest -Uri ($base + $src) -WebSession $s -UseBasicParsing -TimeoutSec 60).StatusCode } catch { 'ERR' }
        Write-Host "  member $id -> photo $src (fetch=$img)"
    }
    elseif ($d -match 'photo-none') { $without++ }
    else { Write-Host "  member $id -> NO PHOTO BLOCK AT ALL" }
}
Write-Host "members checked: withPhoto=$withPhoto placeholder=$without"

$sc = Invoke-WebRequest -Uri "$base/Admin/SystemConstants" -WebSession $s -UseBasicParsing -TimeoutSec 60
Write-Host ("system constants page: {0} hasProvince={1}" -f $sc.StatusCode, ($sc.Content -match 'fa-map-marker-alt'))
