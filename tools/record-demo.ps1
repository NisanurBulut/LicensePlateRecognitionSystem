<#
  Gerçek uygulamayı çalıştırıp README için görselleri üretir:
    docs\images\argus-demo.gif       tüm akış (boş ekran, örnek 1, işlem hattı, sonuç, örnek 2)
    docs\images\argus-result.png     plaka bulundu ve okundu
    docs\images\argus-pipeline.png   işlem hattında bir adımı büyük görüntüleme
    docs\images\argus-warning.png    plaka bulundu ama metin Türk plaka biçimine uymuyor

  Önce uygulamayı derleyin (Release), sonra depo kökünden:
    powershell -STA -File tools\record-demo.ps1

  Not: Ekran görüntüleri pencerenin kendisinden alınır (PrintWindow); pencere başka pencerelerle örtülse de çalışır.
  Uygulamayı kayıt sırasında elle kullanmayın. OCR verisi (tessdata\eng.traineddata) yoksa sonuç ekranında metin görünmez.
#>
param(
  [string]$Exe,
  [int]$GifWidth = 800,
  [int]$ShotWidth = 1000
)
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, PresentationCore, WindowsBase
$root = Split-Path -Parent $PSScriptRoot
if (-not $Exe) { $Exe = Join-Path $root 'PlakaTanimaSistemi\PlakaTanima.Wpf\bin\Release\net48\PlakaTanima.exe' }
if (-not (Test-Path $Exe)) { throw "Uygulama bulunamadi: $Exe  (once 'dotnet build PlakaTanimaSistemi\PlakaTanima.sln -c Release' calistirin)" }
$images = Join-Path $root 'docs\images'
New-Item -ItemType Directory -Force $images | Out-Null
$work = Join-Path ([System.IO.Path]::GetTempPath()) ('argus-demo-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Force $work | Out-Null

Add-Type @"
using System; using System.Runtime.InteropServices;
public class Win {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@
[Win]::SetProcessDPIAware() | Out-Null

# ---------------------------------------------------------------- uygulama sürücüsü
$script:p = $null; $script:h = [IntPtr]::Zero
function Start-App([string]$image) {
  $args = if ($image) { '"' + $image + '"' } else { $null }
  $script:p = if ($args) { Start-Process $Exe -ArgumentList $args -PassThru } else { Start-Process $Exe -PassThru }
  $deadline = (Get-Date).AddSeconds(20)
  while ($script:p.MainWindowHandle -eq 0 -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 200; $script:p.Refresh() }
  if ($script:p.MainWindowHandle -eq 0) { throw 'Uygulama penceresi acilmadi' }
  $script:h = $script:p.MainWindowHandle
  Start-Sleep -Seconds 2
}
function Stop-App {
  if ($script:p -and -not $script:p.HasExited) { $script:p.CloseMainWindow() | Out-Null; Start-Sleep 1; if (-not $script:p.HasExited) { $script:p.Kill() } }
}
function Click([string]$pattern) {
  $el = [System.Windows.Automation.AutomationElement]::FromHandle($script:h)
  $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
  $btn = $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond) | Where-Object { $_.Current.Name -match $pattern } | Select-Object -First 1
  if (-not $btn) { throw "Dugme bulunamadi: $pattern" }
  $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}
function Select-Step([int]$n) {
  $el = [System.Windows.Automation.AutomationElement]::FromHandle($script:h)
  $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)
  $items = $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
  $items[$n - 1].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
}
function Capture([int]$width) {
  if ([Win]::IsIconic($script:h)) { [Win]::ShowWindow($script:h, 9) | Out-Null; Start-Sleep -Milliseconds 300 }
  $r = New-Object Win+RECT; [Win]::GetWindowRect($script:h, [ref]$r) | Out-Null
  $w = $r.R - $r.L; $ht = $r.B - $r.T
  $full = New-Object System.Drawing.Bitmap $w, $ht
  $g = [System.Drawing.Graphics]::FromImage($full); $dc = $g.GetHdc()
  [Win]::PrintWindow($script:h, $dc, 2) | Out-Null
  $g.ReleaseHdc($dc); $g.Dispose()
  $th = [int]($ht * $width / $w)
  $small = New-Object System.Drawing.Bitmap $width, $th
  $g2 = [System.Drawing.Graphics]::FromImage($small)
  $g2.InterpolationMode = 'HighQualityBicubic'; $g2.SmoothingMode = 'HighQuality'; $g2.PixelOffsetMode = 'HighQuality'
  $g2.DrawImage($full, 0, 0, $width, $th); $g2.Dispose(); $full.Dispose()
  return $small
}
function Save-Shot([string]$name) { $b = Capture $ShotWidth; $b.Save((Join-Path $images $name), [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose(); "  $name" }

# ---------------------------------------------------------------- 1) Ekran görüntüleri
$samples = Join-Path $root 'PlakaTanimaSistemi\images'
Write-Host 'Ekran goruntuleri...'
Start-App (Join-Path $samples '1.jpg'); Click 'Plakay'; Start-Sleep -Seconds 5
Save-Shot 'argus-result.png'
Select-Step 5; Start-Sleep -Milliseconds 600
Save-Shot 'argus-pipeline.png'
Stop-App

# Uyarı görüntüsü için: örnek fotoğrafın plaka alanına il kodu geçersiz (99) bir plaka çiz
$bad = Join-Path $work 'gecersiz.jpg'
$src = New-Object System.Drawing.Bitmap (Join-Path $samples '8.jpg')
$sc = [double]$src.Width / 480.0
$x = [int](183 * $sc); $y = [int](207 * $sc); $pw = [int](117 * $sc); $ph = [int](38 * $sc)
$gg = [System.Drawing.Graphics]::FromImage($src); $gg.SmoothingMode = 'AntiAlias'; $gg.TextRenderingHint = 'AntiAliasGridFit'
$gg.FillRectangle([System.Drawing.Brushes]::White, $x, $y, $pw, $ph)
$gg.DrawRectangle((New-Object System.Drawing.Pen([System.Drawing.Color]::Black, [Math]::Max(2, $sc * 1.5))), $x, $y, $pw, $ph)
$font = New-Object System.Drawing.Font('Arial', [single]($ph * 0.52), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fmt = New-Object System.Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
$gg.DrawString('99 QZX 12', $font, [System.Drawing.Brushes]::Black, (New-Object System.Drawing.RectangleF($x, $y, $pw, $ph)), $fmt)
$gg.Dispose(); $src.Save($bad, [System.Drawing.Imaging.ImageFormat]::Jpeg); $src.Dispose()
Start-App $bad; Click 'Plakay'; Start-Sleep -Seconds 5
Save-Shot 'argus-warning.png'
Stop-App

# ---------------------------------------------------------------- 2) Demo GIF
Write-Host 'GIF kareleri...'
$frames = Join-Path $work 'frames'; New-Item -ItemType Directory -Force $frames | Out-Null
$delays = New-Object System.Collections.Generic.List[int]
$clock = [System.Diagnostics.Stopwatch]::StartNew()
$script:n = 0
function Grab { $b = Capture $GifWidth; $b.Save((Join-Path $frames ('f_{0:000}.png' -f $script:n)), [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose(); $script:n++ }
function Hold([int]$ms) { Grab; $delays.Add($ms) }
function Burst([int]$ms) {
  $end = $clock.ElapsedMilliseconds + $ms; $last = $clock.ElapsedMilliseconds
  while ($clock.ElapsedMilliseconds -lt $end) { Grab; $now = $clock.ElapsedMilliseconds; $delays.Add([Math]::Min(250, [Math]::Max(60, [int]($now - $last)))); $last = $now }
}
Start-App $null
Hold 1500
Click 'rnek 1'; Start-Sleep -Milliseconds 500; Hold 1100
Click 'Plakay'; Burst 1500; Start-Sleep -Milliseconds 1500; Hold 2600
Click 'rnek 2'; Start-Sleep -Milliseconds 500; Hold 800
Click 'Plakay'; Burst 1500; Start-Sleep -Milliseconds 1500; Hold 2800
Stop-App

# ---------------------------------------------------------------- 3) GIF'i kodla (WPF) ve gecikme + döngü bilgisini ekle
Write-Host 'GIF kodlaniyor...'
$files = Get-ChildItem $frames -Filter 'f_*.png' | Sort-Object Name
$enc = New-Object System.Windows.Media.Imaging.GifBitmapEncoder
foreach ($f in $files) {
  $img = New-Object System.Windows.Media.Imaging.BitmapImage
  $img.BeginInit(); $img.CacheOption = 'OnLoad'; $img.UriSource = New-Object System.Uri($f.FullName); $img.EndInit(); $img.Freeze()
  $pal = New-Object System.Windows.Media.Imaging.BitmapPalette($img, 256)    # her kare için kendi en iyi 256 rengi
  $idx = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap($img, [System.Windows.Media.PixelFormats]::Indexed8, $pal, 0)
  $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($idx))
}
$ms = New-Object System.IO.MemoryStream; $enc.Save($ms); $raw = $ms.ToArray()

# WPF kodlayıcı kare gecikmesi ve sonsuz döngü yazmaz; bunları GIF bayt akışına biz ekleriz.
$out = New-Object System.IO.MemoryStream
$hdrEnd = 13; if ($raw[10] -band 0x80) { $hdrEnd += 3 * (1 -shl (($raw[10] -band 7) + 1)) }
$out.Write($raw, 0, $hdrEnd)
$loop = [byte[]](0x21, 0xFF, 0x0B) + [System.Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0') + [byte[]](0x03, 0x01, 0x00, 0x00, 0x00)
$out.Write($loop, 0, $loop.Length)
$pos = $hdrEnd; $frame = 0
while ($pos -lt $raw.Length) {
  $t = $raw[$pos]
  if ($t -eq 0x3B) { $out.WriteByte(0x3B); break }
  if ($t -eq 0x21) {
    $start = $pos; $pos += 2
    while ($raw[$pos] -ne 0) { $pos += $raw[$pos] + 1 }; $pos++
    $out.Write($raw, $start, $pos - $start)
  } elseif ($t -eq 0x2C) {
    $cs = [Math]::Max(2, [int][Math]::Round($delays[$frame] / 10.0))
    $gce = [byte[]](0x21, 0xF9, 0x04, 0x04, ($cs -band 0xFF), (($cs -shr 8) -band 0xFF), 0x00, 0x00)   # bırakma: yerinde bırak
    $out.Write($gce, 0, $gce.Length)
    $start = $pos; $lf = $raw[$pos + 9]; $pos += 10
    if ($lf -band 0x80) { $pos += 3 * (1 -shl (($lf -band 7) + 1)) }
    $pos++
    while ($raw[$pos] -ne 0) { $pos += $raw[$pos] + 1 }; $pos++
    $out.Write($raw, $start, $pos - $start); $frame++
  } else { throw "Beklenmeyen GIF baytı $t konum $pos" }
}
$gif = Join-Path $images 'argus-demo.gif'
[System.IO.File]::WriteAllBytes($gif, $out.ToArray())
"  argus-demo.gif  $([math]::Round((Get-Item $gif).Length / 1MB, 2)) MB, $frame kare, $([int](($delays | Measure-Object -Sum).Sum / 1000)) sn"
Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
