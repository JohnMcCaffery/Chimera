cd Bin

Chimera.exe

IF %errorlevel% EQU -1073741819 (
GOTO exit
) ELSE ( 
IF %errorlevel% EQU 0 GOTO exit ELSE GOTO restart
)

GOTO :EOF

:restart
cd ..
launch.bat
GOTO :EOF

:exit

shutdown.exe /s /t 00

GOTO :EOF
