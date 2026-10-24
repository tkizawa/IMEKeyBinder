# IMEKeyBinder スタンドアロンインストーラ自動ビルドスクリプト
# 実行環境のアーキテクチャ（x64 / arm64）に合わせて自己完結型EXEを発行し、
# Inno Setup を用いて .\Installer フォルダ配下にインストーラを生成します。

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "=== IMEKeyBinder インストーラビルド開始 ===" -ForegroundColor Cyan

$rootDir = $PSScriptRoot
Set-Location $rootDir

# 1. 実行環境のアーキテクチャ判定
$osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
$rid = switch ($osArch) {
    "arm64" { "win-arm64" }
    default { "win-x64" }
}
Write-Host "[1/4] ターゲットアーキテクチャ: $rid ($osArch)" -ForegroundColor Green

# 2. app.ico の存在確認
if (-not (Test-Path "$rootDir\app.ico")) {
    Write-Error "app.ico が見つかりません。"
    exit 1
}
Write-Host "[2/4] アプリアイコン確認完了" -ForegroundColor Green

# 3. .NET 10 自己完結型（SingleFile）バイナリの発行
Write-Host "[3/4] .NET 10 自己完結型バイナリ ($rid) を発行中..." -ForegroundColor Yellow
dotnet publish "$rootDir\IMEKeyBinder.csproj" `
    -c $Configuration `
    -r $rid `
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

# 4. Inno Setup コンパイラ (ISCC.exe) の探索
Write-Host "[4/4] Inno Setup コンパイラを探索中..." -ForegroundColor Yellow
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

# 5. インストーラのビルド実行
Write-Host "スタンドアロンインストーラを .\Installer フォルダに生成中..." -ForegroundColor Yellow
if (-not (Test-Path "$rootDir\Installer")) {
    New-Item -ItemType Directory -Path "$rootDir\Installer" | Out-Null
}

& $iscc "$rootDir\installer.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup によるインストーラ生成に失敗しました。"
    exit $LASTEXITCODE
}

$outputExe = Get-ChildItem "$rootDir\Installer\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "🎉 ビルド完了！スタンドアロンインストーラ:" -ForegroundColor Green
Write-Host "ファイルパス: $($outputExe.FullName)" -ForegroundColor White
Write-Host "ファイルサイズ: $([math]::Round($outputExe.Length / 1MB, 2)) MB" -ForegroundColor White
Write-Host "==========================================" -ForegroundColor Cyan
