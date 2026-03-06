@echo off
chcp 65001 >nul

set GODOT_PATH=D:\code_workspace\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64.exe
set PROJECT_PATH=D:\code_workspace\generation-roguelite\generation-roguelite
set APK_RAW=%PROJECT_PATH%\exports\game.apk
set APK_ALIGNED=%PROJECT_PATH%\exports\game-aligned.apk
set APK_SIGNED=%PROJECT_PATH%\exports\game-signed.apk
set KEYSTORE=%USERPROFILE%\.android\debug.keystore
set BUILD_TOOLS=C:\Users\mikami\AppData\Local\Android\Sdk\build-tools\34.0.0
set PACKAGE_NAME=com.example.generationroguelite
set TEMP_ROOT=D:\temp\generation-roguelite

if not exist "%TEMP_ROOT%" mkdir "%TEMP_ROOT%"
set TEMP=%TEMP_ROOT%
set TMP=%TEMP_ROOT%
set JAVA_TOOL_OPTIONS=-Djava.io.tmpdir=%TEMP_ROOT%
set GRADLE_USER_HOME=%TEMP_ROOT%\gradle-user-home
if not exist "%GRADLE_USER_HOME%" mkdir "%GRADLE_USER_HOME%"

if not exist "%PROJECT_PATH%\exports" mkdir "%PROJECT_PATH%\exports"

echo [1/6] Checking device...
adb devices | findstr "device$" >nul
if errorlevel 1 (
    echo ERROR: No device found.
    pause
    exit /b 1
)
echo OK

echo [2/6] Building APK...
"%GODOT_PATH%" --headless --path "%PROJECT_PATH%" --export-debug "Android" "%APK_RAW%"
if not exist "%APK_RAW%" (
    echo ERROR: APK not generated.
    pause
    exit /b 1
)
echo OK

echo [3/6] Aligning...
"%BUILD_TOOLS%\zipalign.exe" -f 4 "%APK_RAW%" "%APK_ALIGNED%"
if errorlevel 1 (
    echo ERROR: zipalign failed.
    pause
    exit /b 1
)
echo OK

echo [4/6] Signing...
call "%BUILD_TOOLS%\apksigner.bat" sign --ks "%KEYSTORE%" --ks-key-alias androiddebugkey --ks-pass pass:android --key-pass pass:android --out "%APK_SIGNED%" "%APK_ALIGNED%"
if errorlevel 1 (
    echo ERROR: apksigner failed.
    pause
    exit /b 1
)
echo OK

echo [5/6] Installing...
adb install -r "%APK_SIGNED%"
if errorlevel 1 (
    echo ERROR: install failed.
    pause
    exit /b 1
)

echo [6/6] Launching...
adb shell monkey -p %PACKAGE_NAME% -c android.intent.category.LAUNCHER 1

echo Done!
pause
