; =====================================================================
; WardrobeFlow — Instalador (A01, version inicial / caso simple)
;
; Alcance de esta version (Entrega 2):
; - Instala la aplicacion (GUI.exe + dependencias) en Archivos de Programa.
; - Crea la base de datos WardrobeFlowDB y todas sus tablas ejecutando los
;   scripts de BD/ contra una instancia SQLEXPRESS local YA INSTALADA.
; - Crea accesos directos (menu inicio + escritorio opcional).
; - Caso de prueba contemplado: instalacion simple, con SQL Server Express
;   (instancia SQLEXPRESS) ya instalado y el servicio en ejecucion.
;
; Fuera de alcance para esta entrega (se agrega en Entrega 3, "casos
; especiales"): deteccion/instalacion de SQL Server si no esta presente,
; deteccion de instancia con otro nombre, deteccion de servicio detenido.
; =====================================================================

#define MyAppName "WardrobeFlow"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Valentina Morana"
#define MyAppExeName "GUI.exe"
#define MyDatabaseName "WardrobeFlowDB"
#define MySqlInstance ".\SQLEXPRESS"

#define AppSourceDir "..\GUI\bin\Release"
#define DbSourceDir "..\BD"

#if !FileExists(AppSourceDir + "\" + MyAppExeName)
  #error "No se encontro GUI.exe en GUI\bin\Release. Compilar el proyecto en modo Release antes de generar el instalador."
#endif

#if !FileExists(DbSourceDir + "\01_Crear_BaseDeDatos.sql")
  #error "No se encontraron los scripts de BD en la carpeta BD."
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
Source: "Credenciales_Iniciales.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Credenciales iniciales"; Filename: "{app}\Credenciales_Iniciales.txt"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\Credenciales_Iniciales.txt"; Description: "Ver las credenciales iniciales"; Flags: postinstall shellexec skipifsilent unchecked
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
// Ejecuta un script .sql con sqlcmd contra la instancia SQLEXPRESS local.
// -E: autenticacion de Windows (igual que el connection string de la app).
// -f 65001: codepage UTF-8 (los scripts tienen acentos, ver comentario en
//   BD/01_Crear_BaseDeDatos.sql).
// -b: si el script tiene un error de T-SQL, sqlcmd devuelve codigo != 0.
function EjecutarScriptSql(NombreArchivo: String; UsarDb: Boolean; var ErrMsg: String): Boolean;
var
  ResultCode: Integer;
  Params: String;
  ScriptPath: String;
  LogPath: String;
begin
  ScriptPath := ExpandConstant('{app}\BD\') + NombreArchivo;
  LogPath := ExpandConstant('{tmp}\sqlcmd_') + NombreArchivo + '.log';

  Params := '-S {#MySqlInstance} -E -b -f 65001';
  if UsarDb then
    Params := Params + ' -d {#MyDatabaseName}';
  Params := Params + ' -i "' + ScriptPath + '" -o "' + LogPath + '"';

  Result := Exec(ExpandConstant('{cmd}'), '/C sqlcmd ' + Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if not Result then
  begin
    ErrMsg := 'No se pudo ejecutar sqlcmd. Verificar que las herramientas de línea de comandos ' +
              'de SQL Server (sqlcmd) estén instaladas y disponibles en PATH.';
    exit;
  end;

  if ResultCode <> 0 then
  begin
    Result := False;
    ErrMsg := 'El script ' + NombreArchivo + ' devolvió un error (código ' + IntToStr(ResultCode) + ').' + #13#13 +
              'Verificar que SQL Server Express esté instalado, la instancia SQLEXPRESS en ejecución, ' +
              'y revisar el log: ' + LogPath;
  end;
end;

// Crea la base de datos y las tablas DESPUES de copiar los archivos (los
// scripts .sql ya están en {app}\BD en este punto del ciclo de instalación).
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrMsg: String;
  Scripts: array[0..8] of String;
  UsaDb: array[0..8] of Boolean;
  i: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    WizardForm.StatusLabel.Caption := 'Creando la base de datos WardrobeFlowDB...';

    Scripts[0] := '01_Crear_BaseDeDatos.sql';            UsaDb[0] := False;
    Scripts[1] := '02_Actualizar_BaseDeDatos.sql';        UsaDb[1] := True;
    Scripts[2] := '03_Permisos_Granulares.sql';           UsaDb[2] := True;
    Scripts[3] := '04_Diagnostico_Limpieza_Nodos_Permiso.sql'; UsaDb[3] := True;
    Scripts[4] := '05_Renovacion_Suscripcion.sql';        UsaDb[4] := True;
    Scripts[5] := '06_Rediseno_Menu.sql';                 UsaDb[5] := True;
    Scripts[6] := '07_Reset_Perfiles_Permisos.sql';       UsaDb[6] := True;
    Scripts[7] := '08_Cobro_Pago.sql';                    UsaDb[7] := True;
    Scripts[8] := '09_Analisis_Abandono.sql';             UsaDb[8] := True;

    for i := 0 to 8 do
    begin
      if not EjecutarScriptSql(Scripts[i], UsaDb[i], ErrMsg) then
      begin
        SuppressibleMsgBox(
          'No se pudo completar la creación de la base de datos.' + #13#13 + ErrMsg + #13#13 +
          'La aplicación quedó instalada, pero necesitará ejecutar manualmente los scripts ' +
          'restantes de la carpeta BD (con SSMS o sqlcmd) antes de poder iniciar sesión.',
          mbError, MB_OK, IDOK);
        exit;
      end;
    end;
  end;
end;
