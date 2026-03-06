@echo off
chcp 65001 >nul

echo ========================================
echo   Build, Sign, Deploy - Generation Roguelite
echo ========================================

rem === Paths (adjust if needed) ===
set GODOT=D:\code_workspace\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64.exe
set PROJECT=D:\code_workspace\generation-roguelite\generation-roguelite
set APK_RAW=%PROJECT%\exports\game.apk
set APK_ALIGNED=%PROJECT%\exports\game-aligned.apk
set APK_SIGNED=%PROJECT%\exports\game-signed.apk
set KEYSTORE=%USERPROFILE%\.android\debug.keystore
set BUILD_TOOLS=C:\Users\mikami\AppData\Local\Android\Sdk\build-tools\34.0.0
set PACKAGE=com.yourname.inheritance

rem === 1. Device check ===
echo [1/5] Checking device...
adb devices 2>nul | findstr /R "device$" >nul
if errorlevel 1 (
    echo ERROR: Android device not found. Connect USB and enable USB debugging.
    pause
    exit /b 1
)
echo OK: Device connected

rem === 2. Build APK ===
echo [2/5] Building APK...
if not exist "%PROJECT%\exports" mkdir "%PROJECT%\exports"
"%GODOT%" --headless --path "%PROJECT%" --export-debug "Android" "%APK_RAW%"
if not exist "%APK_RAW%" (
    echo ERROR: APK was not generated. Check Godot export settings.
    pause
    exit /b 1
)
echo OK: Build complete

rem === 3. Align ===
echo [3/5] Aligning APK...
"%BUILD_TOOLS%\zipalign.exe" -f 4 "%APK_RAW%" "%APK_ALIGNED%"
if errorlevel 1 (
    echo ERROR: zipalign failed.
    pause
    exit /b 1
)
echo OK: Aligned

rem === 4. Sign ===
echo [4/5] Signing APK...
call "%BUILD_TOOLS%\apksigner.bat" sign --ks "%KEYSTORE%" --ks-key-alias androiddebugkey --ks-pass pass:android --key-pass pass:android --out "%APK_SIGNED%" "%APK_ALIGNED%"
if errorlevel 1 (
    echo ERROR: Signing failed.
    pause
    exit /b 1
)
echo OK: Signed

rem === 5. Install and Launch ===
echo [5/5] Installing and launching...
adb install -r "%APK_SIGNED%"
if errorlevel 1 (
    echo ERROR: Install failed.
    pause
    exit /b 1
)
adb shell monkey -p %PACKAGE% -c android.intent.category.LAUNCHER 1 2>nul
echo.
echo ========================================
echo   Deploy complete! Check your device.
echo ========================================
pause
