param(
    [string]$ApkPath = "build/android/generation-roguelite.apk",
    [string]$PackageName = "com.example.generationroguelite",
    [string]$TempRoot = "D:\temp",
    [switch]$LaunchAfterInstall
)

$ErrorActionPreference = "Stop"

function Resolve-AdbPath {
    $candidates = @(
        "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
        "$env:ANDROID_SDK_ROOT\platform-tools\adb.exe",
        "$env:ANDROID_HOME\platform-tools\adb.exe",
        "$env:USERPROFILE\AppData\Local\Android\Sdk\platform-tools\adb.exe"
    )

    foreach ($candidate in $candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    $fromPath = (Get-Command adb -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source)
    if ($fromPath) {
        return $fromPath
    }

    throw "adb.exe not found. Please install Android SDK platform-tools and set path."
}

function Initialize-TempEnvironment {
    param(
        [string]$Root,
        [string]$SubFolder = "generation-roguelite"
    )

    if ([string]::IsNullOrWhiteSpace($Root)) {
        throw "TempRoot is empty. Specify a writable directory such as D:\temp."
    }

    if (-not (Test-Path $Root)) {
        New-Item -ItemType Directory -Path $Root -Force | Out-Null
    }

    $resolvedRoot = (Resolve-Path $Root).Path
    $tempWorkDir = Join-Path $resolvedRoot $SubFolder
    if (-not (Test-Path $tempWorkDir)) {
        New-Item -ItemType Directory -Path $tempWorkDir -Force | Out-Null
    }

    $env:TEMP = $tempWorkDir
    $env:TMP = $tempWorkDir
    $env:TMPDIR = $tempWorkDir

    return $tempWorkDir
}

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot
$tempWorkDir = Initialize-TempEnvironment -Root $TempRoot

$adb = Resolve-AdbPath
$resolvedApk = if ([System.IO.Path]::IsPathRooted($ApkPath)) { $ApkPath } else { Join-Path $projectRoot $ApkPath }

if (-not (Test-Path $resolvedApk)) {
    throw "APK not found: $resolvedApk`nExport Android APK first, then run this script again."
}

$deviceLines = & $adb devices -l | Select-Object -Skip 1 | Where-Object { $_ -match "\S" }
$onlineDevices = @($deviceLines | Where-Object { $_ -match "\sdevice\s" })
if ($onlineDevices.Count -eq 0) {
    throw "No online Android device found (or unauthorized/offline)."
}

Write-Host "adb: $adb"
Write-Host "target apk: $resolvedApk"
Write-Host "connected device(s):"
$onlineDevices | ForEach-Object { Write-Host "  $_" }

& $adb install -r "$resolvedApk"
if ($LASTEXITCODE -ne 0) {
    throw "adb install failed."
}

if ($LaunchAfterInstall) {
    & $adb shell monkey -p $PackageName -c android.intent.category.LAUNCHER 1 | Out-Null
    Write-Host "Launch attempted: $PackageName"
}

Write-Host "Install completed"
Write-Host "temp: $tempWorkDir"
