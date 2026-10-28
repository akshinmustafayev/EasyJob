# Basic smoke test: normal output, colours, a warning and an error.
Write-Host "Hello from PowerShell $($PSVersionTable.PSVersion) on $($PSVersionTable.OS)"
Write-Host "Green text" -ForegroundColor Green
Write-Host "Yellow on blue" -ForegroundColor Yellow -BackgroundColor DarkBlue
Write-Output "Plain output (stdout)"
Write-Warning "This is a warning"
Write-Error "This is an error (stderr)" -ErrorAction Continue
Write-Host "Working directory: $(Get-Location)"
Write-Host "Done."
