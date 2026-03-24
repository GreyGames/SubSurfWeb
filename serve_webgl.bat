@echo off
setlocal
cd /d "%~dp0"

set "BUILD_DIR=Build\WebGL"
if exist "Build\WebGL\index.html" goto build_found
if exist "Build\index.html" set "BUILD_DIR=Build"

:build_found
if not exist "%BUILD_DIR%\index.html" (
  echo [ERROR] Could not find WebGL build index.html
  echo.
  echo Build your project first in Unity:
  echo   File ^> Build Settings ^> WebGL ^> Build
  echo Recommended output folder:
  echo   Build\WebGL
  echo.
  pause
  exit /b 1
)

set "PORT=8090"
if not "%~1"=="" set "PORT=%~1"

set "PY_CMD="
where py >nul 2>&1 && set "PY_CMD=py"
if not defined PY_CMD (
  where python >nul 2>&1 && set "PY_CMD=python"
)

if not defined PY_CMD (
  echo [ERROR] Python launcher not found. Install Python first.
  pause
  exit /b 1
)

echo Serving "%BUILD_DIR%" on http://127.0.0.1:%PORT%/index.html
start "" "http://127.0.0.1:%PORT%/index.html"

%PY_CMD% serve_webgl.py --dir "%BUILD_DIR%" --port %PORT%

endlocal
