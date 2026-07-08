param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "dist"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$solution = Join-Path $root "TrimbleBatteryMonitor.sln"
$publishDir = Join-Path $root "publish"
$installerDir = Join-Path $root "installer\TrimbleBatteryMonitor.Installer"
$wxsFile = Join-Path $installerDir "Product.wxs"
$msiOutput = Join-Path $root $OutputDir

Write-Host "Publishing application..."
dotnet publish (Join-Path $root "src\TrimbleBatteryMonitor\TrimbleBatteryMonitor.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force -Path $msiOutput | Out-Null

Write-Host "Building MSI with WiX..."
wix build $wxsFile `
    -d PublishDir=$publishDir `
    -o (Join-Path $msiOutput "TrimbleBatteryMonitor.msi")

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "MSI created at $(Join-Path $msiOutput 'TrimbleBatteryMonitor.msi')"
