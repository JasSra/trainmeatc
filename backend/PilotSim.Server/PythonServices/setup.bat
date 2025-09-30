@echo off
REM Setup script for Coqui TTS dependencies on Windows
REM Run this once to install required Python packages for TTS

setlocal

set "SCRIPT_DIR=%~dp0"
set "VENV_DIR=%SCRIPT_DIR%.venv"

echo === Coqui TTS Setup ===
echo This will install Python dependencies for text-to-speech
echo.

REM Check if Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python 3 is not installed
    echo Please install Python 3.10 or later from https://www.python.org/
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('python --version') do set PYTHON_VERSION=%%i
echo Found: %PYTHON_VERSION%

REM Create virtual environment if it doesn't exist
if not exist "%VENV_DIR%" (
    echo Creating virtual environment...
    python -m venv "%VENV_DIR%"
) else (
    echo Virtual environment already exists
)

REM Activate virtual environment
echo Activating virtual environment...
call "%VENV_DIR%\Scripts\activate.bat"

REM Upgrade pip
echo Upgrading pip...
python -m pip install --upgrade pip

REM Install dependencies
echo Installing Coqui TTS and dependencies...
echo This may take a few minutes on first run...
pip install -r "%SCRIPT_DIR%requirements.txt"

echo.
echo === Setup Complete ===
echo Coqui TTS is now ready to use!
echo.
echo To use TTS in your ASP.NET application:
echo 1. The application will automatically call Python with the virtual environment
echo 2. Models will be downloaded automatically on first use (~150MB)
echo 3. Subsequent runs will be faster as models are cached
echo.
echo Virtual environment location: %VENV_DIR%
echo.
pause
