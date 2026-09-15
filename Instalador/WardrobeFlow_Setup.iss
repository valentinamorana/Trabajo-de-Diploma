; =====================================================================
; WardrobeFlow — Instalador (A01, version inicial / caso simple)
;
; Alcance de esta version (Entrega 2):
; - Instala la aplicacion (GUI.exe + dependencias) en Archivos de Programa.
; - Crea la base de datos WardrobeFlowDB completa (estructura + los 4
;   procesos de negocio PN01-PN04) ejecutando BD/00_Instalacion_Completa.sql
;   contra una instancia SQLEXPRESS local YA INSTALADA, mediante un cliente
;   SQL embebido (DbInstaller.exe, ADO.NET/SqlClient) que viaja DENTRO del
;   propio instalador — no depende de que el cliente tenga sqlcmd/SQL Server
;   Command Line Utilities instalados aparte (PPT, slide "A01: Instalacion
;   de Base Datos": "Automatizacion mediante cliente SQL embebido...").
; - Crea accesos directos (menu inicio + escritorio opcional).
; - Caso de prueba contemplado: instalacion simple, con SQL Server Express
;   (instancia SQLEXPRESS) ya instalado y el servicio en ejecucion.
; - Un unico archivo .exe de salida: Inno Setup empaqueta GUI.exe + todas
;   las DLLs de la app, los .sql y DbInstaller.exe comprimidos adentro del
;   instalador (ver OutputBaseFilename mas abajo); nada se distribuye suelto.
; - Verifica .NET Framework 4.7.2+ en el cliente ANTES de copiar archivos
;   (InitializeSetup); si falta, avisa y no instala nada.
;
; Fuera de alcance para esta entrega (se agrega en Entrega 3, "casos
; especiales"): deteccion/instalacion de SQL Server si no esta presente,
; deteccion de instancia con otro nombre o de 2 instancias simultaneas,
; deteccion de servicio detenido, desinstalacion de SQL, pre-flight checks
; y rollback automatico si falla la creacion de la BD.
;
; Gap conocido (fuera de esta iteracion, requiere certificado de firma de
; codigo que todavia no se consiguio): firma digital del .exe con SignTool
; (PPT, slide "Compilacion y Generacion .EXE"). Sin firma, Windows
; SmartScreen puede advertir al ejecutarlo.
; =====================================================================

#define MyAppName "WardrobeFlow"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Valentina Morana"
#define MyAppExeName "GUI.exe"
#define MyDatabaseName "WardrobeFlowDB"
#define MySqlInstance ".\SQLEXPRESS"

#define AppSourceDir "..\GUI\bin\Release"
#define DbSourceDir "..\BD"
#define DbInstallerSourceDir "DbInstaller\bin\Release"
#define DbInstallerExeName "DbInstaller.exe"

#if !FileExists(AppSourceDir + "\" + MyAppExeName)
  #error "No se encontro GUI.exe en GUI\bin\Release. Compilar el proyecto en modo Release antes de generar el instalador."
#endif

#if !FileExists(DbSourceDir + "\00_Instalacion_Completa.sql")
  #error "No se encontro BD\00_Instalacion_Completa.sql (script unico de instalacion)."
#endif

#if !FileExists(DbInstallerSourceDir + "\" + DbInstallerExeName)
  #error "No se encontro DbInstaller.exe en Instalador\DbInstaller\bin\Release. Compilar ese proyecto en modo Release antes de generar el instalador."
#endif

[Setup]
AppId={{93353B74-4ABF-4203-B7B1-3DA4831E72B7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=Salida
OutputBaseFilename=Instalador_WardrobeFlow_V1
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#AppSourceDir}\icon.ico
UninstallDisplayName={#MyAppName}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos adicionales:"; Flags: unchecked

[Files]
Source: "{#AppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#DbSourceDir}\*.sql"; DestDir: "{app}\BD"; Flags: ignoreversion
Source: "{#DbInstallerSourceDir}\{#DbInstallerExeName}"; DestDir: "{app}\BD"; Flags: ignoreversion
Source: "Credenciales_Iniciales.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Credenciales iniciales"; Filename: "{app}\Credenciales_Iniciales.txt"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\Credenciales_Iniciales.txt"; Description: "Ver las credenciales iniciales"; Flags: postinstall shellexec skipifsilent unchecked
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
// Verifica que el cliente tenga .NET Framework 4.7.2 o superior instalado
// (release >= 461808, umbral oficial de Microsoft para 4.7.2) ANTES de
// copiar ningún archivo — si falta, se corta acá y no queda nada instalado
// a medias (PPT, slide "Inclusión de Dependencias": "Verificación del
// Runtime de .NET: el instalador valida si la versión requerida está
// presente... antes de continuar").
function TieneNetFramework472(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release)
            and (Release >= 461808);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not TieneNetFramework472() then
  begin
    SuppressibleMsgBox(
      'WardrobeFlow requiere .NET Framework 4.7.2 o superior, que no se encontró instalado en este equipo.' + #13#13 +
      'Instalá .NET Framework 4.7.2 (o una versión posterior) desde ' +
      'https://dotnet.microsoft.com/download/dotnet-framework y volvé a ejecutar este instalador.',
      mbError, MB_OK, IDOK);
    Result := False;
  end;
end;

// Ejecuta un script .sql contra la instancia SQLEXPRESS local usando el
// cliente SQL embebido (DbInstaller.exe, ADO.NET/SqlClient) que se copió
// junto con la app en {app}\BD. A diferencia de sqlcmd, este .exe viaja
// DENTRO del instalador (ver [Files]) y no depende de que el cliente tenga
// las SQL Server Command Line Utilities instaladas aparte — así el
// instalador queda autocontenido en un único .exe (PPT: "cliente SQL
// embebido").
// El log queda en {app}\install.log (persistente, no en {tmp}) para poder
// diagnosticar después de terminada la instalación.
function EjecutarScriptSql(NombreArchivo: String; var ErrMsg: String): Boolean;
var
  ResultCode: Integer;
  DbInstallerPath: String;
  ScriptPath: String;
  LogPath: String;
  Params: String;
begin
  DbInstallerPath := ExpandConstant('{app}\BD\{#DbInstallerExeName}');
  ScriptPath := ExpandConstant('{app}\BD\') + NombreArchivo;
  LogPath := ExpandConstant('{app}\install.log');

  Params := '{#MySqlInstance} "' + ScriptPath + '" "' + LogPath + '"';

  Result := Exec(DbInstallerPath, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if not Result then
  begin
    ErrMsg := 'No se pudo ejecutar DbInstaller.exe (cliente SQL embebido del instalador).';
    exit;
  end;

  if ResultCode <> 0 then
  begin
    Result := False;
    ErrMsg := 'El script ' + NombreArchivo + ' devolvió un error (código ' + IntToStr(ResultCode) + ').' + #13#13 +
              'Verificar que SQL Server Express esté instalado y la instancia SQLEXPRESS en ejecución, ' +
              'y revisar el log: ' + LogPath;
  end;
end;

// Crea la base de datos y las tablas DESPUES de copiar los archivos (los
// scripts .sql ya están en {app}\BD en este punto del ciclo de instalación).
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrMsg: String;
begin
  if CurStep = ssPostInstall then
  begin
    WizardForm.StatusLabel.Caption := 'Creando la base de datos WardrobeFlowDB...';

    // Script único (BD/00_Instalacion_Completa.sql): crea la base y TODOS los
    // módulos de una sola pasada — antes acá se corrían 9 scripts sueltos a
    // mano y, ademas, se habian quedado desactualizados (nunca llegaron a
    // incluir los scripts 10 en adelante, con lo cual PN02/PN03/PN04 nunca
    // se instalaban). No hace falta indicar la base: el script arranca con
    // CREATE DATABASE + USE WardrobeFlowDB propios.
    if not EjecutarScriptSql('00_Instalacion_Completa.sql', ErrMsg) then
    begin
      SuppressibleMsgBox(
        'No se pudo completar la creación de la base de datos.' + #13#13 + ErrMsg + #13#13 +
        'La aplicación quedó instalada, pero necesitará ejecutar manualmente ' +
        'BD\00_Instalacion_Completa.sql (con SSMS) antes de poder iniciar sesión.',
        mbError, MB_OK, IDOK);
      exit;
    end;
  end;
end;
