<#
.SYNOPSIS
  Builds EasyJob for several operating systems / CPUs and zips every build for distribution.

.DESCRIPTION
  Windows PowerShell 5.1+ or PowerShell 7 (also works on macOS/Linux with pwsh). .NET cross-compiles every target
  from any host. Variants (the zip name says which one):
    EasyJob-<version>-<rid>-portable.zip   runs on its own, the .NET runtime is included (bigger)
    EasyJob-<version>-<rid>.zip            small, needs the .NET 10 runtime installed on the machine
  Results: build\dist\*.zip (the publish folders are kept in build\out\).
  macOS targets need 'bash' on PATH (Git for Windows or WSL) because the .app bundle is made by tools/make-macos-app.sh;
  the bundle is only ad-hoc signed when built on a Mac.

.EXAMPLE
  .\build\build.ps1
  .\build\build.ps1 -Rids win-x64,osx-arm64 -Variants portable
  .\build\build.ps1 -Version 2.1.0 -Clean
#>
param(
    [string[]] $Rids = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64'),
    [ValidateSet('portable', 'framework')]
    [string[]] $Variants = @('portable', 'framework'),
    [string] $Version,
    [switch] $Clean
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$Here = $PSScriptRoot
$Root = Split-Path $Here -Parent
$Project = Join-Path $Root 'EasyJob\EasyJob.csproj'
$Out = Join-Path $Here 'out'
$Dist = Join-Path $Here 'dist'

if (-not $Version) { $Version = ([xml](Get-Content $Project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1 }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'The dotnet SDK 10 was not found on PATH.' }

if ($Clean) { Remove-Item $Out, $Dist -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force $Out, $Dist | Out-Null

# Zips a folder. Entries listed in $Executable get Unix mode 755, so the zip also works on macOS/Linux.
function New-Zip([string] $SourceParent, [string] $EntryName, [string] $ZipPath, [string[]] $Executable) {
    if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
    $zip = [System.IO.Compression.ZipFile]::Open($ZipPath, 'Create')
    try {
        $base = Join-Path $SourceParent $EntryName
        foreach ($file in Get-ChildItem $base -Recurse -File -Force) {
            $rel = $file.FullName.Substring($SourceParent.Length).TrimStart('\', '/').Replace('\', '/')
            $entry = [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $rel, 'Optimal')
            $mode = if ($Executable -contains $rel) { 0x81ED } else { 0x81A4 }   # 0100755 / 0100644
            $entry.ExternalAttributes = $mode -shl 16
        }
    } finally { $zip.Dispose() }
}

Write-Host "EasyJob $Version  |  platforms: $($Rids -join ' ')  |  variants: $($Variants -join ' ')"

foreach ($rid in $Rids) {
    foreach ($variant in $Variants) {
        $selfContained = if ($variant -eq 'portable') { 'true' } else { 'false' }
        $suffix = if ($variant -eq 'portable') { '-portable' } else { '' }
        Write-Host "`n== $rid ($variant)"

        if ($rid -like 'osx-*' -and -not (Get-Command bash -ErrorAction SilentlyContinue)) {
            Write-Warning "Skipping $rid: 'bash' is needed to make the .app bundle (install Git for Windows or WSL)."
            continue
        }

        $publish = Join-Path $Out "$rid-$variant"
        if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
        dotnet publish $Project -c Release -r $rid --self-contained $selfContained `
            "-p:Version=$Version" -p:DebugType=None -p:DebugSymbols=false -o $publish --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid ($variant)" }

        $stage = Join-Path $Out "stage-$rid-$variant"
        if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
        New-Item -ItemType Directory -Force $stage | Out-Null
        $zipFile = Join-Path $Dist "EasyJob-$Version-$rid$suffix.zip"

        if ($rid -like 'osx-*') {
            $toBash = { param($p) ($p -replace '\\', '/') -replace '^([A-Za-z]):', { '/' + $_.Groups[1].Value.ToLower() } }
            $script = & $toBash (Join-Path $Root 'tools\make-macos-app.sh')
            bash $script (& $toBash $publish) (& $toBash (Join-Path $stage 'EasyJob.app')) $Version | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "make-macos-app.sh failed for $rid" }
            New-Zip $stage 'EasyJob.app' $zipFile @('EasyJob.app/Contents/MacOS/EasyJob')
        } else {
            $dir = Join-Path $stage 'EasyJob'
            New-Item -ItemType Directory -Force $dir | Out-Null
            Copy-Item (Join-Path $publish '*') $dir -Recurse -Force
            Copy-Item (Join-Path $Root 'LICENSE.txt'), (Join-Path $Root 'README.md'), (Join-Path $Root 'policy.example.json') $dir
            New-Zip $stage 'EasyJob' $zipFile @('EasyJob/EasyJob')
        }
        Remove-Item $stage -Recurse -Force
        Write-Host ("   -> {0} ({1:N1} MB)" -f $zipFile, ((Get-Item $zipFile).Length / 1MB))
    }
}

Write-Host "`nDone. Zips are in $Dist"
