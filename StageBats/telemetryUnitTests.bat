@echo off

:: Si queremos hacer esto super seguro, habría que comprobar si Visual Studio está instalado
set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe
set SOLUTION_DIR=%~dp0..\TelemetrySystem\TelemetrySystem\Telemetry.sln
set BIN_DIR=%~dp0..\TelemetrySystem\TelemetrySystem\bin\Debug

"%MSBUILD_PATH%" "%SOLUTION_DIR%" -p:Configuration=Debug -p:Platform=x64

"%BIN_DIR%\TestTelemetry.exe"

if %ERRORLEVEL% EQU 0 (
    echo Todos los test han salido bien.
    exit /b 0
) else (
    echo Los test no han salido bien.
    exit /b 1
)
