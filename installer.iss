[Setup]
AppName=Quality Diagnostics Center LabSystem
AppVersion=2.0
DefaultDirName={autopf}\Quality Diagnostics Center
DefaultGroupName=Quality Diagnostics Center
OutputDir=Output
OutputBaseFilename=LabSystem-Setup-v2
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=LabSystem.UI\logo.ico

[Files]
Source: "Publish-v2\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Quality Diagnostics Center LabSystem"; Filename: "{app}\LabSystem.UI.exe"
Name: "{autodesktop}\Quality Diagnostics Center LabSystem"; Filename: "{app}\LabSystem.UI.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\LabSystem.UI.exe"; Description: "{cm:LaunchProgram,Quality Diagnostics Center LabSystem}"; Flags: nowait postinstall skipifsilent
