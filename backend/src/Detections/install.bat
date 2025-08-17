@echo off
echo =====================================
echo AI Detection Service Installation
echo =====================================
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.8+ from https://python.org
    pause
    exit /b 1
)

echo Python found. Checking version...
python -c "import sys; exit(0 if sys.version_info >= (3, 8) else 1)"
if errorlevel 1 (
    echo ERROR: Python 3.8+ is required
    python --version
    pause
    exit /b 1
)

echo Creating virtual environment...
python -m venv venv
if errorlevel 1 (
    echo ERROR: Failed to create virtual environment
    pause
    exit /b 1
)

echo Activating virtual environment...
call venv\Scripts\activate

echo Installing Python dependencies...
pip install --upgrade pip setuptools wheel
pip install -r requirements.txt
if errorlevel 1 (
    echo ERROR: Failed to install dependencies
    pause
    exit /b 1
)

echo Testing installation...
python -c "import cv2, ultralytics, numpy; print('All dependencies installed successfully!')"
if errorlevel 1 (
    echo WARNING: Some dependencies may have issues
)

echo Creating environment configuration...
if not exist .env (
    copy .env.example .env
    echo Created .env file from template
    echo Please edit .env file to configure your settings
) else (
    echo .env file already exists
)

echo.
echo =====================================
echo Installation completed successfully!
echo =====================================
echo.
echo To run the AI Detection Service:
echo 1. Activate virtual environment: venv\Scripts\activate
echo 2. Configure settings in .env file
echo 3. Run: python src\main.py
echo.
pause