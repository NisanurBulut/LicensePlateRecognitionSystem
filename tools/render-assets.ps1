<#
  Argus Plate Reader: ikon, logo ve banner üretir. Hepsi aynı geometriden çizilir, bu yüzden tutarlıdır.

  Üretilenler:
    PlakaTanimaSistemi\PlakaTanima.Wpf\Assets\app.ico     uygulama ikonu (16-256 px)
    PlakaTanimaSistemi\PlakaTanima.Wpf\Assets\logo.png    uygulama başlığındaki logo
    docs\images\argus-logo.png / argus-logo.svg           README logosu
    docs\images\argus-banner.svg                          README başlığı (ad ve slogan görselin içinde)

  Kullanım (depo kökünden):
    powershell -STA -File tools\render-assets.ps1
    powershell -STA -File tools\render-assets.ps1 -Name "Başka Ad" -Tagline "Başka slogan"
#>
param(
  [string]$Name = 'Argus Plate Reader',
  [string]$Tagline = 'Find the plate. Read it. See every step.',
  [string]$PlateText = '80 ABC 91052'    # 80 = Osmaniye
)
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'PlakaTanimaSistemi\PlakaTanima.Wpf\Assets'
$images = Join-Path $root 'docs\images'
New-Item -ItemType Directory -Force $assets, $images | Out-Null
$inv = [System.Globalization.CultureInfo]::InvariantCulture

# ---------------------------------------------------------------- ortak geometri (256 x 256 birimlik tuval)
$Geo = @{
  Radius   = 56                    # zemin köşe yuvarlaklığı
  EyeL     = 24;  EyeR = 232; EyeY = 128                    # gözün sol ve sağ ucu, orta çizgisi
  IrisR    = 50;  RingR = 43                                # iris ve içindeki halka
  PlateW   = 76;  PlateH = 28; PlateR = 5; StripW = 10      # göz bebeğindeki plaka
  BracketM = 6;   BracketL = 10; BracketW = 4.2             # sarı tespit köşeleri
  GlintX   = 154; GlintY = 92; GlintR = 5
}
$cyan = [System.Drawing.Color]::FromArgb(0, 180, 216)       # ana renk
$violet = [System.Drawing.Color]::FromArgb(123, 97, 255)    # accent
$navy = [System.Drawing.Color]::FromArgb(15, 23, 42)
$plateBlue = [System.Drawing.Color]::FromArgb(29, 78, 216)
$yellow = [System.Drawing.Color]::FromArgb(250, 204, 21)

function New-RoundedPath([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = $r * 2
  $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
  $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
  $p.CloseFigure(); return $p
}

# İkonu S piksel olarak çizer. Küçük boyutlarda ayrıntılar azaltılır.
function Draw-Icon([int]$S) {
  $k = $S / 256.0
  $bmp = New-Object System.Drawing.Bitmap $S, $S, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'; $g.InterpolationMode = 'HighQualityBicubic'
  $g.Clear([System.Drawing.Color]::Transparent)

  # zemin
  $bg = New-RoundedPath 0 0 $S $S ($Geo.Radius * $k)
  $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush((New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF $S, $S), $cyan, $violet)
  $g.FillPath($grad, $bg)

  # göz (iki kübik Bezier: kontrol noktaları ikinci derece eğriden türetilir)
  $x0 = $Geo.EyeL * $k; $x1 = $Geo.EyeR * $k; $y = $Geo.EyeY * $k
  $top = ($Geo.EyeY - 72) * $k; $bot = ($Geo.EyeY + 72) * $k   # kübik kontrol noktası y'si: 128 -/+ 72 (göz yüksekliği ~108)
  $dx = ($Geo.EyeR - $Geo.EyeL) / 3.0 * $k
  $eye = New-Object System.Drawing.Drawing2D.GraphicsPath
  $eye.AddBezier($x0, $y, ($x0 + $dx), $top, ($x1 - $dx), $top, $x1, $y)
  $eye.AddBezier($x1, $y, ($x1 - $dx), $bot, ($x0 + $dx), $bot, $x0, $y)
  $eye.CloseFigure()
  $g.FillPath([System.Drawing.Brushes]::White, $eye)

  # iris ve halka
  $cx = 128 * $k; $cy = 128 * $k; $ir = $Geo.IrisR * $k
  $g.FillEllipse((New-Object System.Drawing.SolidBrush $navy), ($cx - $ir), ($cy - $ir), (2 * $ir), (2 * $ir))
  if ($S -ge 48) {
    $rr = $Geo.RingR * $k
    $ringPen = New-Object System.Drawing.Pen($cyan, [Math]::Max(1.0, 4.5 * $k))
    $g.DrawEllipse($ringPen, ($cx - $rr), ($cy - $rr), (2 * $rr), (2 * $rr))
  }

  # göz bebeğindeki plaka
  $pw = $Geo.PlateW * $k; $ph = $Geo.PlateH * $k; $px = $cx - $pw / 2; $py = $cy - $ph / 2
  $plate = New-RoundedPath $px $py $pw $ph ([Math]::Max(1, $Geo.PlateR * $k))
  $g.FillPath([System.Drawing.Brushes]::White, $plate)
  if ($S -ge 32) {
    $st = $g.Save(); $g.SetClip($plate)
    $g.FillRectangle((New-Object System.Drawing.SolidBrush $plateBlue), $px, $py, ($Geo.StripW * $k), $ph)
    $g.Restore($st)
  }
  if ($S -ge 192) {
    $font = New-Object System.Drawing.Font('Segoe UI', [single](9.5 * $k), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
    $fmt.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap
    $rect = New-Object System.Drawing.RectangleF(($px + $Geo.StripW * $k), $py, ($pw - $Geo.StripW * $k), $ph)
    # Yazı plakaya sığmıyorsa yazı boyutunu küçült (uzun plaka metinleri için)
    $avail = ($pw - $Geo.StripW * $k) - 8 * $k
    $wide = $g.MeasureString($PlateText, $font).Width
    if ($wide -gt $avail) {
      $font = New-Object System.Drawing.Font('Segoe UI', [single](9.5 * $k * $avail / $wide), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    }
    $g.DrawString($PlateText, $font, (New-Object System.Drawing.SolidBrush $navy), $rect, $fmt)
  }

  # sarı tespit köşeleri
  if ($S -ge 32) {
    $pen = New-Object System.Drawing.Pen($yellow, [Math]::Max(1.2, $Geo.BracketW * $k)); $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    $m = $Geo.BracketM * $k; $L = $Geo.BracketL * $k
    $ax = $px - $m; $bx = $px + $pw + $m; $ay = $py - $m; $by = $py + $ph + $m
    $P = { param($a, $b) New-Object System.Drawing.PointF ([single]$a), ([single]$b) }
    $g.DrawLines($pen, @((& $P $ax ($ay + $L)), (& $P $ax $ay), (& $P ($ax + $L) $ay)))
    $g.DrawLines($pen, @((& $P ($bx - $L) $ay), (& $P $bx $ay), (& $P $bx ($ay + $L))))
    $g.DrawLines($pen, @((& $P $bx ($by - $L)), (& $P $bx $by), (& $P ($bx - $L) $by)))
    $g.DrawLines($pen, @((& $P ($ax + $L) $by), (& $P $ax $by), (& $P $ax ($by - $L))))
  }

  # parıltı
  if ($S -ge 48) {
    $gr = $Geo.GlintR * $k
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 255, 255, 255))), ($Geo.GlintX * $k - $gr), ($Geo.GlintY * $k - $gr), (2 * $gr), (2 * $gr))
  }
  $g.Dispose(); return $bmp
}

# ---------------------------------------------------------------- .ico ve .png
$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = @()
foreach ($s in $sizes) {
  $b = Draw-Icon $s
  $ms = New-Object System.IO.MemoryStream; $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); $pngs += , $ms.ToArray()
  if ($s -eq 256) {
    $b.Save((Join-Path $assets 'logo.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    $b.Save((Join-Path $images 'argus-logo.png'), [System.Drawing.Imaging.ImageFormat]::Png)
  }
  $b.Dispose()
}
$out = New-Object System.IO.MemoryStream; $w = New-Object System.IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
  $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
  $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0); $w.Write([uint16]1); $w.Write([uint16]32)
  $w.Write([uint32]$pngs[$i].Length); $w.Write([uint32]$offset); $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $w.Write($p) }; $w.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $assets 'app.ico'), $out.ToArray())

# ---------------------------------------------------------------- SVG (aynı geometri)
function F($v) { [string]::Format($inv, '{0:0.##}', $v) }
$cx = 128; $cy = 128
$top = $Geo.EyeY - 72; $bot = $Geo.EyeY + 72; $dx = ($Geo.EyeR - $Geo.EyeL) / 3.0
$px = $cx - $Geo.PlateW / 2; $py = $cy - $Geo.PlateH / 2
$ax = $px - $Geo.BracketM; $bx = $px + $Geo.PlateW + $Geo.BracketM; $ay = $py - $Geo.BracketM; $by = $py + $Geo.PlateH + $Geo.BracketM; $L = $Geo.BracketL

# id öneki: aynı sayfada birden çok SVG olursa çakışmasın
function Logo-Group([string]$id) {
@"
    <defs>
      <linearGradient id="$id-bg" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#00B4D8"/><stop offset="1" stop-color="#7B61FF"/>
      </linearGradient>
      <clipPath id="$id-plate"><rect x="$(F $px)" y="$(F $py)" width="$($Geo.PlateW)" height="$($Geo.PlateH)" rx="$($Geo.PlateR)"/></clipPath>
    </defs>
    <rect width="256" height="256" rx="$($Geo.Radius)" fill="url(#$id-bg)"/>
    <path d="M$($Geo.EyeL) $($Geo.EyeY) C$(F ($Geo.EyeL + $dx)) $top $(F ($Geo.EyeR - $dx)) $top $($Geo.EyeR) $($Geo.EyeY) C$(F ($Geo.EyeR - $dx)) $bot $(F ($Geo.EyeL + $dx)) $bot $($Geo.EyeL) $($Geo.EyeY) Z" fill="#FFFFFF"/>
    <circle cx="$cx" cy="$cy" r="$($Geo.IrisR)" fill="#0F172A"/>
    <circle cx="$cx" cy="$cy" r="$($Geo.RingR)" fill="none" stroke="#00B4D8" stroke-width="4.5"/>
    <rect x="$(F $px)" y="$(F $py)" width="$($Geo.PlateW)" height="$($Geo.PlateH)" rx="$($Geo.PlateR)" fill="#FFFFFF"/>
    <rect x="$(F $px)" y="$(F $py)" width="$($Geo.StripW)" height="$($Geo.PlateH)" fill="#1D4ED8" clip-path="url(#$id-plate)"/>
    <text x="$(F ($px + $Geo.StripW + ($Geo.PlateW - $Geo.StripW) / 2))" y="$(F ($cy + 3.4))" text-anchor="middle" font-family="'Segoe UI', 'Helvetica Neue', Arial, sans-serif" font-size="9.5" font-weight="700" fill="#0F172A" textLength="$($Geo.PlateW - $Geo.StripW - 10)" lengthAdjust="spacingAndGlyphs">$PlateText</text>
    <g fill="none" stroke="#FACC15" stroke-width="$($Geo.BracketW)" stroke-linecap="round" stroke-linejoin="round">
      <path d="M$(F $ax) $(F ($ay + $L)) V$(F $ay) H$(F ($ax + $L))"/>
      <path d="M$(F ($bx - $L)) $(F $ay) H$(F $bx) V$(F ($ay + $L))"/>
      <path d="M$(F $bx) $(F ($by - $L)) V$(F $by) H$(F ($bx - $L))"/>
      <path d="M$(F ($ax + $L)) $(F $by) H$(F $ax) V$(F ($by - $L))"/>
    </g>
    <circle cx="$($Geo.GlintX)" cy="$($Geo.GlintY)" r="$($Geo.GlintR)" fill="#FFFFFF" opacity="0.9"/>
"@
}
$nameEsc = [System.Security.SecurityElement]::Escape($Name); $tagEsc = [System.Security.SecurityElement]::Escape($Tagline)

$logoSvg = @"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256" role="img" aria-label="$nameEsc logo">
$(Logo-Group 'l')
</svg>
"@
[System.IO.File]::WriteAllText((Join-Path $images 'argus-logo.svg'), $logoSvg, (New-Object System.Text.UTF8Encoding $false))

# küçük plaka (banner sağ tarafı): sarı köşeli
function Mini-Plate([double]$x, [double]$y) {
  $w2 = 84; $h2 = 34; $m = 6; $l = 9
  @"
    <g transform="translate($x $y)">
      <rect width="$w2" height="$h2" rx="6" fill="#FFFFFF"/>
      <rect width="14" height="$h2" rx="6" fill="#1D4ED8"/><rect x="8" width="6" height="$h2" fill="#1D4ED8"/>
      <rect x="24" y="10" width="14" height="14" rx="2" fill="#0F172A"/><rect x="42" y="10" width="14" height="14" rx="2" fill="#0F172A"/><rect x="60" y="10" width="14" height="14" rx="2" fill="#0F172A"/>
      <g fill="none" stroke="#FACC15" stroke-width="3.4" stroke-linecap="round" stroke-linejoin="round">
        <path d="M$(-$m) $($l - $m) V$(-$m) H$($l - $m)"/><path d="M$($w2 + $m - $l) $(-$m) H$($w2 + $m) V$($l - $m)"/>
        <path d="M$($w2 + $m) $($h2 + $m - $l) V$($h2 + $m) H$($w2 + $m - $l)"/><path d="M$($l - $m) $($h2 + $m) H$(-$m) V$($h2 + $m - $l)"/>
      </g>
    </g>
"@
}
$bannerSvg = @"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 880 150" width="880" height="150" role="img"
     aria-label="${nameEsc}: ${tagEsc}">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#0B1B3A"/><stop offset="0.55" stop-color="#1B2C70"/><stop offset="1" stop-color="#5B45D8"/>
    </linearGradient>
    <linearGradient id="line" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#00B4D8"/><stop offset="1" stop-color="#7B61FF"/>
    </linearGradient>
    <radialGradient id="glow" cx="0.5" cy="0.5" r="0.5">
      <stop offset="0" stop-color="#00B4D8" stop-opacity="0.45"/><stop offset="1" stop-color="#00B4D8" stop-opacity="0"/>
    </radialGradient>
  </defs>

  <rect width="880" height="150" rx="18" fill="url(#bg)"/>
  <circle cx="730" cy="75" r="120" fill="url(#glow)"/>
  <rect x="2" y="2" width="876" height="146" rx="16" fill="none" stroke="url(#line)" stroke-width="2"/>

  <!-- logo (sol) -->
  <g transform="translate(22 13) scale(0.48)">
$(Logo-Group 'b')
  </g>

  <!-- ad ve slogan (logonun yanında) -->
  <text x="168" y="82" font-family="'Segoe UI', 'Helvetica Neue', Arial, sans-serif" font-size="50" font-weight="800" fill="#FFFFFF" textLength="520" lengthAdjust="spacingAndGlyphs">$nameEsc</text>
  <text x="170" y="118" font-family="'Segoe UI', 'Helvetica Neue', Arial, sans-serif" font-size="20" fill="#CFE8FF" opacity="0.95">$tagEsc</text>

  <!-- üç küçük plaka (sağ) -->
$(Mini-Plate 726 22)
$(Mini-Plate 770 58)
$(Mini-Plate 714 94)
</svg>
"@
[System.IO.File]::WriteAllText((Join-Path $images 'argus-banner.svg'), $bannerSvg, (New-Object System.Text.UTF8Encoding $false))

"tamam: app.ico ($([int]((Get-Item (Join-Path $assets 'app.ico')).Length / 1KB)) KB), logo.png, argus-logo.png/.svg, argus-banner.svg"
