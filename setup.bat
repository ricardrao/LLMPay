@echo off
REM LLMPay Quick Start Script for Windows

echo.
echo ============================================
echo   LLMPay Project Setup
echo ============================================
echo.

REM Check .NET SDK
echo [1/5] Checking .NET SDK...
dotnet --version
if errorlevel 1 (
    echo Error: .NET SDK not found!
    exit /b 1
)

REM Restore packages
echo.
echo [2/5] Restoring NuGet packages...
dotnet restore

REM Build solution
echo.
echo [3/5] Building solution...
dotnet build

REM Create database
echo.
echo [4/5] Creating database migrations...
cd LLMPay.Api
dotnet ef migrations add InitialCreate --project ../LLMPay.Infrastructure
dotnet ef database update --project ../LLMPay.Infrastructure
cd ..

echo.
echo [5/5] Setup complete!
echo.
echo ============================================
echo   To start the API:
echo   cd LLMPay.Api && dotnet run
echo ============================================
echo.
echo API will be available at:
echo   https://localhost:5001
echo   http://localhost:5000
echo.
echo Swagger UI: https://localhost:5001/swagger
echo.

pause
