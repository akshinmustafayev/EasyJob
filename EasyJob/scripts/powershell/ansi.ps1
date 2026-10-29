# Raw ANSI escapes are rendered as-is, like in a terminal.
$e = [char]27
Write-Host "${e}[1mbold${e}[0m ${e}[4munderline${e}[0m ${e}[31mred${e}[0m ${e}[38;5;208morange (256)${e}[0m ${e}[38;2;120;200;255mtruecolor${e}[0m"
Write-Host 'Write-Host colors' -ForegroundColor Green
Write-Host 'on a background' -ForegroundColor Black -BackgroundColor Yellow
