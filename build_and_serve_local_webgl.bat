@echo off
setlocal
cd /d "%~dp0"

set "PORT=8090"
if not "%~1"=="" set "PORT=%~1"

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

echo [1/2] Building WebGL Local Dev...
"%UNITY_EXE%" -batchmode -quit -projectPath "%CD%" -executeMethod WebGLBuildAutomation.BuildLocalDev -logFile "%CD%\Logs\webgl_build_local.log"
if not "%ERRORLEVEL%"=="0" (
  echo [ERROR] Build failed. See log:
  echo %CD%\Logs\webgl_build_local.log
  pause
  exit /b 1
)

echo [2/2] Starting local server...
call "%CD%\serve_webgl.bat" %PORT%

endlocal
