param(
    [string]$GodotExe = "",
    [string]$JdkHome = "",
    [string]$PresetName = "Android",
    [string]$ApkPath = "build/android/generation-roguelite.apk",
    [string]$PackageName = "com.example.generationroguelite",
    [string]$TempRoot = "D:\temp",
    [int]$IdleTimeoutSec = 20,
    [int]$ExportTimeoutSec = 1800,
    [switch]$LaunchAfterInstall
)

$ErrorActionPreference = "Stop"

function Resolve-GodotPath {
    param([string]$Hint)

    if (-not [string]::IsNullOrWhiteSpace($Hint) -and (Test-Path $Hint)) {
        return $Hint
    }

    $fromPath = Get-Command godot, godot4 -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
    if ($fromPath) {
        return $fromPath
    }

    $candidates = @(
        "$env:ProgramFiles\Godot\Godot_v4.6-stable_mono_win64.exe",
        "$env:ProgramFiles\Godot\Godot_v4.6-stable_win64.exe",
        "$env:USERPROFILE\Downloads\Godot_v4.6-stable_mono_win64.exe",
        "$env:USERPROFILE\Downloads\Godot_v4.6-stable_win64.exe"
    )

    foreach ($candidate in $candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    throw "Godot executable not found. Specify it with -GodotExe."
}

function Resolve-JdkHome {
    param([string]$Hint)

    if (-not [string]::IsNullOrWhiteSpace($Hint) -and (Test-Path (Join-Path $Hint "bin\java.exe"))) {
        return $Hint
    }

    $candidates = @(
        "C:\Program Files\Android\Android Studio\jbr",
        "C:\Program Files\Java\jdk-21",
        "C:\Program Files\Java\jdk-17",
        "$env:ProgramFiles\Microsoft\jdk-21*",
        "$env:ProgramFiles\Microsoft\jdk-17*",
        "$env:LOCALAPPDATA\Programs\Microsoft\jdk*"
    )

    foreach ($candidate in $candidates) {
        $resolved = Get-ChildItem -Path $candidate -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($resolved -and (Test-Path (Join-Path $resolved.FullName "bin\java.exe"))) {
            return $resolved.FullName
        }

        if (Test-Path (Join-Path $candidate "bin\java.exe")) {
            return $candidate
        }
    }

    throw "Compatible JDK not found. Specify with -JdkHome (JDK 17 or 21)."
}

function Resolve-ApkSignerPath {
    $roots = @(
        "$env:LOCALAPPDATA\Android\Sdk\build-tools",
        "$env:ANDROID_SDK_ROOT\build-tools",
        "$env:ANDROID_HOME\build-tools"
    )

    foreach ($root in $roots) {
        if ([string]::IsNullOrWhiteSpace($root) -or -not (Test-Path $root)) {
            continue
        }

        $buildToolDirs = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue |
            Sort-Object -Property Name -Descending

        foreach ($dir in $buildToolDirs) {
            $candidateBat = Join-Path $dir.FullName "apksigner.bat"
            if (Test-Path $candidateBat) {
                return $candidateBat
            }

            $candidateExe = Join-Path $dir.FullName "apksigner.exe"
            if (Test-Path $candidateExe) {
                return $candidateExe
            }
        }
    }

    $fromPath = Get-Command apksigner -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
    if ($fromPath) {
        return $fromPath
    }

    return ""
}

function Test-ApkHasSignatures {
    param(
        [string]$ApkPath,
        [string]$ApkSignerPath
    )

    if ([string]::IsNullOrWhiteSpace($ApkPath) -or -not (Test-Path $ApkPath)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($ApkSignerPath) -or -not (Test-Path $ApkSignerPath)) {
        return $null
    }

    $hasNativePref = $null -ne (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue)
    if ($hasNativePref) {
        $previousNativePref = $PSNativeCommandUseErrorActionPreference
        $PSNativeCommandUseErrorActionPreference = $false
    }

    try {
        & $ApkSignerPath verify --print-certs "$ApkPath" *> $null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
    finally {
        if ($hasNativePref) {
            $PSNativeCommandUseErrorActionPreference = $previousNativePref
        }
    }
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

    $gradleUserHome = Join-Path $tempWorkDir "gradle-user-home"
    if (-not (Test-Path $gradleUserHome)) {
        New-Item -ItemType Directory -Path $gradleUserHome -Force | Out-Null
    }

    $env:TEMP = $tempWorkDir
    $env:TMP = $tempWorkDir
    $env:TMPDIR = $tempWorkDir
    $env:GRADLE_USER_HOME = $gradleUserHome

    $javaTmpOpt = "-Djava.io.tmpdir=$tempWorkDir"
    if ([string]::IsNullOrWhiteSpace($env:JAVA_TOOL_OPTIONS)) {
        $env:JAVA_TOOL_OPTIONS = $javaTmpOpt
    }
    elseif ($env:JAVA_TOOL_OPTIONS -notmatch "-Djava\.io\.tmpdir=") {
        $env:JAVA_TOOL_OPTIONS = "$javaTmpOpt $env:JAVA_TOOL_OPTIONS"
    }

    return $tempWorkDir
}

function Get-ProcessTreeSnapshot {
    param([int]$RootProcessId)

    $childrenByParent = @{}
    $all = Get-CimInstance -ClassName Win32_Process -Property ProcessId, ParentProcessId -ErrorAction SilentlyContinue
    foreach ($entry in $all) {
        $parentId = [int]$entry.ParentProcessId
        if (-not $childrenByParent.ContainsKey($parentId)) {
            $childrenByParent[$parentId] = New-Object System.Collections.Generic.List[int]
        }
        $childrenByParent[$parentId].Add([int]$entry.ProcessId)
    }

    $queue = New-Object System.Collections.Generic.Queue[int]
    $visited = New-Object System.Collections.Generic.HashSet[int]
    $queue.Enqueue($RootProcessId)

    $pids = New-Object System.Collections.Generic.List[int]
    while ($queue.Count -gt 0) {
        $currentPid = $queue.Dequeue()
        if (-not $visited.Add($currentPid)) {
            continue
        }

        $pids.Add($currentPid)
        if ($childrenByParent.ContainsKey($currentPid)) {
            foreach ($childPid in $childrenByParent[$currentPid]) {
                if (-not $visited.Contains($childPid)) {
                    $queue.Enqueue($childPid)
                }
            }
        }
    }

    $cpuSeconds = 0.0
    if ($pids.Count -gt 0) {
        $procObjects = Get-Process -Id $pids.ToArray() -ErrorAction SilentlyContinue
        foreach ($proc in @($procObjects)) {
            if ($null -ne $proc.CPU) {
                $cpuSeconds += [double]$proc.CPU
            }
        }
    }

    $fingerprint = (($pids.ToArray() | Sort-Object) -join ",")
    return @{
        CpuSeconds = $cpuSeconds
        Fingerprint = $fingerprint
    }
}

function Invoke-ProcessWithIdleTimeout {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [int]$IdleTimeoutSec = 20,
        [int]$TotalTimeoutSec = 1800,
        [int]$PollSec = 2
    )

    $stdoutFile = Join-Path $env:TEMP ("copilot_stdout_{0}.log" -f ([guid]::NewGuid().ToString("N")))
    $stderrFile = Join-Path $env:TEMP ("copilot_stderr_{0}.log" -f ([guid]::NewGuid().ToString("N")))

    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru -NoNewWindow -RedirectStandardOutput $stdoutFile -RedirectStandardError $stderrFile

        $startedAt = Get-Date
        $lastProgressAt = $startedAt
        $lastLength = 0L
        $snapshot = Get-ProcessTreeSnapshot -RootProcessId $process.Id
        $lastCpuSeconds = [double]$snapshot.CpuSeconds
        $lastFingerprint = [string]$snapshot.Fingerprint

        while (-not $process.HasExited) {
            Start-Sleep -Seconds $PollSec

            $outLen = if (Test-Path $stdoutFile) { (Get-Item $stdoutFile).Length } else { 0L }
            $errLen = if (Test-Path $stderrFile) { (Get-Item $stderrFile).Length } else { 0L }
            $currentLength = $outLen + $errLen
            $currentSnapshot = Get-ProcessTreeSnapshot -RootProcessId $process.Id
            $currentCpuSeconds = [double]$currentSnapshot.CpuSeconds
            $currentFingerprint = [string]$currentSnapshot.Fingerprint
            $hasCpuProgress = ($currentCpuSeconds - $lastCpuSeconds) -gt 0.05
            $hasProcessTreeProgress = $currentFingerprint -ne $lastFingerprint

            if ($currentLength -ne $lastLength) {
                $lastLength = $currentLength
                $lastProgressAt = Get-Date
            }

            if ($hasCpuProgress -or $hasProcessTreeProgress) {
                $lastProgressAt = Get-Date
                $lastCpuSeconds = $currentCpuSeconds
                $lastFingerprint = $currentFingerprint
            }

            $idleSeconds = ((Get-Date) - $lastProgressAt).TotalSeconds
            if ($idleSeconds -ge $IdleTimeoutSec) {
                try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}

                $tailOutput = @()
                if (Test-Path $stdoutFile) { $tailOutput += Get-Content $stdoutFile -Tail 40 -ErrorAction SilentlyContinue }
                if (Test-Path $stderrFile) { $tailOutput += Get-Content $stderrFile -Tail 40 -ErrorAction SilentlyContinue }
                $tailText = ($tailOutput -join [Environment]::NewLine)

                return @{
                    ExitCode = -1
                    Output = $tailText
                    TimedOut = $true
                    TimeoutReason = "idle"
                }
            }

            $elapsedSeconds = ((Get-Date) - $startedAt).TotalSeconds
            if ($elapsedSeconds -ge $TotalTimeoutSec) {
                try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}

                $tailOutput = @()
                if (Test-Path $stdoutFile) { $tailOutput += Get-Content $stdoutFile -Tail 40 -ErrorAction SilentlyContinue }
                if (Test-Path $stderrFile) { $tailOutput += Get-Content $stderrFile -Tail 40 -ErrorAction SilentlyContinue }
                $tailText = ($tailOutput -join [Environment]::NewLine)

                return @{
                    ExitCode = -1
                    Output = $tailText
                    TimedOut = $true
                    TimeoutReason = "total"
                }
            }
        }

        $stdoutText = if (Test-Path $stdoutFile) { Get-Content $stdoutFile -Raw -ErrorAction SilentlyContinue } else { "" }
        $stderrText = if (Test-Path $stderrFile) { Get-Content $stderrFile -Raw -ErrorAction SilentlyContinue } else { "" }
        $combined = ($stdoutText + [Environment]::NewLine + $stderrText).Trim()

        if (-not [string]::IsNullOrWhiteSpace($combined)) {
            Write-Output $combined
        }

        return @{
            ExitCode = $process.ExitCode
            Output = $combined
            TimedOut = $false
            TimeoutReason = ""
        }
    }
    finally {
        Remove-Item -Path $stdoutFile -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $stderrFile -Force -ErrorAction SilentlyContinue
    }
}

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$resolvedApkPath = if ([System.IO.Path]::IsPathRooted($ApkPath)) {
    $ApkPath
} else {
    Join-Path $projectRoot $ApkPath
}

$signedApkPath = Join-Path (Split-Path -Parent $resolvedApkPath) (
    "{0}-signed{1}" -f [System.IO.Path]::GetFileNameWithoutExtension($resolvedApkPath), [System.IO.Path]::GetExtension($resolvedApkPath)
)

$godot = Resolve-GodotPath -Hint $GodotExe
$jdk = Resolve-JdkHome -Hint $JdkHome
$tempWorkDir = Initialize-TempEnvironment -Root $TempRoot

$env:JAVA_HOME = $jdk
$env:GRADLE_JAVA_HOME = $jdk
$env:PATH = "$(Join-Path $jdk 'bin');$env:PATH"

$daemonDir = Join-Path $projectRoot ".gradle-export\daemon"
if (Test-Path $daemonDir) {
    Remove-Item -Path $daemonDir -Recurse -Force -ErrorAction SilentlyContinue
}

$gradleWrapper = Join-Path $projectRoot "android/build/gradlew.bat"
if (Test-Path $gradleWrapper) {
    try {
        & $gradleWrapper --stop | Out-Null
    }
    catch {
        Write-Warning "Failed to stop Gradle daemons proactively. Continuing export."
    }
}

$androidBuildOutputDir = Join-Path $projectRoot "android/build/build"
for ($attempt = 1; $attempt -le 3; $attempt++) {
    if (-not (Test-Path $androidBuildOutputDir)) {
        break
    }

    try {
        Remove-Item -Path $androidBuildOutputDir -Recurse -Force -ErrorAction Stop
        break
    }
    catch {
        if ($attempt -eq 3) {
            Write-Warning "Could not fully clean android/build/build before export. Build may fail if files remain locked."
        }
        else {
            Start-Sleep -Seconds 2
        }
    }
}

Write-Host "godot: $godot"
Write-Host "jdk: $jdk"
Write-Host "temp: $tempWorkDir"

$godotArgs = @(
    "--headless",
    "--path", "$projectRoot",
    "--export-debug", "$PresetName",
    "$ApkPath"
)

$apkWriteTimeBefore = $null
if (Test-Path $resolvedApkPath) {
    $apkWriteTimeBefore = (Get-Item $resolvedApkPath).LastWriteTimeUtc
}

$exportResult = Invoke-ProcessWithIdleTimeout -FilePath $godot -Arguments $godotArgs -IdleTimeoutSec $IdleTimeoutSec -TotalTimeoutSec $ExportTimeoutSec
$exportExit = [int]$exportResult.ExitCode
$exportTimedOut = [bool]$exportResult.TimedOut
$exportTimeoutReason = [string]$exportResult.TimeoutReason
$hasKnownEditorSettingsError = $false
if (-not [string]::IsNullOrWhiteSpace($exportResult.Output)) {
    $hasKnownEditorSettingsError = $exportResult.Output -match 'EditorSettings not instantiated yet when getting setting "export/android/shutdown_adb_on_exit"'
}

$hasExplicitExportFailure = $false
if (-not [string]::IsNullOrWhiteSpace($exportResult.Output)) {
    $hasExplicitExportFailure = $exportResult.Output -match 'BUILD FAILED|Project export for preset "Android" failed|ERROR: Project export for preset'
}

$apkUpdatedDuringRun = $false
if (Test-Path $resolvedApkPath) {
    $apkWriteTimeAfter = (Get-Item $resolvedApkPath).LastWriteTimeUtc
    if ($null -eq $apkWriteTimeBefore) {
        $apkUpdatedDuringRun = $true
    }
    else {
        $apkUpdatedDuringRun = $apkWriteTimeAfter -gt $apkWriteTimeBefore
    }
}

if ($exportTimedOut) {
    if ($hasKnownEditorSettingsError -and $apkUpdatedDuringRun -and -not $hasExplicitExportFailure) {
        Write-Warning "Godot timed out during shutdown (known EditorSettings issue), but APK was updated. Continuing with install."
        $exportExit = 1
    }
    else {
        $timeoutLabel = if ([string]::IsNullOrWhiteSpace($exportTimeoutReason)) { "idle" } else { $exportTimeoutReason }
        $tailText = if ([string]::IsNullOrWhiteSpace($exportResult.Output)) { "(no output captured)" } else { $exportResult.Output }
        throw "Godot export timed out (${timeoutLabel}) with no reliable success signal. Last output:`n$tailText"
    }
}

if ($exportExit -ne 0 -and -not (Test-Path $resolvedApkPath)) {
    throw "Godot export failed."
}

if ($exportExit -ne 0 -and (Test-Path $resolvedApkPath)) {
    if ($hasKnownEditorSettingsError) {
        Write-Warning "Godot exited non-zero due known EditorSettings shutdown_adb_on_exit issue, but APK was generated. Continuing with install."
    }
    else {
        Write-Warning "Godot returned non-zero exit code ($exportExit), but APK was generated. Continuing with install."
    }
}

$installScript = Join-Path $PSScriptRoot "install_android.ps1"
if (-not (Test-Path $installScript)) {
    throw "install_android.ps1 not found: $installScript"
}

$apksignerForVerify = Resolve-ApkSignerPath
$rawApkHasSignatures = Test-ApkHasSignatures -ApkPath $resolvedApkPath -ApkSignerPath $apksignerForVerify

$directInstallSucceeded = $false
if ($rawApkHasSignatures -eq $true) {
    try {
        & $installScript -ApkPath $ApkPath -PackageName $PackageName -TempRoot $TempRoot -LaunchAfterInstall:$LaunchAfterInstall
        $directInstallSucceeded = $true
    }
    catch {
        Write-Warning "Direct install failed. Falling back to sign-and-install path."
    }
}
elseif ($rawApkHasSignatures -eq $false) {
    Write-Host "Unsigned APK detected. Skipping direct install and using sign-and-install path."
}
else {
    try {
        & $installScript -ApkPath $ApkPath -PackageName $PackageName -TempRoot $TempRoot -LaunchAfterInstall:$LaunchAfterInstall
        $directInstallSucceeded = $true
    }
    catch {
        Write-Warning "Could not verify APK signature in advance and direct install failed. Falling back to sign-and-install path."
    }
}

if ($directInstallSucceeded) {
    return
}

$signScript = Join-Path $PSScriptRoot "sign_and_install.ps1"
if (-not (Test-Path $signScript)) {
    throw "install_android.ps1 failed, and sign_and_install.ps1 was not found."
}

& $signScript -RawApkPath $ApkPath -SignedApkPath $signedApkPath -PackageName $PackageName -TempRoot $TempRoot -LaunchAfterInstall:$LaunchAfterInstall
