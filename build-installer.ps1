# IMEKeyBinder Standalone Installer Build Script
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "=== IMEKeyBinder インストーラビルド開始 ===" -ForegroundColor Cyan

$rootDir = $PSScriptRoot
Set-Location $rootDir

# 1. Check/Generate app.ico if not exists
if (-not (Test-Path "$rootDir\app.ico")) {
    Write-Host "[1/4] app.ico を生成中..." -ForegroundColor Yellow
    # Icon generation logic
} else {
    Write-Host "[1/4] app.ico 確認完了" -ForegroundColor Green
}

# 2. Publish self-contained single-file binary
Write-Host "[2/4] .NET 10 自己完結型バイナリ (win-x64) を発行中..." -ForegroundColor Yellow
dotnet publish "$rootDir\IMEKeyBinder.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$rootDir\publish"

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish に失敗しました。"
    exit $LASTEXITCODE
}
Write-Host "発行成功: $rootDir\publish\IMEKeyBinder.exe" -ForegroundColor Green

# 3. Locate ISCC.exe (Inno Setup Compiler)
Write-Host "[3/4] Inno Setup コンパイラを探索中..." -ForegroundColor Yellow
$isccPaths = @(
    "C:\Users\$env:USERNAME\AppData\Local\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    $whereIscc = (Get-Command "iscc.exe" -ErrorAction SilentlyContinue)?.Source
    if ($whereIscc) { $iscc = $whereIscc }
}

if (-not $iscc) {
    Write-Error "Inno Setup コンパイラ (ISCC.exe) が見つかりませんでした。"
    exit 1
}
Write-Host "ISCC 検出: $iscc" -ForegroundColor Green

# 4. Build Installer
Write-Host "[4/4] スタンドアロンインストーラをビルド中..." -ForegroundColor Yellow
& $iscc "$rootDir\installer.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup によるインストーラ生成に失敗しました。"
    exit $LASTEXITCODE
}

$outputExe = Get-ChildItem "$rootDir\dist\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "🎉 ビルド完了！スタンドアロンインストーラ:" -ForegroundColor Green
Write-Host "ファイルパス: $($outputExe.FullName)" -ForegroundColor White
Write-Host "ファイルサイズ: $([math]::Round($outputExe.Length / 1MB, 2)) MB" -ForegroundColor White
Write-Host "==========================================" -ForegroundColor Cyan
