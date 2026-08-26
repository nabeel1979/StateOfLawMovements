$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5248'

function Login($email, $pass) {
    $p = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable s -UseBasicParsing -TimeoutSec 40
    $t = ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($p.Content).Groups[1].Value
    $b = @{ __RequestVerificationToken = $t; Email = $email; Password = $pass }
    $a = Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -Body $b -WebSession $s `
         -UseBasicParsing -TimeoutSec 40 -MaximumRedirection 5
    Write-Host "LOGIN $email -> $($a.StatusCode) final=$($a.BaseResponse.ResponseUri.AbsolutePath)"
    return $s
}

function Check($name, $url, $sess, $needles) {
    try {
        $r = Invoke-WebRequest -Uri ($base + $url) -WebSession $sess -UseBasicParsing -TimeoutSec 40
        $miss = @()
        foreach ($n in $needles) { if ($r.Content -notmatch [regex]::Escape($n)) { $miss += $n } }
        if ($miss.Count -eq 0) { Write-Host ("PASS {0,-26} {1}" -f $name, $r.StatusCode) }
        else { Write-Host ("MISS {0,-26} missing: {1}" -f $name, ($miss -join ', ')) }
        return $r.Content
    }
    catch {
        $sc = $null
        if ($_.Exception.Response) { $sc = $_.Exception.Response.StatusCode.value__ }
        Write-Host ("FAIL {0,-26} status={1} :: {2}" -f $name, $sc, $_.Exception.Message.Split([char]10)[0])
        return ''
    }
}

$admin = Login 'admin@qanoon.iq' 'Admin@2024'

$list = Check 'admin members list' '/Admin/Members' $admin @('img-viewer', 'mf-toolbar')
$ids  = ([regex]'/Admin/MemberDetails/(\d+)').Matches($list) | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
Write-Host "member ids found: $($ids -join ',')"
Write-Host ("list thumbs zoomable: {0}" -f ($list -match 'data-zoom='))

foreach ($mid in ($ids | Select-Object -First 6)) {
    $md = Check "member details $mid" "/Admin/MemberDetails/$mid" $admin @('photo-box', 'img-viewer', 'imgViewerStage')
    if ($md -match 'photo-frame') {
        $src = ([regex]'data-zoom="([^"]+)"').Match($md).Groups[1].Value
        Write-Host "     -> photo zoomable: $src"
    }
    elseif ($md -match 'photo-none') { Write-Host '     -> placeholder (no photo)' }
}
