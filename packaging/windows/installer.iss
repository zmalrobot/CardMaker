; Inno Setup Script for CardMaker Desktop
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\publish\win-x64"
#endif

[Setup]
AppId={{C4RD-M4K3R-D35KT0P-V2-0001}}
AppName=CardMaker
AppVersion={#AppVersion}
AppPublisher=CardMaker
DefaultDirName={autopf}\CardMaker
DefaultGroupName=CardMaker
AllowNoIcons=yes
OutputDir=..\..\dist
OutputBaseFilename=CardMaker-Setup-x64
SetupIconFile=..\..\src\CardMaker.Desktop\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\CardMaker.exe

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\CardMaker"; Filename: "{app}\CardMaker.exe"
Name: "{group}\{cm:UninstallProgram,CardMaker}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\CardMaker"; Filename: "{app}\CardMaker.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CardMaker.exe"; Description: "{cm:LaunchProgram,CardMaker}"; Flags: nowait postinstall skipifsilent

