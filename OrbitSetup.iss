; Orbit / Homelab Control Center - Inno Setup installer
; Build first:
; dotnet publish HomelabControlCenter.sln -c Release -r win-x64 --self-contained true

#ifndef SourceDir
  #define SourceDir "HomelabControlCenter\bin\Release\net10.0-windows\win-x64\publish"
#endif

#define AppName "Homelab Control Center"
#define AppVersion "1.0.0"
#define AppPublisher "DevLabStudy"
#define AppExeName "HomelabControlCenter.exe"

[Setup]
AppId={{B1A2F8C0-4D0A-4C92-9D30-0B17CC000001}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://devlabstudy.ovh
AppSupportURL=https://devlabstudy.ovh
AppUpdatesURL=https://github.com/DevLabStudy/HomelabControlCenter
DefaultDirName={autopf}\DevLabStudy\HomelabControlCenter
DefaultGroupName=DevLabStudy\Homelab Control Center
OutputDir=artifacts\installer
OutputBaseFilename=HomelabControlCenter-Setup
SetupIconFile=HomelabControlCenter\devlab_icon.ico
UninstallDisplayIcon={app}\{#AppExeName}
ArchitecturesInstallIn64BitMode=x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
DisableProgramGroupPage=yes

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Utwórz skrót na pulpicie"; GroupDescription: "Dodatkowe skróty:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Uruchom {#AppName}"; Flags: nowait postinstall skipifsilent
