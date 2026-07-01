[Setup]
AppName=Quality Diagnostics Center LabSystem
AppVersion=2.0
DefaultDirName={autopf}\Quality Diagnostics Center
DefaultGroupName=Quality Diagnostics Center
OutputDir=Output
OutputBaseFilename=LabSystem-Setup-v2
Compression=lzma
SolidCompression=yes
MinVersion=6.1sp1
SetupIconFile=LabSystem.UI\logo.ico
WizardSmallImageFile=LabSystem.UI\logo.png

[Files]
Source: "Publish-v2\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Quality Diagnostics Center LabSystem"; Filename: "{app}\LabSystem.UI.exe"
Name: "{autodesktop}\Quality Diagnostics Center LabSystem"; Filename: "{app}\LabSystem.UI.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\LabSystem.UI.exe"; Description: "{cm:LaunchProgram,Quality Diagnostics Center LabSystem}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet451Installed: Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
  begin
    if Release >= 378675 then
      Result := True;
  end;
end;

function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsDotNet451Installed then
  begin
    if MsgBox('This application requires .NET Framework 4.5.1 or later.' + #13#10#13#10 +
              'Would you like to open the browser to download .NET Framework 4.5.1?', mbConfirmation, MB_YESNO) = idYes then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet-framework/net451', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
  end;
end;
