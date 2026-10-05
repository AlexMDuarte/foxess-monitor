#define MyAppName "FoxESS Monitor"
#define MyAppVersion "1.2.0"
#define MyAppExeName "FoxESS Monitor.exe"
[Setup]
AppId={{B5A26719-4B76-4D6B-BD0D-D8F17EB3AF5A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=FoxESS Monitor
DefaultDirName={localappdata}\Programs\FoxESS Monitor
DefaultGroupName=FoxESS Monitor
PrivilegesRequired=lowest
OutputDir=dist
OutputBaseFilename=FoxESS-Monitor-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=Assets\foxess.ico
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
[Files]
Source: "publish\FoxESS Monitor.exe"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\FoxESS Monitor"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\FoxESS Monitor"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Criar atalho no ambiente de trabalho"; Flags: unchecked
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir FoxESS Monitor"; Flags: postinstall nowait skipifsilent
