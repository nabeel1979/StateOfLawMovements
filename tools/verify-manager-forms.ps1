$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5248'

function Get-Token($html) {
    return ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($html).Groups[1].Value
}
function Login($email, $pass) {
    $page = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable s -UseBasicParsing -TimeoutSec 30
    $null = Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -WebSession $s `
            -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5 -Body @{
                __RequestVerificationToken = (Get-Token $page.Content); Email = $email; Password = $pass
            }
    return $s
}

$admin = Login 'admin@qanoon.iq' 'Admin@2024'

# نبحث عن حركة فيها أعضاء لإنشاء مسؤول اختباري عليها
$mid = $null
foreach ($c in 1..10) {
    $p = Invoke-WebRequest -Uri "$base/Admin/Members?movementId=$c" -WebSession $admin -UseBasicParsing -TimeoutSec 30
    if (([regex]'MemberDetails/\d+').Matches($p.Content).Count -gt 0) { $mid = $c; break }
}
if (-not $mid) { throw 'no movement with members' }

$stamp = [DateTime]::Now.ToString('HHmmss')
$mail = "probe$stamp@test.local"
$p1 = 'Probe@2024x'; $p2 = 'Probe@2024y'

$f = Invoke-WebRequest -Uri "$base/Admin/CreateManager?movementId=$mid" -WebSession $admin -UseBasicParsing -TimeoutSec 30
$null = Invoke-WebRequest -Uri "$base/Admin/CreateManager" -Method Post -WebSession $admin `
        -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5 -Body @{
            __RequestVerificationToken = (Get-Token $f.Content)
            movementId = $mid; fullName = "probe $stamp"; email = $mail; password = $p1; title = ''
        }

$mgr = Login $mail $p1
$cp = Invoke-WebRequest -Uri "$base/Account/ChangePassword" -WebSession $mgr -UseBasicParsing -TimeoutSec 30
$null = Invoke-WebRequest -Uri "$base/Account/ChangePassword" -Method Post -WebSession $mgr `
        -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 5 -Body @{
            __RequestVerificationToken = (Get-Token $cp.Content); newPassword = $p2; confirmPassword = $p2
        }

function CountOptions($html, $field) {
    $block = ([regex]("(?s)name=`"$field`".*?</select>")).Match($html).Value
    return ([regex]'<option').Matches($block).Count
}

$add = Invoke-WebRequest -Uri "$base/Manager/AddMember" -WebSession $mgr -UseBasicParsing -TimeoutSec 30
Write-Output ("AddMember  province options   = {0} (expect 19)" -f (CountOptions $add.Content 'Province'))
Write-Output ("AddMember  education options  = {0} (expect 11)" -f (CountOptions $add.Content 'EducationLevel'))
Write-Output ("AddMember  benefit options    = {0} (expect 14)" -f (CountOptions $add.Content 'BenefitField'))

$memList = Invoke-WebRequest -Uri "$base/Manager/Members" -WebSession $mgr -UseBasicParsing -TimeoutSec 30
$memId = ([regex]'EditMember/(\d+)|EditMember\?id=(\d+)').Match($memList.Content)
$id = $memId.Groups[1].Value + $memId.Groups[2].Value
if ($id) {
    $edit = Invoke-WebRequest -Uri "$base/Manager/EditMember/$id" -WebSession $mgr -UseBasicParsing -TimeoutSec 30
    Write-Output ("EditMember province options  = {0} (expect 19)" -f (CountOptions $edit.Content 'Province'))
} else {
    Write-Output 'EditMember skipped (no member id found)'
}

Write-Output "probe account: $mail"
