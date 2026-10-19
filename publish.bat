@echo off
rem Publishes KS Fit Remote (unofficial) locally and adds a Start menu shortcut.
rem   publish.bat            (re)install to %LOCALAPPDATA%\Programs\KsFitRemote, replacing any older version
rem   publish.bat uninstall  remove the install folder and shortcut
setlocal
set "ROOT=%~dp0"
set "DEST=%LOCALAPPDATA%\Programs\KsFitRemote"
set "STAGE=%LOCALAPPDATA%\Programs\KsFitRemote.new"
set "LNK=%APPDATA%\Microsoft\Windows\Start Menu\Programs\KS Fit Remote (Unofficial).lnk"
set "OLDLNK=%APPDATA%\Microsoft\Windows\Start Menu\Programs\KS Fit Remote.lnk"

tasklist /fi "imagename eq KsFitRemote.exe" 2>nul | find /i "KsFitRemote.exe" >nul
if not errorlevel 1 (
  echo KS Fit Remote is running. Close it first so it can disconnect from the pad cleanly.
  exit /b 1
)

if /i "%~1"=="uninstall" (
  if exist "%LNK%" del "%LNK%"
  if exist "%OLDLNK%" del "%OLDLNK%"
  if exist "%DEST%" rmdir /s /q "%DEST%"
  echo Removed KS Fit Remote.
  exit /b 0
)

where dotnet >nul 2>nul || (echo .NET SDK not found. Install .NET 8 SDK or newer. & exit /b 1)

rem Model photos are not in git; fetch them if Pillow is available and they are missing.
if not exist "%ROOT%app\KsFit.App\Assets\devices\kingsmith-z1.png" (
  python -c "import PIL" >nul 2>nul && python "%ROOT%tools\fetch_device_images.py"
)

rem Publish to a staging folder first so a failed build never removes a working install.
if exist "%STAGE%" rmdir /s /q "%STAGE%"
echo Publishing ...
dotnet publish "%ROOT%app\KsFit.App\KsFit.App.csproj" -c Release -r win-x64 --self-contained false -o "%STAGE%" --nologo -v q
if errorlevel 1 (echo Publish failed. The existing install was left untouched. & rmdir /s /q "%STAGE%" 2>nul & exit /b 1)

if exist "%DEST%" (
  echo Removing older version ...
  rmdir /s /q "%DEST%"
  if exist "%DEST%" (echo Could not remove the old install folder. Is a file in it open? & rmdir /s /q "%STAGE%" 2>nul & exit /b 1)
)
if exist "%OLDLNK%" del "%OLDLNK%"
move "%STAGE%" "%DEST%" >nul
if errorlevel 1 (echo Could not move the new build into place. & exit /b 1)

powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%tools\install-shortcut.ps1" -Target "%DEST%\KsFitRemote.exe" -Shortcut "%LNK%"
if errorlevel 1 (echo Could not create the Start menu shortcut. & exit /b 1)

echo.
echo Installed to %DEST%
echo Press the Windows key and type "KS Fit Remote".
exit /b 0
