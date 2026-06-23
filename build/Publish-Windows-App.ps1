param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $root "src\Western Union Automation Task\Western Union Automation Task.csproj"
$publishDir = Join-Path $root "deployment\Western Union Automation Task-win-x64"

Write-Host "Cleaning old deployment folder..."
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Write-Host "Restoring NuGet packages..."
dotnet restore (Join-Path $root "Western Union Automation Task.sln")

Write-Host "Running unit tests..."
dotnet test (Join-Path $root "Western Union Automation Task.sln") --configuration $Configuration --no-restore

Write-Host "Publishing Windows desktop application..."
dotnet publish $appProject `
    --configuration $Configuration `
    --framework net8.0-windows `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $publishDir

Write-Host "Copying sample input file..."
$inputDir = Join-Path $publishDir "Input"
$outputDir = Join-Path $publishDir "Output"
New-Item -ItemType Directory -Force -Path $inputDir | Out-Null
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
Copy-Item (Join-Path $root "Input\ParaBank users.csv") $inputDir -Force

Write-Host "Deployment package created: $publishDir"
Write-Host "Run "Western Union Automation Task.exe" from that folder."
