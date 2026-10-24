; IMEKeyBinder Inno Setup Script
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0.0"
#endif
#ifndef AppArch
  #define AppArch "x64"
#endif
#ifndef SourceDir
  #define SourceDir "publish\win-x64"
#endif

#define MyAppName "IMEKeyBinder"
#define MyAppPublisher "IMEKeyBinder"
#define MyAppExeName "IMEKeyBinder.exe"
#define MyAppMutex "Global\IMEKeyBinder_SingleInstance_Mutex_6B7E1B1A"

[Setup]
AppId={{E68A8D8F-7C90-4826-BD52-9CE59D90F74C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Installer
OutputBaseFilename=IMEKeyBinder_Setup_v{#MyAppVersion}_{#AppArch}
SetupIconFile=app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Architecture
#if LowerCase(AppArch) == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

; Allow user to choose install for current user or all users (administrator)
PrivilegesRequiredOverridesAllowed=dialog
PrivilegesRequired=lowest

; Handle running instance during install / uninstall
AppMutex={#MyAppMutex}
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}

; Uninstall info in Control Panel
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} v{#MyAppVersion} ({#AppArch})

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Windows ログオン時に自動起動する (推奨)"; GroupDescription: "追加のタスク:"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "追加のタスク:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\説明書 (README)"; Filename: "{app}\README.md"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// Close running instance on uninstall
function InitializeUninstall(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  // If app is running, kill it cleanly before uninstalling
  Exec('taskkill.exe', '/F /IM IMEKeyBinder.exe', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
end;
