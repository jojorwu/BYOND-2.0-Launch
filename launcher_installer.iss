[Setup]
AppName=BYOND 2.0 Launcher
AppVersion=1.0
DefaultDirName={pf}\\BYOND 2.0 Launcher
DefaultGroupName=BYOND 2.0 Launcher
OutputBaseFilename=BYOND_2.0_Launcher_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
OutputDir=.\installer
UninstallDisplayIcon={app}\Launcher.exe

[Files]
Source: "Launcher\bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\BYOND 2.0 Launcher"; Filename: "{app}\Launcher.exe"
Name: "{commondesktop}\BYOND 2.0 Launcher"; Filename: "{app}\Launcher.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:";
