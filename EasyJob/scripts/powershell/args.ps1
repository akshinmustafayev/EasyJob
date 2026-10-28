# Takes two answers from the argument dialog: a name and a repeat count.
param(
    [string]$Name = "world",
    [int]$Count = 3
)
Write-Host "Arguments received: Name='$Name' Count=$Count"
1..$Count | ForEach-Object { Write-Host "Hello, $Name! ($_ of $Count)" }
