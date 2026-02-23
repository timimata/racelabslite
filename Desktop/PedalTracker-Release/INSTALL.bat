@echo off
echo ========================================
echo PedalTracker v1.0.1 - Instalador
echo ========================================
echo.
echo Verificando .NET Runtime...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo .NET Runtime nao encontrado!
    echo Por favor, instale .NET 10.0 Runtime de:
    echo https://dotnet.microsoft.com/download/dotnet/10.0
    echo.
    pause
    exit /b 1
)
echo .NET Runtime encontrado!
echo.
echo ========================================
echo Para executar o programa:
echo   Interface Grafica: PedalTracker.Ui.exe
echo   Modo Consola: PedalTracker.exe
echo ========================================
echo.
pause
