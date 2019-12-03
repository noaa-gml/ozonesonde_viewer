@ECHO OFF

REM ##########################################################################################
set execPath=bin\Release
set execName=Ozonesonde Viewer 2019.exe
set progName=Ozonesonde Viewer 2019
REM ##########################################################################################


REM read the version number of the executable, uses code from http://stackoverflow.com/questions/6697878/get-the-file-version-of-a-dll-or-exe
REM note that this depends on the cmd line utility "sigcheck", check above link for download
FOR /F "tokens=1-3" %%i IN ('C:\Users\jordan\cmd_tools\sigcheck.exe "%execPath%\%execName%"') DO ( IF "%%i %%j"=="File version:" SET filever=%%k )

REM strip trailing space
set filever=%filever:~0,-1%

set outDir=%progName% %filever%
ECHO "outdir: %outDir%"

mkdir "%outDir%"
copy "%execPath%\%execName%" "%outDir%"
copy "%execPath%\%execName%.config" "%outDir%"
copy "%execPath%\*.dll" "%outDir%"
copy "%execPath%\bright_rainbow.csv" "%outDir%"
copy "%execPath%\instrument selection graphic.png" "%outDir%"
copy "%execPath%\pumpEff.xml" "%outDir%"
