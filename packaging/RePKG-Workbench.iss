#ifndef AppVersion
  #error AppVersion must be provided by Build-Release.ps1
#endif

#ifndef SourceDir
  #error SourceDir must be provided by Build-Release.ps1
#endif

#ifndef OutputDir
  #error OutputDir must be provided by Build-Release.ps1
#endif

#define AppName "RePKG Workbench"
#define AppExeName "RePKG-Workbench.exe"

[Setup]
AppId={{38CE4B3A-11A7-48CF-B25B-CFF98EF2AA1C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=RePKG Workbench Contributors
AppCopyright=Copyright (C) 2025-2026 RePKG Workbench Contributors
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=RePKG-Workbench-Setup-x64
SetupIconFile=..\src\RePKG.App\Resources\icon.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
VersionInfoVersion={#AppVersion}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[CustomMessages]
english.DesktopRuntimeRequired=.NET 10 Desktop Runtime (x64) is required. Download it from Microsoft now?
english.DesktopRuntimeInstallFirst=Install .NET 10 Desktop Runtime, then run this installer again.
chinesesimplified.DesktopRuntimeRequired=需要安装 .NET 10 Desktop Runtime (x64)。是否立即从 Microsoft 下载？
chinesesimplified.DesktopRuntimeInstallFirst=请先安装 .NET 10 Desktop Runtime，然后重新运行此安装程序。

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  DesktopRuntimeRegistryKey = 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  DotnetSharedHostRegistryKey = 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost';
  DesktopRuntimeDownloadUrl = 'https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe';

function IsVersion10(const Version: String): Boolean;
begin
  Result := Pos('10.', Version) = 1;
end;

function HasDesktopRuntimeAtDotnetRoot(const DotnetRoot: String): Boolean;
var
  FindRec: TFindRec;
  RuntimeDirectory: String;
begin
  Result := False;
  if DotnetRoot = '' then
    Exit;

  RuntimeDirectory := AddBackslash(DotnetRoot) + 'shared\Microsoft.WindowsDesktop.App';
  if not DirExists(RuntimeDirectory) then
    Exit;

  if FindFirst(AddBackslash(RuntimeDirectory) + '*', FindRec) then
  begin
    try
      repeat
        if ((FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0) and
           IsVersion10(FindRec.Name) then
        begin
          Result := True;
          Exit;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function IsDesktopRuntimeInstalled(): Boolean;
var
  DotnetRoot: String;
  RuntimeVersions: TArrayOfString;
  Index: Integer;
begin
  if RegQueryStringValue(HKLM64, DotnetSharedHostRegistryKey, 'Path', DotnetRoot) and
     HasDesktopRuntimeAtDotnetRoot(DotnetRoot) then
  begin
    Result := True;
    Exit;
  end;

  if HasDesktopRuntimeAtDotnetRoot(ExpandConstant('{pf64}\dotnet')) or
     HasDesktopRuntimeAtDotnetRoot(ExpandConstant('{localappdata}\Microsoft\dotnet')) or
     HasDesktopRuntimeAtDotnetRoot(GetEnv('DOTNET_ROOT_X64')) or
     HasDesktopRuntimeAtDotnetRoot(GetEnv('DOTNET_ROOT')) then
  begin
    Result := True;
    Exit;
  end;

  Result := False;
  if RegGetSubkeyNames(HKLM64, DesktopRuntimeRegistryKey, RuntimeVersions) then
  begin
    for Index := 0 to GetArrayLength(RuntimeVersions) - 1 do
    begin
      if IsVersion10(RuntimeVersions[Index]) then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := IsDesktopRuntimeInstalled();
  if Result then
    Exit;

  if MsgBox(
    ExpandConstant('{cm:DesktopRuntimeRequired}'),
    mbConfirmation,
    MB_YESNO) = IDYES then
  begin
    ShellExec('open', DesktopRuntimeDownloadUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;

  MsgBox(ExpandConstant('{cm:DesktopRuntimeInstallFirst}'), mbInformation, MB_OK);
end;
