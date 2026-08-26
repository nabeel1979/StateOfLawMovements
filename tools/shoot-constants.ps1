$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5248'
$dir  = 'C:\qanoon_shot'
New-Item -ItemType Directory -Force -Path $dir | Out-Null

$token = ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
$login = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable s -UseBasicParsing -TimeoutSec 30
$null  = Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -WebSession $s `
         -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5 -Body @{
             __RequestVerificationToken = $token.Match($login.Content).Groups[1].Value
             Email = 'admin@qanoon.iq'; Password = 'Admin@2024'
         }

# نحوّل روابط الأصول النسبية إلى مطلقة ليعرضها المتصفح من الملف المحلي
$page = (Invoke-WebRequest -Uri "$base/Admin/SystemConstants" -WebSession $s -UseBasicParsing -TimeoutSec 40).Content
$page = $page -replace 'href="/', ('href="' + $base + '/')
$page = $page -replace 'src="/',  ('src="'  + $base + '/')

$html = Join-Path $dir 'constants.html'
[System.IO.File]::WriteAllText($html, $page, [System.Text.Encoding]::UTF8)

$chrome = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
$shots = @(
    @{ Size = '1440,1250'; Out = 'constants-desktop.png' },
    @{ Size = '430,1500';  Out = 'constants-mobile.png'  }
)

foreach ($shot in $shots) {
    $out = Join-Path $dir $shot.Out
    & $chrome --headless=new --disable-gpu --hide-scrollbars `
        --window-size=$($shot.Size) --virtual-time-budget=6000 `
        --screenshot="$out" "file:///$($html -replace '\\','/')" 2>$null
    if (Test-Path $out) {
        Write-Output ("{0}  {1} bytes" -f $shot.Out, (Get-Item $out).Length)
    } else {
        Write-Output ("{0}  FAILED" -f $shot.Out)
    }
}
