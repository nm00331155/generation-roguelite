param(
    [string]$RawApkPath = "build/android/generation-roguelite.apk",
    [string]$SignedApkPath = "build/android/generation-roguelite-signed.apk",
    [string]$PackageName = "com.example.generationroguelite",
    [string]$TempRoot = "D:\temp",
    [switch]$LaunchAfterInstall
)

$ErrorActionPreference = "Stop"

function Resolve-ToolPath {
    param([string[]]$Candidates, [string]$CommandName)

    foreach ($candidate in $Candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    $fromPath = (Get-Command $CommandName -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source)
    if ($fromPath) {
        return $fromPath
    }

    throw "$CommandName not found."
}

function Resolve-KeytoolPath {
    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($env:JAVA_HOME)) {
        $candidates += Join-Path $env:JAVA_HOME "bin\keytool.exe"
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GRADLE_JAVA_HOME)) {
        $candidates += Join-Path $env:GRADLE_JAVA_HOME "bin\keytool.exe"
    }

    return Resolve-ToolPath -Candidates $candidates -CommandName "keytool"
}

function Ensure-DebugKeystore {
    param(
        [string]$KeystorePath,
        [string]$KeytoolPath
    )

    if (Test-Path $KeystorePath) {
        return
    }

    $keystoreDir = Split-Path -Parent $KeystorePath
    if (-not (Test-Path $keystoreDir)) {
        New-Item -ItemType Directory -Path $keystoreDir -Force | Out-Null
    }

    & $KeytoolPath -genkeypair -v -keystore "$KeystorePath" -storepass android -alias androiddebugkey -keypass android -dname "CN=Android Debug,O=Android,C=US" -keyalg RSA -keysize 2048 -validity 10000 -noprompt
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $KeystorePath)) {
        throw "Failed to create debug keystore: $KeystorePath"
    }

    Write-Host "Created debug keystore: $KeystorePath"
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

    $javaTmpOpt = "-Djava.io.tmpdir=$tempWorkDir"
    if ([string]::IsNullOrWhiteSpace($env:JAVA_TOOL_OPTIONS)) {
        $env:JAVA_TOOL_OPTIONS = $javaTmpOpt
    }
    elseif ($env:JAVA_TOOL_OPTIONS -notmatch "-Djava\.io\.tmpdir=") {
        $env:JAVA_TOOL_OPTIONS = "$javaTmpOpt $env:JAVA_TOOL_OPTIONS"
    }

    return $tempWorkDir
}

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot
$tempWorkDir = Initialize-TempEnvironment -Root $TempRoot

$adb = Resolve-ToolPath -Candidates @(
    "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    "$env:ANDROID_SDK_ROOT\platform-tools\adb.exe",
    "$env:ANDROID_HOME\platform-tools\adb.exe"
) -CommandName "adb"

$zipalign = Resolve-ToolPath -Candidates @(
    "$env:LOCALAPPDATA\Android\Sdk\build-tools\34.0.0\zipalign.exe",
    "$env:LOCALAPPDATA\Android\Sdk\build-tools\35.0.0\zipalign.exe"
) -CommandName "zipalign"

$apksigner = Resolve-ToolPath -Candidates @(
    "$env:LOCALAPPDATA\Android\Sdk\build-tools\34.0.0\apksigner.bat",
    "$env:LOCALAPPDATA\Android\Sdk\build-tools\35.0.0\apksigner.bat"
) -CommandName "apksigner"

$keytool = Resolve-KeytoolPath

$rawApk = if ([System.IO.Path]::IsPathRooted($RawApkPath)) { $RawApkPath } else { Join-Path $projectRoot $RawApkPath }
$signedApk = if ([System.IO.Path]::IsPathRooted($SignedApkPath)) { $SignedApkPath } else { Join-Path $projectRoot $SignedApkPath }
$alignedApk = [System.IO.Path]::ChangeExtension($signedApk, ".aligned.apk")
$keystore = Join-Path $env:USERPROFILE ".android\debug.keystore"

if (-not (Test-Path $rawApk)) {
    throw "Raw APK not found: $rawApk"
}

Ensure-DebugKeystore -KeystorePath $keystore -KeytoolPath $keytool

$targetDir = Split-Path -Parent $signedApk
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir | Out-Null
}

& $zipalign -f 4 "$rawApk" "$alignedApk"
if ($LASTEXITCODE -ne 0) {
    throw "zipalign failed."
}

& $apksigner sign --ks "$keystore" --ks-key-alias androiddebugkey --ks-pass pass:android --key-pass pass:android --out "$signedApk" "$alignedApk"
if ($LASTEXITCODE -ne 0) {
    throw "apksigner failed."
}

& $adb install -r "$signedApk"
if ($LASTEXITCODE -ne 0) {
    throw "adb install failed."
}

if ($LaunchAfterInstall) {
    & $adb shell monkey -p $PackageName -c android.intent.category.LAUNCHER 1 | Out-Null
}

Write-Host "Signed and installed: $signedApk"
Write-Host "temp: $tempWorkDir"
