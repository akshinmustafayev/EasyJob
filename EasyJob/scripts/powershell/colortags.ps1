# EasyJob colour tags: \cNNEJ opens a colour, /cNNEJ closes it. Plus raw ANSI escapes.
Write-Host '\c01EJyellow-green/c01EJ  \c04EJcrimson/c04EJ  \c07EJblue/c07EJ  \c12EJgreen/c12EJ'
$e = [char]27
Write-Host "${e}[1mbold${e}[0m ${e}[4munderline${e}[0m ${e}[31mred${e}[0m ${e}[38;5;208morange (256)${e}[0m ${e}[38;2;120;200;255mtruecolor${e}[0m"
