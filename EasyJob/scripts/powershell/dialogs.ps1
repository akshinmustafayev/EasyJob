# Demo of EasyJobPSTools: native dialogs from a script (Windows, macOS, Linux).
Import-Module (Join-Path $PSScriptRoot '../../EasyJobPSTools/EasyJobPSTools.psd1') -Force

Write-Host "Dialog backend: $(Get-EJDialogBackend)"

$name = Show-EJInputBox -Header 'EasyJob' -Text 'What is your name?' -AllowEmptyResult $false -DefaultValue $env:USER
if (-not $name) { Write-Host 'Cancelled.'; exit 0 }
Write-Host "Hello, $name"

$env = Show-EJSelectItem -Header 'EasyJob' -Text 'Pick an environment' -Items 'dev', 'staging', 'prod'
Write-Host "Environment: $env"

if (Show-EJConfirm -Header 'EasyJob' -Text "Pretend to deploy to $env?" -DefaultNo) {
    Show-EJNotification -Title 'EasyJob' -Text "Deployed to $env"
    Show-EJMessageBox -Header 'EasyJob' -Text "Deployed to $env (not really)."
}
else {
    Write-Host 'Skipped.'
}
