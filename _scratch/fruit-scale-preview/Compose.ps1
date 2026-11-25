param([switch]$Alternatives)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$maps = @(
    @{ Level = 'L1'; Name = 'Training Ring'; Scale = '0.95'; Art = 'CREATIVE ART'; Reason = 'Chừa khoảng quanh fruit, giữ dáng quả dễ nhận ra giữa cây và đạo cụ.' },
    @{ Level = 'L2'; Name = 'Moving Ground'; Scale = '0.90'; Art = 'GREYBOX'; Reason = 'Tăng khoảng trống thị giác giữa fruit, bàn xoay và khối đẩy.' },
    @{ Level = 'L3'; Name = 'Cannon Court'; Scale = '0.85'; Art = 'CREATIVE ART'; Reason = 'Lộ thêm mặt sân quanh fruit và giữ pháo dễ thấy.' },
    @{ Level = 'L4'; Name = 'Frost Ring'; Scale = '0.90'; Art = 'CREATIVE ART'; Reason = 'Lộ rõ hơn mép ô băng dưới chân, vẫn dễ nhận ra từng fruit.' },
    @{ Level = 'L5'; Name = 'Chaos Core'; Scale = '0.85'; Art = 'GREYBOX'; Reason = 'Giảm phần fruit che ô băng trong khung hình có cả pháo.' }
)
if ($Alternatives) {
    $maps = @(
        @{ Level = 'L2'; Name = 'Moving Ground'; Scale = '0.90'; Alternative = '0.80'; Art = 'GREYBOX'; Reason = '0.80× chừa thêm khoảng quanh fruit, vẫn dễ phân biệt sáu quả trên nền greybox.' },
        @{ Level = 'L3'; Name = 'Cannon Court'; Scale = '0.85'; Alternative = '0.70'; Art = 'CREATIVE ART'; Reason = '0.70× lộ thêm sân, nhưng Peach và Blueberry dễ lẫn với đạo cụ khi nhìn toàn map.' }
    )
}

$background = [Drawing.Color]::FromArgb(15, 23, 37)
$white = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(240, 246, 252))
$muted = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(186, 201, 216))
$accent = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(111, 230, 197))
$amber = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255, 208, 116))
$titleFont = [Drawing.Font]::new('Segoe UI', 53, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$scaleFont = [Drawing.Font]::new('Segoe UI', 42, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$noteFont = [Drawing.Font]::new('Segoe UI', 35, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)

try {
    foreach ($map in $maps) {
        $left = [Drawing.Image]::FromFile((Join-Path $PSScriptRoot "$($map.Level)-baseline.png"))
        $right = [Drawing.Image]::FromFile((Join-Path $PSScriptRoot "$($map.Level)-proposed.png"))
        $extra = if ($Alternatives) {
            [Drawing.Image]::FromFile((Join-Path $PSScriptRoot "$($map.Level)-alternative.png"))
        }
        $width = if ($Alternatives) { 4896 } else { 3272 }
        $pair = [Drawing.Bitmap]::new($width, 1160)
        $graphics = [Drawing.Graphics]::FromImage($pair)
        try {
            if ($left.Width -ne 1600 -or $left.Height -ne 900 -or $right.Size -ne $left.Size -or
                ($Alternatives -and $extra.Size -ne $left.Size)) {
                throw "$($map.Level) has unexpected render dimensions."
            }
            $graphics.Clear($background)
            $graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
            $graphics.DrawString("$($map.Level)  $($map.Name)", $titleFont, $white, 24, 15)
            $artBrush = if ($map.Art -eq 'GREYBOX') { $amber } else { $muted }
            $artWidth = $graphics.MeasureString($map.Art, $noteFont).Width
            $graphics.DrawString($map.Art, $noteFont, $artBrush, ($width - 24 - $artWidth), 28)
            $graphics.DrawString('1.0×   Hiện tại', $scaleFont, $white, 24, 95)
            $proposalLabel = if ($Alternatives) { 'Thử trước' } else { 'Đề xuất' }
            $proposalBrush = if ($Alternatives) { $white } else { $accent }
            $graphics.DrawString("$($map.Scale)×   $proposalLabel", $scaleFont, $proposalBrush, 1648, 95)
            $graphics.DrawImageUnscaled($left, 24, 165)
            $graphics.DrawImageUnscaled($right, 1648, 165)
            $graphics.DrawString($map.Reason, $noteFont, $muted, 24, 1091)
            $name = "$($map.Level)-comparison.png"
            if ($Alternatives) {
                $graphics.DrawString("$($map.Alternative)×   Thử mới", $scaleFont, $accent, 3272, 95)
                $graphics.DrawImageUnscaled($extra, 3272, 165)
                $name = "$($map.Level)-compare-$($map.Alternative).png"
            }
            $pair.Save((Join-Path $PSScriptRoot $name), [Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $graphics.Dispose()
            $pair.Dispose()
            $left.Dispose()
            $right.Dispose()
            if ($extra) { $extra.Dispose() }
        }
    }
} finally {
    foreach ($resource in @($white, $muted, $accent, $amber, $titleFont, $scaleFont, $noteFont)) {
        $resource.Dispose()
    }
}
