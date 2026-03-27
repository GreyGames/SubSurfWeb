@echo off
setlocal
cd /d "%~dp0"

set "OUTPUT_DIR=Build\WebGL"
set "DEPLOY_DIR=docs"

for /f "tokens=2 delims=: " %%v in ('findstr /b "m_EditorVersion:" "ProjectSettings\ProjectVersion.txt"') do set "UNITY_VERSION=%%v"
if not defined UNITY_VERSION (
  echo [ERROR] Could not read Unity version from ProjectSettings\ProjectVersion.txt
  pause
  exit /b 1
)

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not exist "%UNITY_EXE%" (
  echo [ERROR] Unity not found at:
  echo %UNITY_EXE%
  pause
  exit /b 1
)

tasklist /FI "IMAGENAME eq Unity.exe" | find /I "Unity.exe" >nul
if %ERRORLEVEL%==0 (
  echo [ERROR] Unity is already running.
  echo Close all Unity windows, then run this script again.
  pause
  exit /b 1
)

if not exist "Logs" mkdir Logs

echo Building WebGL RELEASE (Sub-10MB preset)...
"%UNITY_EXE%" -batchmode -quit -projectPath "%CD%" -executeMethod WebGLBuildAutomation.BuildRelease -logFile "%CD%\Logs\webgl_build_release.log"
if not "%ERRORLEVEL%"=="0" (
  echo [ERROR] Build failed. See log:
  echo %CD%\Logs\webgl_build_release.log
  pause
  exit /b 1
)

if not exist "%OUTPUT_DIR%\index.html" (
  echo [ERROR] Build output not found at %OUTPUT_DIR%
  pause
  exit /b 1
)

set "HAS_BROTLI="
dir /b "%OUTPUT_DIR%\Build\*.br" >nul 2>&1 && set "HAS_BROTLI=1"
if not defined HAS_BROTLI (
  dir /b "%OUTPUT_DIR%\Build\*.unityweb" >nul 2>&1 && set "HAS_BROTLI=1"
)

if not defined HAS_BROTLI (
  echo [ERROR] Release build does not contain Brotli-compressed build artifacts.
  echo Check PlayerSettings.WebGL.compressionFormat and build logs.
  pause
  exit /b 1
)

if not exist "%DEPLOY_DIR%" mkdir "%DEPLOY_DIR%"

call :sync_dir "%OUTPUT_DIR%\Build" "%DEPLOY_DIR%\Build"
if errorlevel 1 (
  pause
  exit /b 1
)

call :sync_dir "%OUTPUT_DIR%\TemplateData" "%DEPLOY_DIR%\TemplateData"
if errorlevel 1 (
  pause
  exit /b 1
)

copy /Y "%OUTPUT_DIR%\index.html" "%DEPLOY_DIR%\index.html" >nul
if errorlevel 1 (
  echo [ERROR] Failed to copy %OUTPUT_DIR%\index.html to %DEPLOY_DIR%\index.html
  pause
  exit /b 1
)

echo [OK] Release build complete and synced to %DEPLOY_DIR% with Brotli assets.
pause
endlocal
exit /b 0

:sync_dir
set "SRC=%~1"
set "DST=%~2"
if not exist "%SRC%" (
  echo [ERROR] Missing required folder: %SRC%
  exit /b 1
)

robocopy "%SRC%" "%DST%" /MIR /R:2 /W:1 /NFL /NDL /NJH /NJS /NP >nul
if %ERRORLEVEL% GEQ 8 (
  echo [ERROR] Failed to sync %SRC% to %DST%
  exit /b 1
)

exit /b 0
