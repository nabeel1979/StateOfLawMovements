# Uploads photos with various extensions to the public join form and reports acceptance
# NOTE: no Arabic literals (PS 5.1 reads this file as ANSI)
$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5248'
$cs   = 'Server=DESKTOP-C4GD19I\N;Database=StateOfLawMovements;Trusted_Connection=True;TrustServerCertificate=True'

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Net.Http

function Q($sql) {
    $c = New-Object System.Data.SqlClient.SqlConnection $cs; $c.Open()
    $cmd = $c.CreateCommand(); $cmd.CommandText = $sql
    $dt = New-Object System.Data.DataTable; $dt.Load($cmd.ExecuteReader()); $c.Close()
    return , $dt
}
function E($sql) {
    $c = New-Object System.Data.SqlClient.SqlConnection $cs; $c.Open()
    $cmd = $c.CreateCommand(); $cmd.CommandText = $sql
    $n = $cmd.ExecuteNonQuery(); $c.Close(); return $n
}

$tok = (Q "SELECT TOP 1 PublicToken FROM Movements WHERE IsActive = 1").Rows[0].Item('PublicToken')
Write-Host "join token: $tok"

$tmp = Join-Path $env:TEMP 'qanoon_ext_test'
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
$bmp = New-Object System.Drawing.Bitmap 40, 50
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::SeaGreen); $g.Dispose()
$bmp.Save("$tmp\ok.jpg",  [System.Drawing.Imaging.ImageFormat]::Jpeg)
$bmp.Save("$tmp\ok.jpeg", [System.Drawing.Imaging.ImageFormat]::Jpeg)
$bmp.Save("$tmp\ok.png",  [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Save("$tmp\bad.gif", [System.Drawing.Imaging.ImageFormat]::Gif)
$bmp.Save("$tmp\bad.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$bmp.Dispose()
Copy-Item "$tmp\ok.png" "$tmp\bad.webp" -Force
Copy-Item "$tmp\ok.png" "$tmp\bad.svg"  -Force
Copy-Item "$tmp\ok.png" "$tmp\bad.png.html" -Force

$cases = @(
    @{ f = 'ok.jpg';       expect = 'accept' },
    @{ f = 'ok.jpeg';      expect = 'accept' },
    @{ f = 'ok.png';       expect = 'accept' },
    @{ f = 'bad.webp';     expect = 'reject' },
    @{ f = 'bad.gif';      expect = 'reject' },
    @{ f = 'bad.bmp';      expect = 'reject' },
    @{ f = 'bad.svg';      expect = 'reject' },
    @{ f = 'bad.png.html'; expect = 'reject' }
)

$i = 0
foreach ($c in $cases) {
    $i++
    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.CookieContainer = New-Object System.Net.CookieContainer
    $handler.AllowAutoRedirect = $true
    $client = New-Object System.Net.Http.HttpClient $handler

    $page = $client.GetStringAsync("$base/join/$tok").GetAwaiter().GetResult()
    $rvt = ([regex]'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Match($page).Groups[1].Value

    $phone = '0770' + (1000000 + $i).ToString()   # 11 digits, starts with 07
    $name  = 'ext test ' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

    $form = New-Object System.Net.Http.MultipartFormDataContent
    $form.Add((New-Object System.Net.Http.StringContent $rvt),   '__RequestVerificationToken')
    $form.Add((New-Object System.Net.Http.StringContent $name),  'FullName')
    $form.Add((New-Object System.Net.Http.StringContent $phone), 'Phone')
    $form.Add((New-Object System.Net.Http.StringContent '1'),    'Gender')

    $bytes = [IO.File]::ReadAllBytes((Join-Path $tmp $c.f))
    $fileContent = New-Object System.Net.Http.ByteArrayContent (, $bytes)
    $fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue 'image/png'
    $form.Add($fileContent, 'Photo', $c.f)

    try {
        $resp = $client.PostAsync("$base/join/$tok", $form).GetAwaiter().GetResult()
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $url  = $resp.RequestMessage.RequestUri.AbsolutePath

        $accepted = $url -match 'Confirmation'
        $verdict  = if ($accepted) { 'accept' } else { 'reject' }
        $mark     = if ($verdict -eq $c.expect) { 'PASS' } else { 'FAIL' }

        Write-Host ("{0} {1,-14} {2,-7} (want {3,-6}) http={4}" -f `
                    $mark, $c.f, $verdict, $c.expect, [int]$resp.StatusCode)

        if (-not $accepted) {
            # the summary block lists validation errors; text is HTML-encoded so decode it
            $alert = ([regex]'(?s)<div class="alert alert-danger mb-4">(.*?)</div>\s*</div>').Match($body).Groups[1].Value
            if (-not $alert) { $alert = ([regex]'(?s)<div class="alert alert-danger mb-4">(.*?)</ul>').Match($body).Groups[1].Value }
            $text = [System.Net.WebUtility]::HtmlDecode(($alert -replace '<[^>]+>', ' ')) -replace '\s+', ' '
            Write-Host ("     reason: " + $text.Trim())
        }

        if ($accepted) {
            $row = Q "SELECT PhotoPath FROM JoinRequests WHERE Phone = '$phone'"
            if ($row.Rows.Count -gt 0) {
                $pp = $row.Rows[0].Item('PhotoPath')
                Write-Host "     saved as: $pp"
                if ($pp -isnot [DBNull]) {
                    $f = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) 'src\QanoonCoalition.Web\wwwroot') ($pp.TrimStart('/') -replace '/', '\')
                    Remove-Item $f -Force -ErrorAction SilentlyContinue
                }
            }
            E "DELETE FROM JoinRequests WHERE Phone = '$phone'" | Out-Null
        }
    }
    catch { Write-Host ("ERR  {0,-14} :: {1}" -f $c.f, $_.Exception.Message.Split([char]10)[0]) }
    finally { $client.Dispose(); $handler.Dispose() }
}

Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
