@echo off
setlocal
cd /d "%~dp0"

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

echo [OK] Release build complete at Build\WebGL
pause
endlocal
