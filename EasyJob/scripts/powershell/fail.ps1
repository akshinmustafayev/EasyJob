Write-Host "About to fail with exit code 2..."
Write-Error "Something went wrong" -ErrorAction Continue
exit 2
