#define AppVersion "2.1.0"
#ifdef TestInstall
  #define AppIdentity "PNYColor-InstallerTest-2"
  #define SettingsKey "Software\PNYColor\InstallerTest"
  #define AppGuard "Local\PNYColor.InstallerTest"
  #define OutputName "PNY-Color-Setup-Test"
#else
  #define AppIdentity "{{74BA3049-739F-4D41-BAF1-BDE0EA286C02}"
  #define SettingsKey "Software\PNYColor"
  #define AppGuard "Local\PNYColor.Uprising.1384196E.v2"
  #define OutputName "PNY-Color-Completo-2.1.0"
#endif

[Setup]
AppId={#AppIdentity}
AppName=PNY Color
AppVersion={#AppVersion}
AppVerName=PNY Color {#AppVersion}
AppPublisher=PNY Color — Independent Project
DefaultDirName={autopf}\PNY Color
DefaultGroupName=PNY Color
DisableDirPage=no
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
WizardStyle=modern
WizardSizePercent=120
ShowLanguageDialog=yes
UsePreviousLanguage=yes
UsePreviousAppDir=yes
UsePreviousTasks=yes
OutputDir=.
OutputBaseFilename={#OutputName}
SetupIconFile=pny-color.ico
WizardImageFile=installer-side.bmp
WizardSmallImageFile=installer-mark.bmp
UninstallDisplayIcon={app}\PnyColor.exe
Compression=lzma2/max
SolidCompression=yes
AppMutex={#AppGuard}
CloseApplications=no
RestartApplications=no
SetupLogging=yes
UninstallLogging=yes
Uninstallable=yes
VersionInfoVersion=2.1.0.0
VersionInfoDescription=PNY Color Installer
VersionInfoProductName=PNY Color
VersionInfoProductVersion=2.1.0

[Languages]
Name: "pt_BR"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"

[CustomMessages]
pt_BR.DesktopIcon=Criar atalho na área de trabalho
en.DesktopIcon=Create a desktop shortcut
es.DesktopIcon=Crear un acceso directo en el escritorio
fr.DesktopIcon=Créer un raccourci sur le bureau
pt_BR.StartMenuIcon=Criar atalho no menu Iniciar
en.StartMenuIcon=Create a Start menu shortcut
es.StartMenuIcon=Crear un acceso directo en el menú Inicio
fr.StartMenuIcon=Créer un raccourci dans le menu Démarrer
pt_BR.Launch=Abrir PNY Color
en.Launch=Launch PNY Color
es.Launch=Abrir PNY Color
fr.Launch=Ouvrir PNY Color
pt_BR.BridgeError=Não foi possível preparar a ponte SignalRGB. Abra a página SignalRGB no PNY Color para tentar novamente.
en.BridgeError=Could not prepare the SignalRGB bridge. Open the SignalRGB page in PNY Color to retry.
es.BridgeError=No se pudo preparar el puente SignalRGB. Abre la página SignalRGB en PNY Color para reintentar.
fr.BridgeError=Impossible de préparer le pont SignalRGB. Ouvrez la page SignalRGB dans PNY Color pour réessayer.
pt_BR.FinishedLabel=PNY Color foi instalado com o plugin SignalRGB incluído.%n%nReinicie o SignalRGB para detectar a placa e mantenha PNY Color aberto para receber as cores. O driver NVIDIA e o aplicativo SignalRGB são instalados separadamente.
en.FinishedLabel=PNY Color and its SignalRGB plugin are installed.%n%nRestart SignalRGB to detect the card and keep PNY Color open to receive colors. The NVIDIA driver and SignalRGB application are installed separately.
es.FinishedLabel=PNY Color y su plugin SignalRGB están instalados.%n%nReinicia SignalRGB y mantén PNY Color abierto. El controlador NVIDIA y SignalRGB se instalan por separado.
fr.FinishedLabel=PNY Color et son plugin SignalRGB sont installés.%n%nRedémarrez SignalRGB et gardez PNY Color ouvert. Le pilote NVIDIA et SignalRGB s’installent séparément.
pt_BR.WelcomeLabel1=Suas cores. Seu controle.
en.WelcomeLabel1=Your colors. Your control.
es.WelcomeLabel1=Tus colores. Tu control.
fr.WelcomeLabel1=Vos couleurs. Votre contrôle.
pt_BR.WelcomeLabel2=Instale o PNY Color, um controlador RGB independente.%n%nEscolha a pasta e os atalhos nas próximas etapas. O idioma selecionado será usado no aplicativo.%n%nCompatibilidade atual: PNY RTX 3080 Ti UPRISING validada. O instalador não ativa efeitos nem muda a iluminação.
en.WelcomeLabel2=Install PNY Color, an independent RGB controller.%n%nChoose your folder and shortcuts in the next steps. The selected language will be used in the application.%n%nCurrent support: the validated PNY RTX 3080 Ti UPRISING. Setup does not activate effects or change lighting.
es.WelcomeLabel2=Instala PNY Color, un controlador RGB independiente.%n%nElige la carpeta y los accesos directos en los siguientes pasos. La aplicación usará el idioma seleccionado.%n%nCompatibilidad actual: PNY RTX 3080 Ti UPRISING validada. La instalación no activa efectos ni cambia la iluminación.
fr.WelcomeLabel2=Installez PNY Color, un contrôleur RGB indépendant.%n%nChoisissez le dossier et les raccourcis dans les étapes suivantes. La langue choisie sera utilisée dans l’application.%n%nCompatibilité actuelle : PNY RTX 3080 Ti UPRISING validée. L’installation n’active aucun effet et ne modifie pas l’éclairage.

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenuicon"; Description: "{cm:StartMenuIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "PnyColor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "PNY-Color-LEIA-ME.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "SignalRGB\PNY_Color_Bridge.js"; DestDir: "{app}\SignalRGB"; Flags: ignoreversion
Source: "SignalRGB\README.md"; DestDir: "{app}\SignalRGB"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\PNY Color\PNY Color"; Filename: "{app}\PnyColor.exe"; WorkingDir: "{app}"; Tasks: startmenuicon
Name: "{autoprograms}\PNY Color\{cm:UninstallProgram,PNY Color}"; Filename: "{uninstallexe}"; Tasks: startmenuicon
Name: "{autodesktop}\PNY Color"; Filename: "{app}\PnyColor.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "{#SettingsKey}"; ValueType: string; ValueName: "Language"; ValueData: "{code:GetAppLanguage}"
; User preferences and language are deliberately retained on uninstall.

[Run]
Filename: "{app}\PnyColor.exe"; Description: "{cm:Launch}"; Flags: nowait postinstall skipifsilent

[Code]
function GetAppLanguage(Param: String): String;
begin
  Result := ActiveLanguage;
  if Result = 'pt_BR' then Result := 'pt-BR';
end;

procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel1.Caption := CustomMessage('WelcomeLabel1');
  WizardForm.WelcomeLabel2.Caption := CustomMessage('WelcomeLabel2');
  WizardForm.FinishedLabel.Caption := CustomMessage('FinishedLabel');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  BridgeArguments: String;
begin
#ifdef TestInstall
  BridgeArguments := '--packaging-self-test';
#else
  BridgeArguments := '--install-bridge';
#endif
  if CurStep = ssPostInstall then
    if not Exec(ExpandConstant('{app}\PnyColor.exe'), BridgeArguments, ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      RaiseException(CustomMessage('BridgeError'))
    else if ResultCode <> 0 then
      RaiseException(CustomMessage('BridgeError'));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Command: String;
  Expected: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    Expected := '"' + ExpandConstant('{app}\PnyColor.exe') + '" --startup';
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PNYColor', Command) then
      if CompareText(Command, Expected) = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PNYColor');
  end;
end;
