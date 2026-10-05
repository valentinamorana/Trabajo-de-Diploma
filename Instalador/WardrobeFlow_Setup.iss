; =====================================================================
; WardrobeFlow — Instalador (A01, version completa)
;
; Entrega 2 (caso base, ya resuelto):
; - Instala la aplicacion (GUI.exe + dependencias) en Archivos de Programa.
; - Crea la base de datos WardrobeFlowDB completa (estructura + los 4
;   procesos de negocio PN01-PN04) ejecutando BD/00_Instalacion_Completa.sql
;   mediante un cliente SQL embebido (DbInstaller.exe, ADO.NET/SqlClient)
;   que viaja DENTRO del propio instalador — no depende de que el cliente
;   tenga sqlcmd/SQL Server Command Line Utilities instalados aparte (PPT,
;   slide "A01: Instalacion de Base Datos": "cliente SQL embebido").
; - Crea accesos directos (menu inicio + escritorio opcional).
; - Un unico archivo .exe de salida: todo (GUI.exe + DLLs + .sql +
;   DbInstaller.exe) va comprimido adentro del instalador.
; - Verifica .NET Framework 4.7.2+ en el cliente ANTES de copiar archivos;
;   si falta, avisa y no instala nada.
;
; Entrega 3 (casos especiales):
; - Deteccion de instancias SQL Server locales (registro de Windows, PPT
;   slide "A01.1: Instancias SQL Inexistentes"). Si hay 2+, se le pide al
;   usuario elegir cual usar.
; - Si no hay NINGUNA instancia pero SQL LocalDB SI esta instalado en el
;   equipo (comun en maquinas con Visual Studio), se ofrece usarlo
;   automaticamente ((localdb)\MSSQLLocalDB) sin pedirle nada mas al
;   usuario (PPT slide "Instancias SQL Inexistentes": "Fallback Automatico").
; - Si no hay NINGUNA instancia (ni LocalDB): la opcion por defecto instala
;   SQL Server 2022 Express LocalDB, que viaja embebido en el instalador
;   (Redist\SqlLocalDB.msi, ~60MB, msiexec /passive). Se instala recien
;   cuando el usuario confirma la instalacion (PrepareToInstall), no al
;   elegir la opcion: si cancela antes, no queda nada instalado.
;   "Siguiente, Siguiente" alcanza aunque el equipo no tenga ningun motor.
;   Si el usuario elige no instalarlo, se le muestra el link de descarga de
;   SQL Server y se corta sin copiar nada (PPT slide "Sin Motor de Base de
;   Datos": "Asistente Guiado").
; - LocalDB es PRIVADO de cada usuario de Windows (la instancia y la base
;   viven en su perfil). Como el instalador corre elevado (puede ser con la
;   cuenta de OTRO administrador, via UAC), con LocalDB todo lo que toca la
;   base (backup, script, verificacion) corre como el usuario que inicio el
;   instalador (ExecAsOriginalUser): asi la base queda en SU perfil, que es
;   donde la va a buscar la app. El wizard avisa que esa base es solo de ese
;   usuario de Windows.
; - Si la instancia elegida existe pero su servicio de Windows esta
;   detenido, se intenta arrancarlo automaticamente (PPT slide "Servicio
;   SQL Detenido": ServiceController + Start()); si no se puede (permisos),
;   se avisa con instrucciones. Se chequea DOS veces: como pre-flight en el
;   wizard (antes de copiar archivos) y de nuevo antes de crear la BD.
; - Pre-flight check (PPT slide "Resiliencia": "antes de alterar el
;   sistema"), todo ANTES de copiar archivos: conexion (servidor ingresado a
;   mano), servicio (instancia detectada) y PERMISOS SQL (sysadmin o
;   CREATE ANY DATABASE; db_owner si la base ya existe). El chequeo de
;   espacio en disco lo hace Inno Setup por si solo (comportamiento nativo).
; - Actualizacion segura: si la base ya existe, ANTES de correr el script se
;   hace un backup (WardrobeFlowDB_pre_<version>_<fecha>.bak, en la carpeta
;   de backups de la instancia). Si una ACTUALIZACION falla, NO se
;   desinstala nada (la version anterior ya fue reemplazada y el rollback
;   borraria la instalacion entera): se informa el error y donde quedo el
;   backup para restaurar.
; - Rollback automatico (instalacion NUEVA): si el servicio no arranca o
;   falla la creacion de la BD DESPUES de copiar archivos, se desinstala
;   todo lo recien copiado automaticamente (preservando el log en
;   Documentos) en vez de dejar la app instalada pero no funcional (PPT
;   slide "Resiliencia": "Rollback Automatico").
; - El App.config (GUI.exe.config) instalado se reescribe con el servidor
;   realmente elegido — asi el connection string queda embebido pero
;   correcto para CUALQUIER instancia, no solo ".\SQLEXPRESS" fijo.
; - Al desinstalar, se pregunta (por defecto "No") si tambien se quiere
;   borrar la base de datos (y el login del grupo Usuarios, si ninguna otra
;   base lo usa).
; - Varias instancias en el mismo equipo (ej. SQLEXPRESS + SQLEXPRESS01 +
;   LocalDB): se listan todas con su version de SQL Server; LocalDB tambien
;   aparece aunque haya instancias. Al reinstalar se preselecciona la que se
;   uso la vez anterior (ahi estan los datos). Para instalaciones
;   desatendidas: Instalador.exe /VERYSILENT /SERVIDOR=.\SQLEXPRESS01
; - Despues del script: se da acceso a la base al grupo local "Usuarios"
;   con MINIMO PRIVILEGIO (lectura/escritura de datos, EXECUTE y backup; no
;   db_owner) — Integrated Security = el usuario de Windows que abre la app,
;   que puede no ser el administrador que instalo — y se corre una
;   verificacion final (admin semilla + datos); si falla, rollback.
;   Restaurar un backup desde la app requiere un administrador de Windows
;   (sysadmin de la instancia).
; - {app}\Backups y {app}\TempBackups quedan escribibles para usuarios
;   comunes (la app guarda ahi sus backups; TempBackups es el temporal de
;   respaldo cuando C:\Users\Public no se puede usar). {app}\Logs tambien,
;   para los logs de las operaciones que corren como el usuario original.
; - La firma digital (instalador Y desinstalador) la hace
;   compilar-y-firmar.ps1 (SignTool via la directiva SignTool de Inno) con
;   un certificado autofirmado: Windows SmartScreen igual advierte porque no
;   es de una CA de confianza. Sin el .pfx el instalador queda sin firmar.
;
; Fuera de alcance (gap conocido, no bloqueante):
; - SQL Server Express completo (servicio multiusuario, ~260MB) no se
;   embebe: sin motor se instala LocalDB, que alcanza para un puesto.
; - En /VERYSILENT sin ningun motor no se instala LocalDB: pasar
;   /SERVIDOR= con una instancia existente.
; =====================================================================

#define MyAppName "WardrobeFlow"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Valentina Morana"
#define MyAppExeName "GUI.exe"
#define MyDatabaseName "WardrobeFlowDB"
; Valor por defecto sugerido en la pantalla de ingreso manual del servidor
; (ya no se usa como destino fijo: el destino real se resuelve en tiempo de
; instalacion segun lo que el usuario elija/tenga instalado).
#define MySqlInstanceSugerido ".\SQLEXPRESS"

#define AppSourceDir "..\GUI\bin\Release"
#define DbSourceDir "..\BD"
#define DbInstallerSourceDir "DbInstaller\bin\Release"
#define DbInstallerExeName "DbInstaller.exe"
; Instalador oficial de SQL Server 2022 Express LocalDB. No se versiona (es
; de Microsoft, ~60MB): compilar-y-firmar.ps1 lo descarga si falta.
#define LocalDbMsi "Redist\SqlLocalDB.msi"

#if !FileExists(AppSourceDir + "\" + MyAppExeName)
  #error "No se encontro GUI.exe en GUI\bin\Release. Compilar el proyecto en modo Release antes de generar el instalador."
#endif

#if !FileExists(DbSourceDir + "\00_Instalacion_Completa.sql")
  #error "No se encontro BD\00_Instalacion_Completa.sql (script unico de instalacion)."
#endif

#if !FileExists(DbInstallerSourceDir + "\" + DbInstallerExeName)
  #error "No se encontro DbInstaller.exe en Instalador\DbInstaller\bin\Release. Compilar ese proyecto en modo Release antes de generar el instalador."
#endif

#if !FileExists(LocalDbMsi)
  #error "No se encontro Redist\SqlLocalDB.msi. Correr compilar-y-firmar.ps1, que lo descarga."
#endif

[Setup]
AppId={{93353B74-4ABF-4203-B7B1-3DA4831E72B7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
; Sin esto, un instalador de 32 bits (el default de Inno) en Windows de 64
; bits queda sujeto a la redirección WOW64 del registro: las lecturas bajo
; SOFTWARE\Microsoft\... ven la vista de 32 bits (WOW6432Node), y SQL Server
; de 64 bits se registra en la vista NATIVA. Verificado en este equipo: sin
; esta línea, DetectarInstanciasSql() encuentra 0 instancias aunque
; SQLEXPRESS esté corriendo; con esta línea, lo detecta correctamente (el
; código igual lee también WOW6432Node, por instancias de 32 bits).
ArchitecturesInstallIn64BitMode=x64compatible
; Si GUI.exe está corriendo, copiar/reemplazar sus archivos (o el desinstalador
; intentando borrarlos) falla a mitad de camino con el archivo bloqueado. Con
; esto, Inno detecta la app abierta (via Restart Manager de Windows) y le
; pide al usuario cerrarla antes de continuar, en vez de fallar silenciosamente.
CloseApplications=yes
RestartApplications=no
OutputDir=Salida
OutputBaseFilename=Instalador_WardrobeFlow_{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#AppSourceDir}\icon.ico
UninstallDisplayName={#MyAppName}
; Firma: compilar-y-firmar.ps1 define FIRMAR y pasa el comando de SignTool
; (/Swfsign=...) cuando hay certificado. Así Inno firma también el
; DESINSTALADOR (unins000.exe), no solo el Setup.
#ifdef FIRMAR
SignTool=wfsign
SignedUninstaller=yes
#endif

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos adicionales:"; Flags: unchecked

[Files]
; Excludes: no se distribuyen símbolos de depuración ni carpetas/archivos que
; la app genera al usarse en el equipo de desarrollo (backups, credenciales
; generadas, logs).
Source: "{#AppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,Backups\*,TempBackups\*,CredencialesGeneradas\*,*.bak,*.wfbak,*.log"
Source: "{#DbSourceDir}\*.sql"; DestDir: "{app}\BD"; Flags: ignoreversion
Source: "{#DbInstallerSourceDir}\{#DbInstallerExeName}"; DestDir: "{app}\BD"; Flags: ignoreversion
; Segunda copia, solo para {tmp}: permite usar DbInstaller.exe DURANTE el
; wizard (pre-flight de conexion) antes de que se copien los archivos a
; {app}, vía ExtractTemporaryFile — ver PaginaIngresoManual mas abajo.
Source: "{#DbInstallerSourceDir}\{#DbInstallerExeName}"; DestDir: "{tmp}"; Flags: dontcopy
Source: "Credenciales_Iniciales.txt"; DestDir: "{app}"; Flags: ignoreversion
; Solo se extrae (a {tmp}) si hay que instalar LocalDB: ver InstalarLocalDb.
Source: "{#LocalDbMsi}"; DestDir: "{tmp}"; Flags: dontcopy nocompression

; La app guarda backups y su configuracion de recordatorio en {app}\Backups.
; {app} esta en Archivos de Programa, donde un usuario sin permisos de
; administrador no puede escribir: sin esto, hacer un backup fallaba para
; cualquier usuario comun.
; TempBackups: DAL.Backup.DirectorioTempSeguro() usa C:\Users\Public\WardrobeFlow_Temp y,
; solo si esa carpeta no se puede usar, cae a {app}\TempBackups, donde la app (el usuario
; común) copia y borra el .bak temporal que lee/escribe SQL Server. Sigue haciendo falta
; escritura de usuarios ahí para ese caso; queda con users-modify a propósito.
; Logs: con LocalDB, DbInstaller corre como el usuario original (no elevado) y escribe su
; log acá. Es una carpeta aparte, NO {app}\BD: ahí está DbInstaller.exe, que el
; instalador ejecuta elevado, y no debe quedar escribible por usuarios comunes.
[Dirs]
Name: "{app}\Backups"; Permissions: users-modify
Name: "{app}\TempBackups"; Permissions: users-modify
Name: "{app}\Logs"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Credenciales iniciales (solo demo)"; Filename: "{app}\Credenciales_Iniciales.txt"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\Credenciales_Iniciales.txt"; Description: "Ver las credenciales iniciales (solo demo)"; Flags: postinstall shellexec skipifsilent unchecked
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

; install.log y uninstall.log se escriben en tiempo de ejecucion (CurStepChanged
; / CurUninstallStepChanged) — Inno no los conoce por venir de [Files], asi que
; sin esto quedan huerfanos en {app} y el desinstalador NO borra el directorio
; (por seguridad, no borra carpetas no vacias). Verificado con un harness
; aislado: sin esta seccion, el rollback automatico dejaba {app} a medio
; borrar; con ella, se limpia por completo. Si la desinstalación falla al
; borrar la base, el uninstall.log se copia antes a Documentos.
[UninstallDelete]
Type: files; Name: "{app}\install.log"
Type: files; Name: "{app}\install.log.out"
Type: files; Name: "{app}\uninstall.log"
Type: files; Name: "{app}\uninstall.log.out"
Type: filesandordirs; Name: "{app}\Logs"

[Code]
var
  PaginaSeleccionInstancia: TInputOptionWizardPage;
  PaginaSinInstancias: TInputOptionWizardPage;
  PaginaIngresoManual: TInputQueryWizardPage;
  InstanciasDetectadas: TArrayOfString;
  LocalDbDisponible: Boolean;
  ServidorElegido: String;
  // '' si la instancia final es manual/posiblemente remota: en ese caso no
  // hay un servicio de Windows LOCAL que tenga sentido chequear/arrancar.
  ServicioWindowsElegido: String;
  // Servidor pasado por linea de comandos (/SERVIDOR=.\SQLEXPRESS01). Si
  // viene, se usa ese y se saltan las paginas de base de datos: sirve para
  // instalaciones desatendidas (/VERYSILENT) en equipos con varias
  // instancias, donde el default (la primera detectada) puede no ser la
  // correcta.
  ServidorPorParametro: String;
  // El usuario eligio instalar LocalDB: se instala recien en PrepareToInstall
  // (despues de "Instalar"), no al elegir la opcion en el wizard.
  InstalarLocalDbPendiente: Boolean;
  // El usuario eligio "no tengo motor": el wizard se cierra sin preguntar.
  CerrarSinConfirmar: Boolean;
  // Ruta completa del backup previo a la actualizacion ('' si no se hizo).
  RutaBackupPrevio: String;
  // Ya se mostro el aviso de que una base LocalDB es de un solo usuario.
  AvisoLocalDbMostrado: Boolean;
  // Habia una version instalada antes de empezar (se calcula al inicio, antes de
  // que RegisterPreviousData guarde los datos de ESTA instalacion).
  HabiaVersionPrevia: Boolean;

// ExitProcess no es una funcion built-in de Pascal Script: hay que
// importarla de la WinAPI para poder cortar el setup despues de un fallo
// en ssPostInstall (ahi ya no hay forma "limpia" de abortar).
procedure ExitProcess(uExitCode: Cardinal);
  external 'ExitProcess@kernel32.dll stdcall';

// ExitProcess no le deja a Setup borrar su carpeta temporal: se borra lo
// que se pueda antes de salir (lo que este en uso queda, Windows lo limpia).
procedure TerminarSetup(uExitCode: Cardinal);
begin
  DelTree(ExpandConstant('{tmp}'), True, True, True);
  ExitProcess(uExitCode);
end;

// ---------------------------------------------------------------------
// Verificacion de .NET Framework (Entrega 2, PPT "Inclusión de Dependencias")
// ---------------------------------------------------------------------
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

// ---------------------------------------------------------------------
// Deteccion de instancias SQL Server (Entrega 3, PPT "A01.1: Instancias
// SQL Inexistentes" — "Detección y Búsqueda... mediante... Registro de
// Windows").
// ---------------------------------------------------------------------
function ArrayContieneTexto(const Arr: TArrayOfString; const Valor: String): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 0 to GetArrayLength(Arr) - 1 do
    if CompareText(Arr[I], Valor) = 0 then
    begin
      Result := True;
      exit;
    end;
end;

// Con ArchitecturesInstallIn64BitMode (ver [Setup]) las lecturas de HKLM ven
// la vista NATIVA de 64 bits, que es donde se registra SQL Server de 64 bits.
// Se lee ademas WOW6432Node para no perder instancias de SQL Server de 32
// bits (raras, pero posibles) y se combinan sin duplicados.
function DetectarInstanciasSql(): TArrayOfString;
var
  NombresNativo, NombresWow: TArrayOfString;
  Combinadas: TArrayOfString;
  I, N: Integer;
begin
  if not RegGetValueNames(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', NombresNativo) then
    SetArrayLength(NombresNativo, 0);
  if not RegGetValueNames(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL', NombresWow) then
    SetArrayLength(NombresWow, 0);

  SetArrayLength(Combinadas, GetArrayLength(NombresNativo) + GetArrayLength(NombresWow));
  N := 0;
  for I := 0 to GetArrayLength(NombresNativo) - 1 do
  begin
    Combinadas[N] := NombresNativo[I];
    N := N + 1;
  end;
  for I := 0 to GetArrayLength(NombresWow) - 1 do
  begin
    if not ArrayContieneTexto(NombresNativo, NombresWow[I]) then
    begin
      Combinadas[N] := NombresWow[I];
      N := N + 1;
    end;
  end;
  SetArrayLength(Combinadas, N);

  Result := Combinadas;
end;

// Fallback automático a LocalDB (PPT, slide "A01.1: Instancias SQL
// Inexistentes": "Fallback Automático"). Detecta si LocalDB YA está
// instalado (común en máquinas con Visual Studio) para ofrecerlo directo;
// si no lo está, el instalador lo instala desde el MSI embebido
// (Redist\SqlLocalDB.msi, ver InstalarLocalDb).
function TieneLocalDbInstalado(): Boolean;
var
  Versiones: TArrayOfString;
begin
  Result := (RegGetSubkeyNames(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions', Versiones)
             and (GetArrayLength(Versiones) > 0))
         or (RegGetSubkeyNames(HKCU, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions', Versiones)
             and (GetArrayLength(Versiones) > 0));
end;

// La instancia por defecto de SQL Server se registra como "MSSQLSERVER"
// (servicio "MSSQLSERVER", Data Source "."); cualquier otra es una
// instancia con nombre (servicio "MSSQL$<nombre>", Data Source ".\<nombre>").
function NombreServicioParaInstancia(const Instancia: String): String;
begin
  if CompareText(Instancia, 'MSSQLSERVER') = 0 then
    Result := 'MSSQLSERVER'
  else
    Result := 'MSSQL$' + Instancia;
end;

function DataSourceParaInstancia(const Instancia: String): String;
begin
  if CompareText(Instancia, 'MSSQLSERVER') = 0 then
    Result := '.'
  else
    Result := '.\' + Instancia;
end;

// "MSSQL15.SQLEXPRESS" -> "SQL Server 2019". Con varias instancias en el
// mismo equipo (ej. una Express vieja y una nueva) el nombre solo no alcanza
// para elegir: se muestra tambien la version de cada una.
function VersionDeInstancia(const Instancia: String): String;
var
  Id, Mayor: String;
  P: Integer;
begin
  Result := '';
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', Instancia, Id) then
    if not RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL', Instancia, Id) then
      exit;
  P := Pos('.', Id);
  if (Pos('MSSQL', Id) <> 1) or (P = 0) then exit;
  Mayor := Copy(Id, 6, P - 6);
  if Mayor = '11' then Result := 'SQL Server 2012'
  else if Mayor = '12' then Result := 'SQL Server 2014'
  else if Mayor = '13' then Result := 'SQL Server 2016'
  else if Mayor = '14' then Result := 'SQL Server 2017'
  else if Mayor = '15' then Result := 'SQL Server 2019'
  else if Mayor = '16' then Result := 'SQL Server 2022'
  else if Mayor = '17' then Result := 'SQL Server 2025'
  else Result := 'SQL Server v' + Mayor;
end;

function EtiquetaInstancia(const Instancia: String): String;
var
  Version: String;
begin
  Result := DataSourceParaInstancia(Instancia);
  Version := VersionDeInstancia(Instancia);
  if Version <> '' then
    Result := Result + '   (' + Version + ')';
end;

function EsLocalDb(const Servidor: String): Boolean;
begin
  Result := Pos('(localdb)', Lowercase(Servidor)) = 1;
end;

// Una actualizacion: ya habia una version instalada (con su servidor guardado).
function EsActualizacion(): Boolean;
begin
  Result := HabiaVersionPrevia;
end;

// Lee el resultado corto ("<log>.out", UTF-8) que deja DbInstaller.exe.
function LeerSalidaDbInstaller(const LogPath: String): String;
var
  Contenido: AnsiString;
begin
  Result := '';
  if LoadStringFromFile(LogPath + '.out', Contenido) then
    Result := Trim(UTF8Decode(Contenido));
end;

// Aviso (una sola vez) de que una base en LocalDB es del usuario de Windows
// que la crea: otro usuario del equipo que abra la app ve su propio LocalDB vacio.
procedure AvisarLocalDbPorUsuario();
begin
  if AvisoLocalDbMostrado then exit;
  AvisoLocalDbMostrado := True;
  SuppressibleMsgBox(
    'Importante: SQL LocalDB es privado de cada usuario de Windows.' + #13#13 +
    'La base de datos de WardrobeFlow se va a crear en el perfil del usuario con el que iniciaste ' +
    'este instalador, y solo ese usuario de Windows la va a ver. Si otra persona inicia sesión en ' +
    'Windows con otra cuenta y abre WardrobeFlow, no va a encontrar los datos.' + #13#13 +
    'Para que varios usuarios de Windows compartan la base, instalá SQL Server Express y elegí esa instancia.',
    mbInformation, MB_OK, IDOK);
end;

// Indices de PaginaSeleccionInstancia: primero las N instancias detectadas,
// despues LocalDB (solo si esta instalado) y al final "Otra...".
function IndiceLocalDbEnSeleccion(): Integer;
begin
  if LocalDbDisponible then Result := GetArrayLength(InstanciasDetectadas) else Result := -1;
end;

function IndiceOtraEnSeleccion(): Integer;
begin
  Result := GetArrayLength(InstanciasDetectadas);
  if LocalDbDisponible then Result := Result + 1;
end;

// Indices de PaginaSinInstancias: la primera opcion es siempre LocalDB
// (usarlo si ya esta instalado, o instalarlo si no).
function IndiceLocalDbEnSinInstancias(): Integer;
begin
  Result := 0;
end;

function IndiceManualEnSinInstancias(): Integer;
begin
  Result := 1;
end;

function IndiceCancelarEnSinInstancias(): Integer;
begin
  Result := 2;
end;

// Instala SQL Server 2022 Express LocalDB desde el MSI embebido (equipo sin
// ningun motor) y deja creada y arrancada la instancia MSSQLLocalDB. /passive
// muestra solo la barra de progreso de Windows Installer, sin preguntas.
// Se llama desde PrepareToInstall: recien despues de que el usuario confirmo.
// La instancia se crea y arranca como el usuario ORIGINAL (no el admin que
// eleva): LocalDB es por usuario y la base tiene que quedar en su perfil.
function InstalarLocalDb(var ErrMsg: String): Boolean;
var
  ResultCode: Integer;
  Msi, Log, SqlLocalDbExe: String;
begin
  Result := False;
  ExtractTemporaryFile('SqlLocalDB.msi');
  Msi := ExpandConstant('{tmp}\SqlLocalDB.msi');
  Log := ExpandConstant('{userdocs}\WardrobeFlow_LocalDB_install.log');
  if not Exec(ExpandConstant('{sys}\msiexec.exe'),
              '/i "' + Msi + '" /passive /norestart IACCEPTSQLLOCALDBLICENSETERMS=YES /l*v "' + Log + '"',
              '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    ResultCode := -1;
  // 3010 = instalado OK, pide reiniciar (LocalDB funciona igual sin reinicio).
  if (ResultCode <> 0) and (ResultCode <> 3010) then
  begin
    ErrMsg :=
      'No se pudo instalar SQL Server LocalDB (código ' + IntToStr(ResultCode) + ').' + #13#13 +
      'Detalle en: ' + Log + #13#13 +
      'Podés instalar SQL Server Express a mano desde https://www.microsoft.com/sql-server/sql-server-downloads ' +
      'y volver a ejecutar este instalador. No se instaló nada de WardrobeFlow.';
    exit;
  end;

  LocalDbDisponible := TieneLocalDbInstalado();
  if not LocalDbDisponible then
  begin
    ErrMsg := 'SQL Server LocalDB se instaló pero no aparece registrado en el equipo. Detalle en: ' + Log;
    exit;
  end;

  SqlLocalDbExe := ExpandConstant('{commonpf64}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe');
  if FileExists(SqlLocalDbExe) then
  begin
    ExecAsOriginalUser(SqlLocalDbExe, 'create MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode); // ya existe: no pasa nada
    ExecAsOriginalUser(SqlLocalDbExe, 'start MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
  Result := True;
end;

// ---------------------------------------------------------------------
// Paginas del wizard
// ---------------------------------------------------------------------
procedure InitializeWizard();
var
  I, IndicePrevio: Integer;
  ServidorPrevio: String;
begin
  InstanciasDetectadas := DetectarInstanciasSql();
  LocalDbDisponible := TieneLocalDbInstalado();
  ServidorPorParametro := Trim(ExpandConstant('{param:SERVIDOR|}'));
  // Reinstalacion/actualizacion: el servidor que se uso la vez anterior (ahi
  // estan los datos). Con varias instancias, preseleccionar otra crearia una
  // base nueva y vacia en esa y la app "perderia" los datos existentes.
  ServidorPrevio := GetPreviousData('ServidorSql', '');
  HabiaVersionPrevia := ServidorPrevio <> '';

  // Valor por defecto sensato para ServidorElegido/ServicioWindowsElegido
  // desde ya (coincide con lo pre-seleccionado en la pagina de abajo): si
  // el instalador se corre en modo /VERYSILENT, NextButtonClick nunca se
  // llama para paginas que no se muestran, así que sin esto la instalación
  // silenciosa quedaría con un servidor vacío.
  if GetArrayLength(InstanciasDetectadas) > 0 then
  begin
    ServidorElegido := DataSourceParaInstancia(InstanciasDetectadas[0]);
    ServicioWindowsElegido := NombreServicioParaInstancia(InstanciasDetectadas[0]);
  end
  else if LocalDbDisponible then
  begin
    ServidorElegido := '(localdb)\MSSQLLocalDB';
    ServicioWindowsElegido := ''; // LocalDB no corre como servicio de Windows
  end
  else
  begin
    ServidorElegido := '{#MySqlInstanceSugerido}';
    ServicioWindowsElegido := '';
  end;

  // Se extrae una copia de DbInstaller.exe a {tmp} para poder usarlo
  // DURANTE el wizard (pre-flight de conexion), antes de que exista {app}.
  // Si por algun motivo falla, el pre-flight simplemente se salta mas
  // adelante (guardado con FileExists) en vez de tirar abajo el wizard.
  try
    ExtractTemporaryFile('{#DbInstallerExeName}');
  except
  end;

  // Caso: hay 1+ instancias locales detectadas -> elegir cual usar.
  PaginaSeleccionInstancia := CreateInputOptionPage(wpSelectTasks,
    'Base de datos', 'Se encontró SQL Server en este equipo',
    'Se detectaron las siguientes instancias de SQL Server instaladas localmente. Elegí cuál va a usar WardrobeFlow ' +
    '(una base en SQL LocalDB queda solo para tu usuario de Windows):',
    True, False);
  IndicePrevio := -1;
  for I := 0 to GetArrayLength(InstanciasDetectadas) - 1 do
  begin
    PaginaSeleccionInstancia.Add(EtiquetaInstancia(InstanciasDetectadas[I]));
    if CompareText(DataSourceParaInstancia(InstanciasDetectadas[I]), ServidorPrevio) = 0 then
      IndicePrevio := I;
  end;
  if LocalDbDisponible then
  begin
    PaginaSeleccionInstancia.Add('(localdb)\MSSQLLocalDB   (SQL LocalDB, solo para tu usuario de Windows)');
    if EsLocalDb(ServidorPrevio) then
      IndicePrevio := IndiceLocalDbEnSeleccion();
  end;
  PaginaSeleccionInstancia.Add('Otra instancia o servidor (ingresar manualmente)');
  // Servidor previo que no es ninguno de los detectados (remoto/manual):
  // queda preseleccionado "Otra..." con ese valor ya cargado.
  if (IndicePrevio = -1) and (ServidorPrevio <> '') then
    IndicePrevio := IndiceOtraEnSeleccion();
  if GetArrayLength(InstanciasDetectadas) > 0 then
  begin
    if IndicePrevio >= 0 then
      PaginaSeleccionInstancia.SelectedValueIndex := IndicePrevio
    else
      PaginaSeleccionInstancia.SelectedValueIndex := 0;
  end;

  // Encadenadas por .ID (no todas a wpSelectTasks): crear varias paginas
  // custom con el mismo AfterID puede insertarlas en orden invertido.
  // Caso: no se detecto ninguna instancia local.
  PaginaSinInstancias := CreateInputOptionPage(PaginaSeleccionInstancia.ID,
    'Base de datos', 'No se encontró SQL Server en este equipo',
    'WardrobeFlow necesita una instancia de SQL Server (Express, LocalDB, o una instalación completa) para funcionar. ' +
    'Una base en SQL LocalDB queda solo para tu usuario de Windows. ¿Cómo querés continuar?',
    True, False);
  if LocalDbDisponible then
    PaginaSinInstancias.Add('Usar SQL LocalDB, ya detectado en este equipo ((localdb)\MSSQLLocalDB)')
  else
    PaginaSinInstancias.Add('Instalar SQL Server Express LocalDB (recomendado, incluido en este instalador; se instala al confirmar)');
  PaginaSinInstancias.Add('Ya tengo SQL Server instalado (con otro nombre, o en otro servidor) — ingresar manualmente');
  PaginaSinInstancias.Add('No tengo ningún motor de SQL Server instalado — cancelar la instalación');
  PaginaSinInstancias.SelectedValueIndex := 0;

  // Ingreso manual del servidor\instancia (comun a los dos casos de arriba).
  PaginaIngresoManual := CreateInputQueryPage(PaginaSinInstancias.ID,
    'Base de datos', 'Servidor de SQL Server',
    'Ingresá el servidor y, si corresponde, la instancia (ejemplos: .\SQLEXPRESS, (localdb)\MSSQLLocalDB, MIPC\INSTANCIA):');
  PaginaIngresoManual.Add('Servidor\Instancia:', False);
  PaginaIngresoManual.Values[0] := '{#MySqlInstanceSugerido}';
  if ServidorPrevio <> '' then
    PaginaIngresoManual.Values[0] := ServidorPrevio;

  // Defaults coherentes con lo preseleccionado (importa en /VERYSILENT,
  // donde NextButtonClick no se llama).
  if (GetArrayLength(InstanciasDetectadas) > 0) and (IndicePrevio >= 0) then
  begin
    if IndicePrevio < GetArrayLength(InstanciasDetectadas) then
    begin
      ServidorElegido := DataSourceParaInstancia(InstanciasDetectadas[IndicePrevio]);
      ServicioWindowsElegido := NombreServicioParaInstancia(InstanciasDetectadas[IndicePrevio]);
    end
    else
    begin
      ServidorElegido := ServidorPrevio;
      ServicioWindowsElegido := '';
    end;
  end;

  if ServidorPorParametro <> '' then
  begin
    ServidorElegido := ServidorPorParametro;
    ServicioWindowsElegido := '';
    for I := 0 to GetArrayLength(InstanciasDetectadas) - 1 do
      if CompareText(DataSourceParaInstancia(InstanciasDetectadas[I]), ServidorPorParametro) = 0 then
        ServicioWindowsElegido := NombreServicioParaInstancia(InstanciasDetectadas[I]);
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (ServidorPorParametro <> '') and ((PageID = PaginaSeleccionInstancia.ID) or
     (PageID = PaginaSinInstancias.ID) or (PageID = PaginaIngresoManual.ID)) then
    Result := True
  else if PageID = PaginaSeleccionInstancia.ID then
    Result := GetArrayLength(InstanciasDetectadas) = 0
  else if PageID = PaginaSinInstancias.ID then
    Result := GetArrayLength(InstanciasDetectadas) > 0
  else if PageID = PaginaIngresoManual.ID then
  begin
    if GetArrayLength(InstanciasDetectadas) > 0 then
      // Se salta el ingreso manual salvo que se haya elegido la ultima
      // opcion de la lista ("Otra instancia...").
      Result := PaginaSeleccionInstancia.SelectedValueIndex <> IndiceOtraEnSeleccion()
    else
      // Sin instancias detectadas: el ingreso manual solo aplica si se
      // eligio esa opcion especifica (el indice depende de si tambien se
      // ofrece LocalDB automatico).
      Result := PaginaSinInstancias.SelectedValueIndex <> IndiceManualEnSinInstancias();
  end;
end;

// "No tengo ningun motor": se cierra el wizard sin la pregunta de "¿Salir?".
procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
begin
  if CerrarSinConfirmar then
    Confirm := False;
end;

// Pre-flight del servicio de Windows para una instancia detectada
// localmente: chequea/arranca el servicio usando la copia de DbInstaller.exe
// en {tmp} (todavía no existe {app}). Si falla, le pregunta al usuario si
// quiere volver atrás a elegir otra instancia o seguir igual (se vuelve a
// chequear, con rollback si vuelve a fallar, en CurStepChanged).
function VerificarServicioPreflight(const NombreServicio: String): Boolean;
var
  ResultCode: Integer;
  DbInstallerTmp, LogPath: String;
begin
  Result := True; // si no se puede chequear acá, no bloqueamos: se reintenta después
  DbInstallerTmp := ExpandConstant('{tmp}\{#DbInstallerExeName}');
  if not FileExists(DbInstallerTmp) then exit;

  LogPath := ExpandConstant('{tmp}\preflight.log');
  if Exec(DbInstallerTmp, 'check-service "' + NombreServicio + '" "' + LogPath + '"',
          '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0) then
    exit; // Result ya es True: servicio OK

  if SuppressibleMsgBox(
       'El servicio de Windows para esa instancia (' + NombreServicio + ') no está disponible ' +
       '(no existe, o está detenido y no se pudo iniciar automáticamente — ¿faltan permisos de administrador?).' + #13#13 +
       '¿Querés continuar de todos modos? (se va a reintentar más adelante)',
       mbConfirmation, MB_YESNO, IDYES) = IDNO then
    Result := False;
end;

// Pre-flight de PERMISOS SQL (antes de copiar archivos): la cuenta con la que
// corre el instalador tiene que poder crear la base (sysadmin o CREATE ANY
// DATABASE) o, si ya existe, ser su db_owner. Con LocalDB no aplica: el dueño
// de la instancia es sysadmin de ella. Devuelve False si hay que quedarse en la
// pagina (permisos insuficientes, o no se pudo conectar y el usuario no sigue).
function VerificarPermisosPreflight(const Servidor: String): Boolean;
var
  ResultCode: Integer;
  DbInstallerTmp, LogPath, Detalle: String;
begin
  Result := True;
  if EsLocalDb(Servidor) then exit;
  DbInstallerTmp := ExpandConstant('{tmp}\{#DbInstallerExeName}');
  if not FileExists(DbInstallerTmp) then exit;

  LogPath := ExpandConstant('{tmp}\preflight.log');
  if not Exec(DbInstallerTmp, 'check-perms "' + Servidor + '" "{#MyDatabaseName}" "' + LogPath + '"',
              '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    exit; // no se pudo lanzar: no bloquear, el script lo detecta igual
  Detalle := LeerSalidaDbInstaller(LogPath);

  if ResultCode = 3 then
  begin
    SuppressibleMsgBox(
      'Permisos insuficientes en SQL Server (' + Servidor + ').' + #13#13 + Detalle + #13#13 +
      'Ejecutá el instalador con una cuenta de Windows que sea administradora de esa instancia (sysadmin), ' +
      'pedile a quien administra SQL Server que te dé el rol dbcreator, o elegí otra instancia.',
      mbError, MB_OK, IDOK);
    Result := False;
  end
  else if ResultCode <> 0 then
  begin
    if SuppressibleMsgBox(
         'No se pudieron verificar los permisos en ''' + Servidor + '''.' + #13#13 + Detalle + #13#13 +
         '¿Querés continuar de todos modos?',
         mbConfirmation, MB_YESNO, IDNO) = IDNO then
      Result := False;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
  LogPath: String;
  DbInstallerTmp: String;
  IndiceElegido: Integer;
  ConexionOk: Boolean;
begin
  Result := True;

  if CurPageID = PaginaSeleccionInstancia.ID then
  begin
    IndiceElegido := PaginaSeleccionInstancia.SelectedValueIndex;
    if IndiceElegido < GetArrayLength(InstanciasDetectadas) then
    begin
      // Se eligió una instancia detectada automáticamente.
      ServidorElegido := DataSourceParaInstancia(InstanciasDetectadas[IndiceElegido]);
      ServicioWindowsElegido := NombreServicioParaInstancia(InstanciasDetectadas[IndiceElegido]);

      // Pre-flight (PPT "Resiliencia": validar servicios ANTES de alterar
      // el sistema): chequear/arrancar el servicio ACÁ, antes de copiar
      // ningún archivo. Si el usuario decide "continuar de todos modos", se
      // vuelve a intentar más tarde igual (con rollback si falla ahí).
      if not VerificarServicioPreflight(ServicioWindowsElegido) then
      begin
        Result := False;
        exit;
      end;
      // ...y los permisos SQL de la cuenta que instala.
      if not VerificarPermisosPreflight(ServidorElegido) then
      begin
        Result := False;
        exit;
      end;
    end
    else if IndiceElegido = IndiceLocalDbEnSeleccion() then
    begin
      // LocalDB junto a otras instancias: no corre como servicio de Windows.
      ServidorElegido := '(localdb)\MSSQLLocalDB';
      ServicioWindowsElegido := '';
      AvisarLocalDbPorUsuario();
    end;
    // Si eligió "Otra..." (el último índice), ServidorElegido se define
    // más abajo, en PaginaIngresoManual.
  end;

  if CurPageID = PaginaSinInstancias.ID then
  begin
    InstalarLocalDbPendiente := False;
    if PaginaSinInstancias.SelectedValueIndex = IndiceLocalDbEnSinInstancias() then
    begin
      // Sin ningun motor: LocalDB se instala desde el MSI embebido, pero
      // recien en PrepareToInstall (despues de que el usuario confirma).
      InstalarLocalDbPendiente := not LocalDbDisponible;
      // Fallback automático a LocalDB (PPT slide "Instancias SQL
      // Inexistentes"): no hace falta pedirle nada más al usuario. LocalDB
      // no corre como servicio de Windows: no hay nada que chequear/arrancar.
      ServidorElegido := '(localdb)\MSSQLLocalDB';
      ServicioWindowsElegido := '';
      AvisarLocalDbPorUsuario();
    end
    else if PaginaSinInstancias.SelectedValueIndex = IndiceCancelarEnSinInstancias() then
    begin
      // "No tengo ningún motor instalado" -> cortar ACÁ, sin copiar nada
      // (PPT slide "Sin Motor de Base de Datos": Asistente Guiado con link
      // de descarga). El usuario instala el motor y vuelve a correr esto.
      SuppressibleMsgBox(
        'WardrobeFlow necesita SQL Server para funcionar y no se detectó ninguna instancia en este equipo.' + #13#13 +
        'Descargá e instalá SQL Server Express (o LocalDB) desde:' + #13 +
        'https://www.microsoft.com/sql-server/sql-server-downloads' + #13#13 +
        'Después, volvé a ejecutar este instalador. No se instaló nada.',
        mbInformation, MB_OK, IDOK);
      // Cierre normal del wizard (Setup limpia su carpeta temporal), sin la
      // pregunta de confirmacion (ver CancelButtonClick).
      CerrarSinConfirmar := True;
      WizardForm.Close;
      Result := False;
      exit;
    end;
    // Si eligió "ingresar manualmente", ServidorElegido se define más
    // abajo, en PaginaIngresoManual.
  end;

  if CurPageID = PaginaIngresoManual.ID then
  begin
    ServidorElegido := Trim(PaginaIngresoManual.Values[0]);
    if ServidorElegido = '' then
    begin
      SuppressibleMsgBox('Ingresá un servidor/instancia válido.', mbError, MB_OK, IDOK);
      Result := False;
      exit;
    end;

    // Instancia manual = posiblemente remota: no hay un servicio de
    // Windows local que tenga sentido chequear/arrancar más adelante.
    ServicioWindowsElegido := '';

    if EsLocalDb(ServidorElegido) then
    begin
      // LocalDB ingresado a mano: el pre-flight (que corre como el admin
      // que eleva) probaria el LocalDB de OTRO perfil; no se prueba acá.
      AvisarLocalDbPorUsuario();
      exit;
    end;

    // Pre-flight check (PPT, slide "Resiliencia"): probar la conexión
    // antes de seguir. No es bloqueante: si falla, se avisa y se puede
    // reintentar o continuar igual (por si el motor arranca más tarde).
    DbInstallerTmp := ExpandConstant('{tmp}\{#DbInstallerExeName}');
    if FileExists(DbInstallerTmp) then
    begin
      LogPath := ExpandConstant('{tmp}\preflight.log');
      ConexionOk := Exec(DbInstallerTmp, 'test-connection "' + ServidorElegido + '" "' + LogPath + '"',
                         '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
      if not ConexionOk then
      begin
        if SuppressibleMsgBox(
             'No se pudo conectar a ''' + ServidorElegido + '''.' + #13#13 +
             'Verificá el nombre del servidor/instancia y que el servicio esté en ejecución.' + #13#13 +
             '¿Querés continuar de todos modos?',
             mbConfirmation, MB_YESNO, IDNO) = IDNO then
        begin
          Result := False;
          exit;
        end;
      end
      else if not VerificarPermisosPreflight(ServidorElegido) then
      begin
        Result := False;
        exit;
      end;
    end;
  end;
end;

// LocalDB (si el usuario eligio instalarlo) se instala recien acá: el usuario
// ya confirmo en "Listo para instalar". Si falla, Setup se detiene con el
// mensaje antes de copiar ningun archivo de WardrobeFlow.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ErrMsg: String;
begin
  Result := '';
  if InstalarLocalDbPendiente then
  begin
    WizardForm.PreparingLabel.Caption := 'Instalando SQL Server Express LocalDB...';
    if InstalarLocalDb(ErrMsg) then
      InstalarLocalDbPendiente := False
    else
      Result := ErrMsg;
  end;
end;

// ---------------------------------------------------------------------
// Ejecucion del script SQL / actualizacion del connection string
// ---------------------------------------------------------------------

// Busca SubStr en S empezando en la posicion Desde (1-based). Se usa en vez
// de PosEx porque no esta garantizado que este disponible en Pascal Script.
// AnsiString (no String/Unicode) a proposito: opera byte a byte sobre el
// archivo UTF-8 sin decodificarlo (ver ActualizarConnectionString).
function PosDesde(const SubStr, S: AnsiString; Desde: Integer): Integer;
var
  I, Len: Integer;
begin
  Result := 0;
  Len := Length(SubStr);
  if Len = 0 then exit;
  for I := Desde to Length(S) - Len + 1 do
  begin
    if Copy(S, I, Len) = SubStr then
    begin
      Result := I;
      exit;
    end;
  end;
end;

// Reemplaza el valor de "Data Source=...;" dentro de un connection string,
// dejando el resto (Initial Catalog, Integrated Security, etc.) intacto.
function ReemplazarDataSource(const Contenido, NuevoServidor: AnsiString): AnsiString;
var
  Inicio, Fin: Integer;
  Marca: AnsiString;
begin
  Result := Contenido;
  Marca := 'Data Source=';
  Inicio := Pos(Marca, Contenido);
  if Inicio = 0 then exit;
  Inicio := Inicio + Length(Marca);
  Fin := PosDesde(';', Contenido, Inicio);
  if Fin = 0 then exit;
  Result := Copy(Contenido, 1, Inicio - 1) + NuevoServidor + Copy(Contenido, Fin, Length(Contenido) - Fin + 1);
end;

// Reescribe {app}\GUI.exe.config con el servidor realmente elegido — así el
// connection string queda embebido (no ".env") pero correcto para
// CUALQUIER instancia que se haya detectado/elegido/ingresado, no solo
// ".\SQLEXPRESS" fijo como en la Entrega 2. El archivo es UTF-8: se trabaja
// sobre sus bytes y el servidor se codifica en UTF-8 (antes se convertia a
// ANSI y un nombre con acentos quedaba corrupto). Si un config viejo no trae
// Connect Timeout se agrega (30 s: el arranque en frio de LocalDB).
function ActualizarConnectionString(const NuevoServidor: String): Boolean;
var
  RutaConfig: String;
  Contenido, Nuevo: AnsiString;
  P: Integer;
begin
  Result := False;
  RutaConfig := ExpandConstant('{app}\{#MyAppExeName}.config');
  if not LoadStringFromFile(RutaConfig, Contenido) then
  begin
    SuppressibleMsgBox('No se encontró (o no se pudo leer) ' + RutaConfig + '.' + #13#13 +
                       'WardrobeFlow no va a poder conectarse a la base. Reinstalá la aplicación.',
                       mbError, MB_OK, IDOK);
    exit;
  end;
  Nuevo := ReemplazarDataSource(Contenido, UTF8Encode(NuevoServidor));
  if Pos('Connect Timeout', Nuevo) = 0 then
  begin
    P := Pos('TrustServerCertificate=True', Nuevo);
    if P > 0 then
      Insert(';Connect Timeout=30', Nuevo, P + Length('TrustServerCertificate=True'));
  end;
  if not SaveStringToFile(RutaConfig, Nuevo, False) then
  begin
    SuppressibleMsgBox('No se pudo escribir ' + RutaConfig + ' con el servidor elegido (' + NuevoServidor + ').',
                       mbError, MB_OK, IDOK);
    exit;
  end;
  Result := True;
end;

// Log de las operaciones de base. Con LocalDB DbInstaller corre como el usuario
// original (no elevado), que no puede escribir en {app}: su log va a {app}\Logs.
function LogBaseDeDatos(): String;
begin
  if EsLocalDb(ServidorElegido) then
    Result := ExpandConstant('{app}\Logs\install.log')
  else
    Result := ExpandConstant('{app}\install.log');
end;

// Ejecuta DbInstaller.exe ({app}\BD) con los parametros dados. Con LocalDB,
// como el usuario que inicio el instalador (la instancia y la base son de su
// perfil); con un SQL Server de verdad, elevado como siempre.
function EjecutarDbInstaller(const Params: String; var ResultCode: Integer): Boolean;
var
  Exe: String;
begin
  Exe := ExpandConstant('{app}\BD\{#DbInstallerExeName}');
  if EsLocalDb(ServidorElegido) then
    Result := ExecAsOriginalUser(Exe, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
  else
    Result := Exec(Exe, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if not Result then
    ResultCode := -1;
end;

// Ejecuta un script .sql contra ServidorElegido usando el cliente SQL
// embebido (DbInstaller.exe, ADO.NET/SqlClient) copiado en {app}\BD. No
// depende de sqlcmd ni de ninguna herramienta externa. El log queda en
// LogBaseDeDatos() (persistente) para poder diagnosticar después.
function EjecutarScriptSql(NombreArchivo: String; var ErrMsg: String): Boolean;
var
  ResultCode: Integer;
  ScriptPath, LogPath: String;
begin
  ScriptPath := ExpandConstant('{app}\BD\') + NombreArchivo;
  LogPath := LogBaseDeDatos();

  Result := EjecutarDbInstaller('run-script "' + ServidorElegido + '" "' + ScriptPath + '" "' + LogPath + '"', ResultCode);

  if not Result then
  begin
    ErrMsg := 'No se pudo ejecutar DbInstaller.exe (cliente SQL embebido del instalador).';
    exit;
  end;

  if ResultCode <> 0 then
  begin
    Result := False;
    ErrMsg := 'El script ' + NombreArchivo + ' devolvió un error (código ' + IntToStr(ResultCode) + ') contra ''' + ServidorElegido + '''.' + #13#13 +
              'Puede ser un problema de conexión o un error en el script SQL. Revisá el log para más detalle: ' + LogPath;
  end;
end;

// Devuelve las ultimas hasta MaxChars caracteres de Texto (o el texto
// entero si es mas corto) — para mostrar un extracto del log directo en
// el mensaje, sin obligar al usuario a ir a abrir el archivo para
// enterarse de qué pasó.
function UltimosCaracteres(const Texto: AnsiString; MaxChars: Integer): AnsiString;
begin
  if Length(Texto) <= MaxChars then
    Result := Texto
  else
    Result := '(...)' + #13#10 + Copy(Texto, Length(Texto) - MaxChars + 1, MaxChars);
end;

// Copia el log de la base FUERA de {app} (a Documentos) y devuelve un extracto
// de sus ultimas lineas. Si no se puede copiar, el extracto igual se muestra en
// el mensaje: la comunicacion con el usuario no depende de ese archivo.
procedure PreservarLog(const Destino: String; var RutaFinal, Extracto: String);
var
  LogOriginal: String;
  ContenidoLog: AnsiString;
begin
  LogOriginal := LogBaseDeDatos();
  RutaFinal := ExpandConstant('{userdocs}\') + Destino;
  Extracto := '';
  if FileExists(LogOriginal) then
  begin
    if LoadStringFromFile(LogOriginal, ContenidoLog) then
      Extracto := UTF8Decode(UltimosCaracteres(ContenidoLog, 500));
    if not CopyFile(LogOriginal, RutaFinal, False) then
      RutaFinal := LogOriginal + ' (no se pudo copiar a Documentos)';
  end
  else
    RutaFinal := '(no se generó ningún log para este error)';
end;

function TextoBackup(): String;
begin
  if RutaBackupPrevio <> '' then
    Result := 'Antes de tocar la base se hizo un backup completo en:' + #13#10 + RutaBackupPrevio + #13#10 +
              '(en el servidor ' + ServidorElegido + '). Para volver al estado anterior, un administrador de SQL Server ' +
              'puede restaurarlo (RESTORE DATABASE {#MyDatabaseName} FROM DISK = ''<ruta>'' WITH REPLACE).'
  else
    Result := '';
end;

// Rollback automático (PPT, slide "Resiliencia y Flujo UX": "Rollback
// Automático: Deshacer cambios si la creación de la BD o las tablas falla
// a mitad del proceso"). Solo para una instalación NUEVA (en una
// actualización se usa FallarActualizacion). En vez de dejar la app
// instalada pero rota:
//   1) Preserva el log FUERA de {app} (en Documentos), porque {app} está
//      por desaparecer (ver PreservarLog).
//   2) Avisa con un mensaje estructurado: qué pasó, qué se hizo, qué hacer.
//   3) Corre el desinstalador silenciosamente (a esta altura ya existe:
//      Inno lo genera durante la copia de archivos, antes de ssPostInstall).
//      Con /SUPPRESSMSGBOXES, la pregunta de "¿borrar también la BD?" del
//      desinstalador se autorresponde con su default (No) — no tiene
//      sentido preguntar por una BD que puede ni haberse llegado a crear.
//   4) Termina el proceso: nunca vuelve, así que no hace falta "exit"
//      después de llamarla.
// PuedeHaberBDParcial: True solo cuando ya se llegó a intentar correr el
// script (a diferencia de un servicio caído, donde nunca se tocó la BD) —
// el script no está envuelto en una única transacción (CREATE DATABASE no
// puede estarlo), así que un fallo a mitad de camino puede dejar objetos
// creados a medias en el servidor.
procedure RevertirInstalacion(const MensajeError: String; const PuedeHaberBDParcial: Boolean);
var
  LogPreservado, ExtractoLog, MensajeFinal: String;
  ResultCode: Integer;
begin
  PreservarLog('WardrobeFlow_instalacion_fallida.log', LogPreservado, ExtractoLog);

  MensajeFinal :=
    'QUÉ PASÓ:' + #13#10 + MensajeError + #13#13;

  if PuedeHaberBDParcial then
    MensajeFinal := MensajeFinal +
      'Como el script de creación de la base no se ejecuta dentro de una única transacción, ' +
      'es posible que hayan quedado objetos creados a medias en el servidor.' + #13#13;
  if TextoBackup() <> '' then
    MensajeFinal := MensajeFinal + TextoBackup() + #13#13;

  MensajeFinal := MensajeFinal +
    'QUÉ SE HIZO:' + #13#10 +
    'Se deshace la instalación (rollback automático) para no dejar la aplicación instalada pero no funcional.' + #13#13 +
    'QUÉ HACER AHORA:' + #13#10 +
    'Solucioná el problema descripto arriba y volvé a ejecutar el instalador. ' +
    'Log completo: ' + LogPreservado;

  if ExtractoLog <> '' then
    MensajeFinal := MensajeFinal + #13#13 + 'DETALLE TÉCNICO (últimas líneas del log):' + #13#10 + ExtractoLog;

  SuppressibleMsgBox(MensajeFinal, mbError, MB_OK, IDOK);

  Exec(ExpandConstant('{uninstallexe}'), '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  TerminarSetup(1);
end;

// Falla durante una ACTUALIZACION (habia una version instalada). NO se corre
// el desinstalador: borraria la instalacion anterior entera y el servidor
// guardado en el registro. Se informa el error, se indica donde quedo el
// backup previo y se termina. Nunca vuelve.
procedure FallarActualizacion(const MensajeError: String);
var
  LogPreservado, ExtractoLog, MensajeFinal: String;
begin
  PreservarLog('WardrobeFlow_actualizacion_fallida.log', LogPreservado, ExtractoLog);

  MensajeFinal :=
    'QUÉ PASÓ:' + #13#10 + MensajeError + #13#13 +
    'La actualización a la versión {#MyAppVersion} no se completó. La base de datos puede haber quedado ' +
    'actualizada a medias.' + #13#13;
  if TextoBackup() <> '' then
    MensajeFinal := MensajeFinal + TextoBackup() + #13#13
  else
    MensajeFinal := MensajeFinal + 'No se llegó a modificar la base (no hizo falta backup).' + #13#13;

  MensajeFinal := MensajeFinal +
    'QUÉ SE HIZO:' + #13#10 +
    'No se desinstaló nada (la instalación y la configuración del servidor se conservan).' + #13#13 +
    'QUÉ HACER AHORA:' + #13#10 +
    'Solucioná el problema descripto arriba y volvé a ejecutar este instalador (el script es idempotente y ' +
    'continúa la actualización). Si preferís volver atrás, restaurá el backup indicado. ' +
    'Log completo: ' + LogPreservado;

  if ExtractoLog <> '' then
    MensajeFinal := MensajeFinal + #13#13 + 'DETALLE TÉCNICO (últimas líneas del log):' + #13#10 + ExtractoLog;

  SuppressibleMsgBox(MensajeFinal, mbError, MB_OK, IDOK);
  TerminarSetup(1);
end;

// Falla despues de copiar archivos: en una actualizacion no se desinstala.
procedure FallarInstalacion(const MensajeError: String; const PuedeHaberBDParcial: Boolean);
begin
  if EsActualizacion() then
    FallarActualizacion(MensajeError)
  else
    RevertirInstalacion(MensajeError, PuedeHaberBDParcial);
end;

// ---------------------------------------------------------------------
// Instalacion: reescribir config, chequear servicio, crear la BD
// ---------------------------------------------------------------------
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrMsg, Avisos: String;
  ResultCode: Integer;
  LogPath: String;
begin
  if CurStep = ssPostInstall then
  begin
    ActualizarConnectionString(ServidorElegido);
    LogPath := LogBaseDeDatos();

    // Servicio SQL Detenido (PPT slide "A01.1: Servicio SQL Detenido"):
    // solo aplica a instancias detectadas localmente (ServicioWindowsElegido
    // vacío = instancia manual/posiblemente remota, no hay servicio local
    // que chequear). Esto es una SEGUNDA verificación — la primera ya pasó
    // en el wizard (VerificarServicioPreflight, antes de copiar archivos);
    // esta es la red de seguridad final antes de tocar la base de datos.
    if ServicioWindowsElegido <> '' then
    begin
      WizardForm.StatusLabel.Caption := 'Verificando el servicio de SQL Server...';
      if not Exec(ExpandConstant('{app}\BD\{#DbInstallerExeName}'),
                  'check-service "' + ServicioWindowsElegido + '" "' + LogPath + '"',
                  '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        ResultCode := -1; // no se pudo ni siquiera lanzar DbInstaller.exe

      if ResultCode = 2 then
        FallarInstalacion(
          'No se encontró el servicio de Windows ''' + ServicioWindowsElegido + ''' en este equipo. ' +
          'La instancia de SQL Server puede haberse desinstalado.', False)
      else if ResultCode <> 0 then
        FallarInstalacion(
          'El servicio de SQL Server (' + ServicioWindowsElegido + ') está detenido y no se pudo iniciar ' +
          'automáticamente (¿faltan permisos de administrador?).', False);
    end;

    // Backup previo: si la base ya existe (actualización o reinstalación), se
    // respalda ANTES de que el script la modifique. Código 4 = la base no
    // existe (instalación nueva): no hay nada que respaldar.
    WizardForm.StatusLabel.Caption := 'Respaldando la base de datos existente...';
    RutaBackupPrevio := '';
    EjecutarDbInstaller('backup "' + ServidorElegido + '" "{#MyDatabaseName}" "{#MyAppVersion}" "' + LogPath + '"', ResultCode);
    if ResultCode = 0 then
      RutaBackupPrevio := LeerSalidaDbInstaller(LogPath)
    else if ResultCode <> 4 then
      FallarInstalacion('No se pudo hacer el backup de la base existente antes de actualizarla, así que NO se ' +
                        'modificó la base. Revisá el espacio en disco y los permisos de la carpeta de backups ' +
                        'de SQL Server. Log: ' + LogPath, False);

    WizardForm.StatusLabel.Caption := 'Creando o actualizando la base de datos WardrobeFlowDB...';

    // Script único (BD/00_Instalacion_Completa.sql): crea la base y TODOS
    // los módulos de una sola pasada.
    if not EjecutarScriptSql('00_Instalacion_Completa.sql', ErrMsg) then
      FallarInstalacion('No se pudo completar la creación/actualización de la base de datos.' + #13#13 + ErrMsg, True);

    // Los AVISO: del script (archivos huérfanos adjuntados, datos de prueba
    // omitidos, etc.) se muestran: no frenan la instalación pero el usuario
    // tiene que enterarse.
    Avisos := LeerSalidaDbInstaller(LogPath);
    if Avisos <> '' then
      SuppressibleMsgBox('La base de datos se instaló con estos avisos:' + #13#13 + Avisos + #13#13 +
                         'Detalle en: ' + LogPath, mbInformation, MB_OK, IDOK);

    // Acceso para cualquier usuario de Windows del equipo (ver
    // OtorgarAccesoUsuariosLocales en DbInstaller), con mínimo privilegio:
    // cubre el caso de instalar con la cuenta de otro administrador (UAC) y
    // abrir la app con la propia. No aplica a LocalDB (es una instancia
    // privada de cada usuario) y no es bloqueante: el que instaló ya tiene
    // acceso igual.
    if not EsLocalDb(ServidorElegido) then
    begin
      WizardForm.StatusLabel.Caption := 'Otorgando acceso a los usuarios del equipo...';
      EjecutarDbInstaller('grant-users "' + ServidorElegido + '" "{#MyDatabaseName}" "' + LogPath + '"', ResultCode);
      if ResultCode <> 0 then
        SuppressibleMsgBox(
          'La base quedó instalada, pero no se pudo dar acceso al grupo de usuarios de Windows del equipo ' +
          '(código ' + IntToStr(ResultCode) + ').' + #13#13 +
          'La aplicación va a funcionar para la cuenta que instaló; otros usuarios de Windows no van a poder ' +
          'conectarse hasta que un administrador de SQL Server les dé acceso. Detalle en: ' + LogPath,
          mbError, MB_OK, IDOK);
    end;

    // Smoke test final: la base responde con la misma forma de conexion que
    // usa la app y tiene el admin semilla y datos para operar. Si no, rollback
    // (instalación nueva) o aviso con el backup (actualización).
    WizardForm.StatusLabel.Caption := 'Verificando la instalación...';
    EjecutarDbInstaller('verify "' + ServidorElegido + '" "{#MyDatabaseName}" "' + LogPath + '"', ResultCode);
    if ResultCode <> 0 then
      FallarInstalacion('La base de datos se creó pero la verificación final falló ' +
                        '(no se encontró el usuario admin inicial o faltan datos).', True);
  end;
end;

// Guarda el servidor elegido para poder usarlo despues, al desinstalar
// (ver CurUninstallStepChanged) y en la proxima actualizacion.
procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'ServidorSql', ServidorElegido);
end;

// ---------------------------------------------------------------------
// Desinstalacion: preguntar (default NO) si tambien se quiere borrar la BD
// ---------------------------------------------------------------------
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
  LogPath, LogCopia, ServidorGuardado, Nota: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Se pregunta ANTES de que Inno borre los archivos — DbInstaller.exe
    // todavía está en {app}\BD en este punto, lo necesitamos para poder
    // borrar la base. Por defecto NO se borra (acción destructiva e
    // irreversible). MB_DEFBUTTON2 deja el foco en "No"; el IDNO final es
    // solo la respuesta por defecto en desinstalación silenciosa.
    if SuppressibleMsgBox(
         '¿Querés borrar también la base de datos WardrobeFlowDB?' + #13#13 +
         'Esta acción NO se puede deshacer. Si no estás seguro, elegí "No": la base va a quedar en el servidor aunque desinstales la aplicación.',
         mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES then
    begin
      ServidorGuardado := GetPreviousData('ServidorSql', '{#MySqlInstanceSugerido}');
      LogPath := ExpandConstant('{app}\uninstall.log');
      // drop-database borra la base y, si ninguna otra base lo usa, el login
      // del grupo "Usuarios" que creó grant-users.
      if not Exec(ExpandConstant('{app}\BD\{#DbInstallerExeName}'),
                  'drop-database "' + ServidorGuardado + '" "{#MyDatabaseName}" "' + LogPath + '"',
                  '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        ResultCode := -1;
      Nota := '';
      // LocalDB es por usuario: el desinstalador (elevado) ve el LocalDB de
      // la cuenta que eleva. Si la base era de otro usuario de Windows, ese
      // usuario tiene que borrarla desde su sesión.
      if EsLocalDb(ServidorGuardado) then
        Nota := #13#13 + 'Nota: con SQL LocalDB solo se borra la base del usuario de Windows que ejecuta el ' +
                'desinstalador. Si la base era de otro usuario, tiene que borrarla esa persona desde su sesión.';
      if ResultCode <> 0 then
      begin
        // El uninstall.log se borra con {app}: se conserva una copia.
        LogCopia := ExpandConstant('{userdocs}\WardrobeFlow_desinstalacion.log');
        if not CopyFile(LogPath, LogCopia, False) then
          LogCopia := LogPath + ' (no se pudo copiar; se borra al terminar)';
        SuppressibleMsgBox(
          'No se pudo borrar la base de datos {#MyDatabaseName} en ''' + ServidorGuardado + ''' ' +
          '(código ' + IntToStr(ResultCode) + '). La aplicación se desinstala igual; la base quedó en el servidor.' + #13#13 +
          'Detalle en: ' + LogCopia + Nota,
          mbError, MB_OK, IDOK);
      end
      else if Nota <> '' then
        SuppressibleMsgBox('Base de datos eliminada.' + Nota, mbInformation, MB_OK, IDOK);
    end;
  end;
end;
