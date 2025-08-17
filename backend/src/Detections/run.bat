@echo off
echo Starting AI Detection Service...
echo.

REM Check if virtual environment exists
if not exist venv\Scripts\activate.bat (
    echo ERROR: Virtual environment not found
    echo Please run install.bat first
    pause
    exit /b 1
)

REM Activate virtual environment
call venv\Scripts\activate

REM Check if .env file exists
if not exist .env (
    echo WARNING: .env file not found
    echo Creating from template...
    copy .env.example .env
    echo Please edit .env file to configure your settings
    notepad .env
)

echo Starting AI Detection Service...
echo Press Ctrl+C to stop
echo.

python src\main.py

echo.
echo AI Detection Service stopped
pause