$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "QlyPhongKham.csproj"
$publishDir = Join-Path $PSScriptRoot "artifacts\publish"
$archivePath = Join-Path $PSScriptRoot "artifacts\QlyPhongKham-win-x64.zip"

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Path (Split-Path -Parent $publishDir) -Force | Out-Null

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (Test-Path -LiteralPath (Join-Path $publishDir "dbconfig.txt")) {
    throw "Refusing to package dbconfig.txt. Remove local database credentials from the publish output and try again."
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "init_database.sql") -Destination $publishDir
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README-DISTRIBUTION.md") -Destination $publishDir
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README.md") -Destination $publishDir

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $archivePath -CompressionLevel Optimal
Write-Host "Created package: $archivePath"
