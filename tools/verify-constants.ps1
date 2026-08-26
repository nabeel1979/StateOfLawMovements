$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5248'
$fail = 0

function Get-Token($html) {
    return ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($html).Groups[1].Value
}
function Check($label, $ok, $extra) {
    if (-not $ok) { $script:fail++ }
    $tag = if ($ok) { 'PASS' } else { 'FAIL' }
    Write-Output ("{0}  {1}  {2}" -f $tag, $label, $extra)
}

$page = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable s -UseBasicParsing -TimeoutSec 30
$null = Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -WebSession $s `
        -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5 -Body @{
            __RequestVerificationToken = (Get-Token $page.Content)
            Email = 'admin@qanoon.iq'; Password = 'Admin@2024'
        }

# ── صفحة الثوابت: الفئات الأربع موجودة والمحافظات مبذورة ──
$sc = Invoke-WebRequest -Uri "$base/Admin/SystemConstants" -WebSession $s -UseBasicParsing -TimeoutSec 40
Check 'constants page loads' ($sc.StatusCode -eq 200) "status=$($sc.StatusCode)"

$cards = ([regex]'data-category="(\w+)"').Matches($sc.Content) | ForEach-Object { $_.Groups[1].Value }
$uniq = ($cards | Select-Object -Unique) -join ','
Check 'four categories rendered' ($cards.Count -eq 4) "categories=$uniq"

$provRows = ([regex]'(?s)data-category="Province".*?</section>').Match($sc.Content).Value
$provCount = ([regex]'class="sc-row').Matches($provRows).Count
Check 'province seeded with 18 values' ($provCount -eq 18) "rows=$provCount"

Check 'reorder buttons present' ($sc.Content -match 'MoveConstant') ''
Check 'toggle buttons present'  ($sc.Content -match 'ToggleConstant') ''
Check 'inline add form present' ($sc.Content -match 'CreateConstant') ''

# ── التصفية بفئة واحدة تُبقي الأعداد صحيحة في الشريط ──
$one = Invoke-WebRequest -Uri "$base/Admin/SystemConstants?category=Province" -WebSession $s -UseBasicParsing -TimeoutSec 30
$oneCards = ([regex]'data-category="(\w+)"').Matches($one.Content).Count
Check 'filtered view shows one card' ($oneCards -eq 1) "cards=$oneCards"
$tabCounts = ([regex]'class="sc-tab-count">(\d+)<').Matches($one.Content) | ForEach-Object { [int]$_.Groups[1].Value }
Check 'tab counts survive filtering' (($tabCounts | Where-Object { $_ -eq 0 }).Count -eq 0) ("counts=" + ($tabCounts -join ','))

$bad = Invoke-WebRequest -Uri "$base/Admin/SystemConstants?category=Nope" -WebSession $s -UseBasicParsing -TimeoutSec 30
Check 'unknown category falls back to all' (([regex]'data-category="(\w+)"').Matches($bad.Content).Count -eq 4) ''

# ── المحافظة تأتي من الثوابت في كل الاستمارات ──
$cm = Invoke-WebRequest -Uri "$base/Admin/CreateMovement" -WebSession $s -UseBasicParsing -TimeoutSec 30
$cmOpts = ([regex]'(?s)name="governorate".*?</select>').Match($cm.Content).Value
Check 'CreateMovement province options' (([regex]'<option').Matches($cmOpts).Count -eq 19) ("options=" + ([regex]'<option').Matches($cmOpts).Count)

$em = Invoke-WebRequest -Uri "$base/Admin/EditMovement/1" -WebSession $s -UseBasicParsing -TimeoutSec 30
$emOpts = ([regex]'(?s)name="governorate".*?</select>').Match($em.Content).Value
Check 'EditMovement province options' (([regex]'<option').Matches($emOpts).Count -eq 19) ("options=" + ([regex]'<option').Matches($emOpts).Count)

# فلتر الأعضاء يقرأ المحافظات من الثوابت
$mem = Invoke-WebRequest -Uri "$base/Admin/Members?filters[0].Field=province&filters[0].Op=eq" -WebSession $s -UseBasicParsing -TimeoutSec 40
$ch = ([regex]'var CHOICES = (\{.*?\});').Match($mem.Content)
if ($ch.Success) {
    $obj = $ch.Groups[1].Value | ConvertFrom-Json
    Check 'filter province list populated' ($obj.province.Count -ge 18) ("count=" + $obj.province.Count)
} else { Check 'filter province list populated' $false 'CHOICES missing' }

# ── إضافة قيمة ثم تعطيلها ثم حذفها ──
$tok = Get-Token $sc.Content
$probe = "zzProbe$([DateTime]::Now.ToString('HHmmss'))"
$null = Invoke-WebRequest -Uri "$base/Admin/CreateConstant" -Method Post -WebSession $s `
        -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5 `
        -Body @{ __RequestVerificationToken = $tok; category = 'Province'; value = $probe }

$after = Invoke-WebRequest -Uri "$base/Admin/SystemConstants?category=Province" -WebSession $s -UseBasicParsing -TimeoutSec 30
Check 'new value added' ($after.Content -match [regex]::Escape($probe)) ''

$rowId = ([regex]("(?s)data-value=`"$probe`".*?name=`"id`" value=`"(\d+)`"")).Match($after.Content).Groups[1].Value
Check 'new value has an id' ($rowId -ne '') "id=$rowId"

if ($rowId) {
    $tok2 = Get-Token $after.Content
    $null = Invoke-WebRequest -Uri "$base/Admin/ToggleConstant" -Method Post -WebSession $s `
            -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5 `
            -Body @{ __RequestVerificationToken = $tok2; id = $rowId }
    $off = Invoke-WebRequest -Uri "$base/Admin/SystemConstants?category=Province" -WebSession $s -UseBasicParsing -TimeoutSec 30
    $offRow = ([regex]("(?s)<div class=`"sc-row[^`"]*`" data-value=`"$probe`"")).Match($off.Content).Value
    Check 'toggle marks value disabled' ($offRow -match 'is-off') "row=$offRow"

    # القيمة المعطلة لا تظهر في استمارة الحركة
    $cm2 = Invoke-WebRequest -Uri "$base/Admin/CreateMovement" -WebSession $s -UseBasicParsing -TimeoutSec 30
    Check 'disabled value hidden from form' (-not ($cm2.Content -match [regex]::Escape($probe))) ''

    $tok3 = Get-Token $off.Content
    $null = Invoke-WebRequest -Uri "$base/Admin/DeleteConstant" -Method Post -WebSession $s `
            -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5 `
            -Body @{ __RequestVerificationToken = $tok3; id = $rowId }
    $gone = Invoke-WebRequest -Uri "$base/Admin/SystemConstants?category=Province" -WebSession $s -UseBasicParsing -TimeoutSec 30
    Check 'probe value cleaned up' (-not ($gone.Content -match [regex]::Escape($probe))) ''
    Check 'province back to 18' (([regex]'class="sc-row').Matches((([regex]'(?s)data-category="Province".*?</section>').Match($gone.Content).Value)).Count -eq 18) ''
}

# ── استمارة الانضمام العامة ──
$tokenRow = Invoke-WebRequest -Uri "$base/Admin/Movements" -WebSession $s -UseBasicParsing -TimeoutSec 30
$jt = ([regex]'/join/([A-Za-z0-9_\-]+)').Match($tokenRow.Content).Groups[1].Value
if ($jt) {
    try {
        $join = Invoke-WebRequest -Uri "$base/join/$jt" -UseBasicParsing -TimeoutSec 30
        $jOpts = ([regex]'(?s)name="Province".*?</select>').Match($join.Content).Value
        Check 'Join form province options' (([regex]'<option').Matches($jOpts).Count -eq 19) ("options=" + ([regex]'<option').Matches($jOpts).Count)
    } catch { Check 'Join form province options' $false $_.Exception.Message.Split([char]10)[0] }
} else { Write-Output 'SKIP  join form (no token found)' }

Write-Output ''
Write-Output $(if ($fail -eq 0) { 'ALL PASSED' } else { "$fail FAILED" })
exit $(if ($fail -eq 0) { 0 } else { 1 })
