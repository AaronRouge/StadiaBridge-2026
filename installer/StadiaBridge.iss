#ifndef AppVersion
  #error AppVersion debe recibirse desde build-installer.ps1
#endif

[Setup]
AppId={{1149F375-EA9A-48CA-84D3-E318082FAE99}
AppName=Stadia Bridge
AppVersion={#AppVersion}
AppPublisher=Stadia Bridge
DefaultDirName={autopf}\Stadia Bridge
DefaultGroupName=Stadia Bridge
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=StadiaBridge-Setup-{#AppVersion}-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\src\StadiaBridge\Assets\stadia-bridge.ico
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
UninstallDisplayIcon={app}\StadiaBridge.exe
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
AppMutex=Local\StadiaBridgeUsb
InfoBeforeFile=ANTES-DE-INSTALAR.txt

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: checkedonce

[Files]
Source: "..\dist\StadiaBridge-Standalone\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,diagnostico.txt,prueba-vibracion.txt"
Source: "vendor\ViGEmBus_1.22.0_x64_x86_arm64.exe"; Flags: dontcopy

[Icons]
Name: "{group}\Stadia Bridge"; Filename: "{app}\StadiaBridge.exe"; IconFilename: "{app}\StadiaBridge.exe"; IconIndex: 0
Name: "{autodesktop}\Stadia Bridge"; Filename: "{app}\StadiaBridge.exe"; IconFilename: "{app}\StadiaBridge.exe"; IconIndex: 0; Tasks: desktopicon
Name: "{group}\Desinstalar Stadia Bridge"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\StadiaBridge.exe"; Description: "Abrir Stadia Bridge"; Flags: nowait postinstall skipifsilent runasoriginaluser; Check: CanLaunch

[Code]
var
  DriverRestart: Boolean;

function DriverPresent: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SYSTEM\CurrentControlSet\Services\ViGEmBus');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Code: Integer;
begin
  Result := '';
  if DriverPresent then begin
    Log('ViGEmBus existente: se conserva la instalación actual.');
    Exit;
  end;
  WizardForm.StatusLabel.Caption := 'Instalando el controlador de mandos virtuales...';
  ExtractTemporaryFile('ViGEmBus_1.22.0_x64_x86_arm64.exe');
  if not Exec(ExpandConstant('{tmp}\ViGEmBus_1.22.0_x64_x86_arm64.exe'),
    '/exenoui /qn /norestart', '', SW_HIDE, ewWaitUntilTerminated, Code) then begin
    Result := 'No se pudo iniciar la instalación de ViGEmBus. Consulta el registro del instalador.';
    Exit;
  end;
  if (Code = 3010) or (Code = 1641) then begin
    DriverRestart := True;
    NeedsRestart := True;
  end else if Code <> 0 then begin
    Result := Format('ViGEmBus no pudo instalarse (código %d). La aplicación necesita este controlador para emular Xbox.', [Code]);
  end else if not DriverPresent then begin
    Result := 'ViGEmBus terminó sin registrar el controlador. Reinicia Windows y vuelve a ejecutar el instalador.';
  end;
end;

function NeedRestart: Boolean;
begin
  Result := DriverRestart;
end;

function CanLaunch: Boolean;
begin
  Result := not DriverRestart;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Code: Integer;
begin
  if (CurStep = ssPostInstall) and not DriverRestart then begin
    if not Exec(ExpandConstant('{app}\StadiaBridge.exe'), '--check-vigem', '', SW_HIDE, ewWaitUntilTerminated, Code) then
      Code := -1;
    if Code <> 0 then begin
      DriverRestart := True;
      Log(Format('El bus no está disponible todavía (código %d). Se recomienda reiniciar.', [Code]));
      if not WizardSilent then
        MsgBox('La aplicación está instalada. Reinicia Windows antes de usar la emulación Xbox. Si el problema continúa, revisa ViGEmBus en Aplicaciones instaladas.', mbInformation, MB_OK);
    end;
  end;
end;
