# IMEKeyBinder スタンドアロンインストーラ自動ビルドスクリプト
# 指定されたアーキテクチャ（x64 / arm64 / all）に合わせて自己完結型EXEを発行し、
# Inno Setup を用いて .\Installer フォルダ配下にインストーラを生成します。

param(
    [ValidateSet("all", "x64", "arm64")]
    [string]$Arch = "all",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "=== IMEKeyBinder インストーラビルド開始 ===" -ForegroundColor Cyan

$rootDir = $PSScriptRoot
Set-Location $rootDir

# 1. バージョン番号の取得 (IMEKeyBinder.csproj から抽出)
$csprojPath = "$rootDir\IMEKeyBinder.csproj"
[xml]$csprojXml = Get-Content $csprojPath
$appVersion = $csprojXml.Project.PropertyGroup.Version
if (-not $appVersion) {
    $appVersion = "1.0.0.0"
}
Write-Host "[1/5] アプリケーションバージョン: $appVersion" -ForegroundColor Green

# 2. 対象アーキテクチャの決定
$targets = if ($Arch -eq "all") {
    @("x64", "arm64")
} else {
    @($Arch)
}
Write-Host "[2/5] ビルド対象アーキテクチャ: $($targets -join ', ')" -ForegroundColor Green

# 3. app.ico の存在確認
if (-not (Test-Path "$rootDir\app.ico")) {
    Write-Error "app.ico が見つかりません。"
    exit 1
}
Write-Host "[3/5] アプリアイコン確認完了" -ForegroundColor Green

# 4. Inno Setup コンパイラ (ISCC.exe) の探索
Write-Host "[4/5] Inno Setup コンパイラを探索中..." -ForegroundColor Yellow
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

# 5. 各アーキテクチャごとに発行およびインストーラ生成
if (-not (Test-Path "$rootDir\Installer")) {
    New-Item -ItemType Directory -Path "$rootDir\Installer" | Out-Null
}

$createdFiles = @()

foreach ($target in $targets) {
    $rid = "win-$target"
    $publishDir = "$rootDir\publish\$rid"
    
    Write-Host "`n--- [$target] .NET 10 自己完結型バイナリ ($rid) 発行中... ---" -ForegroundColor Yellow
    dotnet publish "$csprojPath" `
        -c $Configuration `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $publishDir

    if ($LASTEXITCODE -ne 0) {
        Write-Error "[$target] dotnet publish に失敗しました。"
        exit $LASTEXITCODE
    }
    Write-Host "発行成功: $publishDir\IMEKeyBinder.exe" -ForegroundColor Green

    Write-Host "--- [$target] Inno Setup によるインストーラ生成中... ---" -ForegroundColor Yellow
    & $iscc `
        "/DMyAppVersion=$appVersion" `
        "/DAppArch=$target" `
        "/DSourceDir=publish\$rid" `
        "$rootDir\installer.iss"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "[$target] Inno Setup によるインストーラ生成に失敗しました。"
        exit $LASTEXITCODE
    }

    $expectedExeName = "IMEKeyBinder_Setup_v${appVersion}_${target}.exe"
    $outputExePath = "$rootDir\Installer\$expectedExeName"
    if (Test-Path $outputExePath) {
        $createdFiles += (Get-Item $outputExePath)
    }
}

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "🎉 ビルド完了！スタンドアロンインストーラ:" -ForegroundColor Green
foreach ($file in $createdFiles) {
    Write-Host "  [$($file.Name)]" -ForegroundColor Yellow
    Write-Host "    パス: $($file.FullName)" -ForegroundColor White
    Write-Host "    サイズ: $([math]::Round($file.Length / 1MB, 2)) MB" -ForegroundColor White
}
Write-Host "==========================================" -ForegroundColor Cyan
