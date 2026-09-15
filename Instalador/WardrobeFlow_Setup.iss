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
; - Si no hay NINGUNA instancia (ni LocalDB) y el usuario no quiere
;   ingresar un servidor a mano: se le muestra el link de descarga de SQL
;   Server Express/LocalDB y se corta la instalacion sin copiar nada (PPT
;   slide "Sin Motor de Base de Datos": "Asistente Guiado").
; - Si la instancia elegida existe pero su servicio de Windows esta
;   detenido, se intenta arrancarlo automaticamente (PPT slide "Servicio
;   SQL Detenido": ServiceController + Start()); si no se puede (permisos),
;   se avisa con instrucciones. Se chequea DOS veces: como pre-flight en el
;   wizard (antes de copiar archivos) y de nuevo antes de crear la BD.
; - Pre-flight check de conexion (servidor ingresado a mano) y de servicio
;   (instancia detectada), ambos ANTES de copiar archivos (PPT slide
;   "Resiliencia": "antes de alterar el sistema"). El chequeo de espacio en
;   disco lo hace Inno Setup por si solo (comportamiento nativo).
; - Rollback automatico: si el servicio no arranca o falla la creacion de
;   la BD DESPUES de copiar archivos, se desinstala todo lo recien copiado
;   automaticamente (preservando el log en Documentos) en vez de dejar la
;   app instalada pero no funcional (PPT slide "Resiliencia": "Rollback
;   Automatico").
; - El App.config (GUI.exe.config) instalado se reescribe con el servidor
;   realmente elegido — asi el connection string queda embebido pero
;   correcto para CUALQUIER instancia, no solo ".\SQLEXPRESS" fijo.
; - Al desinstalar, se pregunta (por defecto "No") si tambien se quiere
;   borrar la base de datos.
;
; Fuera de alcance (gap conocido, no bloqueante):
; - Firma digital del .exe con SignTool (requiere certificado de codigo
;   propio; no es algo que se pueda generar/conseguir automaticamente).
; - Instalacion silenciosa/embebida de SQL Server Express o LocalDB si NO
;   esta presente en el equipo: se eligio deliberadamente NO embeber ese
;   instalador (~60MB+) y en su lugar detectar+guiar (o usar LocalDB si ya
;   esta instalado), para mantener el instalador liviano.
; =====================================================================

#define MyAppName "WardrobeFlow"
#define MyAppVersion "1.0.0"
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
; Sin esto, un instalador de 32 bits (el default de Inno) en Windows de 64
; bits queda sujeto a la redirección WOW64 del registro: TODA lectura bajo
; SOFTWARE\Microsoft\... (incluso pasando explícitamente la ruta
; WOW6432Node) termina viendo la vista de 32 bits, que es donde SQL Server
; NUNCA se registra. Verificado en este equipo: sin esta línea,
; DetectarInstanciasSql() encuentra 0 instancias aunque SQLEXPRESS esté
; corriendo; con esta línea, lo detecta correctamente.
ArchitecturesInstallIn64BitMode=x64compatible
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
; Segunda copia, solo para {tmp}: permite usar DbInstaller.exe DURANTE el
; wizard (pre-flight de conexion) antes de que se copien los archivos a
; {app}, vía ExtractTemporaryFile — ver PaginaIngresoManual mas abajo.
Source: "{#DbInstallerSourceDir}\{#DbInstallerExeName}"; DestDir: "{tmp}"; Flags: dontcopy
Source: "Credenciales_Iniciales.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Credenciales iniciales"; Filename: "{app}\Credenciales_Iniciales.txt"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\Credenciales_Iniciales.txt"; Description: "Ver las credenciales iniciales"; Flags: postinstall shellexec skipifsilent unchecked
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

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

// ExitProcess no es una funcion built-in de Pascal Script: hay que
// importarla de la WinAPI para poder cortar el setup de una sin copiar
// ningun archivo (usada cuando el usuario dice "no tengo SQL instalado").
procedure ExitProcess(uExitCode: Cardinal);
  external 'ExitProcess@kernel32.dll stdcall';

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

// Lee tanto la ruta nativa de 64 bits como WOW6432Node: Inno Setup compila
// por defecto instaladores de 32 bits, y un proceso de 32 bits en Windows
// de 64 bits es redirigido por el sistema a WOW6432Node al leer
// SOFTWARE\Microsoft\... — como SQL Server (64 bits) solo se registra en
// la rama nativa, sin este doble chequeo el instalador de 32 bits NUNCA
// encontraría una instancia real (verificado en este mismo equipo).
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
// Inexistentes": "Fallback Automático: Intentar la creación de una
// instancia LocalDB en demanda si la arquitectura lo permite"). No
// instalamos LocalDB nosotros (ver decisión de diseño: no embeber el
// instalador de ~60MB) — solo detectamos si YA está presente (algo común
// en máquinas con Visual Studio) para ofrecerlo como opción automática
// antes de pedirle al usuario que instale algo.
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

// Indices de PaginaSinInstancias: cambian segun si se ofrece o no la
// opcion de LocalDB automatico (siempre primera, si esta disponible).
function IndiceLocalDbEnSinInstancias(): Integer;
begin
  Result := 0; // solo valido si LocalDbDisponible
end;

function IndiceManualEnSinInstancias(): Integer;
begin
  if LocalDbDisponible then Result := 1 else Result := 0;
end;

function IndiceCancelarEnSinInstancias(): Integer;
begin
  if LocalDbDisponible then Result := 2 else Result := 1;
end;

// ---------------------------------------------------------------------
// Paginas del wizard
// ---------------------------------------------------------------------
procedure InitializeWizard();
var
  I: Integer;
begin
  InstanciasDetectadas := DetectarInstanciasSql();
  LocalDbDisponible := TieneLocalDbInstalado();

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
    'Se detectaron las siguientes instancias de SQL Server instaladas localmente. Elegí cuál va a usar WardrobeFlow:',
    True, False);
  for I := 0 to GetArrayLength(InstanciasDetectadas) - 1 do
    PaginaSeleccionInstancia.Add(InstanciasDetectadas[I]);
  PaginaSeleccionInstancia.Add('Otra instancia o servidor (ingresar manualmente)');
  if GetArrayLength(InstanciasDetectadas) > 0 then
    PaginaSeleccionInstancia.SelectedValueIndex := 0;

  // Encadenadas por .ID (no todas a wpSelectTasks): crear varias paginas
  // custom con el mismo AfterID puede insertarlas en orden invertido.
  // Caso: no se detecto ninguna instancia local.
  PaginaSinInstancias := CreateInputOptionPage(PaginaSeleccionInstancia.ID,
    'Base de datos', 'No se encontró SQL Server en este equipo',
    'WardrobeFlow necesita una instancia de SQL Server (Express, LocalDB, o una instalación completa) para funcionar. ¿Cómo querés continuar?',
    True, False);
  if LocalDbDisponible then
    PaginaSinInstancias.Add('Usar SQL LocalDB, ya detectado en este equipo ((localdb)\MSSQLLocalDB)');
  PaginaSinInstancias.Add('Ya tengo SQL Server instalado (con otro nombre, o en otro servidor) — ingresar manualmente');
  PaginaSinInstancias.Add('No tengo ningún motor de SQL Server instalado — cancelar la instalación');
  PaginaSinInstancias.SelectedValueIndex := 0;

  // Ingreso manual del servidor\instancia (comun a los dos casos de arriba).
  PaginaIngresoManual := CreateInputQueryPage(PaginaSinInstancias.ID,
    'Base de datos', 'Servidor de SQL Server',
    'Ingresá el servidor y, si corresponde, la instancia (ejemplos: .\SQLEXPRESS, (localdb)\MSSQLLocalDB, MIPC\INSTANCIA):');
  PaginaIngresoManual.Add('Servidor\Instancia:', False);
  PaginaIngresoManual.Values[0] := '{#MySqlInstanceSugerido}';
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if PageID = PaginaSeleccionInstancia.ID then
    Result := GetArrayLength(InstanciasDetectadas) = 0
  else if PageID = PaginaSinInstancias.ID then
    Result := GetArrayLength(InstanciasDetectadas) > 0
  else if PageID = PaginaIngresoManual.ID then
  begin
    if GetArrayLength(InstanciasDetectadas) > 0 then
      // Se salta el ingreso manual salvo que se haya elegido la ultima
      // opcion de la lista ("Otra instancia...").
      Result := PaginaSeleccionInstancia.SelectedValueIndex <> GetArrayLength(InstanciasDetectadas)
    else
      // Sin instancias detectadas: el ingreso manual solo aplica si se
      // eligio esa opcion especifica (el indice depende de si tambien se
      // ofrece LocalDB automatico).
      Result := PaginaSinInstancias.SelectedValueIndex <> IndiceManualEnSinInstancias();
  end;
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

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
  LogPath: String;
  DbInstallerTmp: String;
  IndiceElegido: Integer;
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
      // ningún archivo — no recién en ssPostInstall como en la versión
      // anterior. Si el usuario decide "continuar de todos modos", se
      // vuelve a intentar más tarde igual (con rollback si falla ahí).
      if not VerificarServicioPreflight(ServicioWindowsElegido) then
      begin
        Result := False;
        exit;
      end;
    end;
    // Si eligió "Otra..." (el último índice), ServidorElegido se define
    // más abajo, en PaginaIngresoManual.
  end;

  if CurPageID = PaginaSinInstancias.ID then
  begin
    if LocalDbDisponible and (PaginaSinInstancias.SelectedValueIndex = IndiceLocalDbEnSinInstancias()) then
    begin
      // Fallback automático a LocalDB (PPT slide "Instancias SQL
      // Inexistentes"): ya está instalado en este equipo, no hace falta
      // pedirle nada más al usuario. LocalDB no corre como servicio de
      // Windows, así que no hay nada que chequear/arrancar acá.
      ServidorElegido := '(localdb)\MSSQLLocalDB';
      ServicioWindowsElegido := '';
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
      ExitProcess(1);
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

    // Pre-flight check (PPT, slide "Resiliencia"): probar la conexión
    // antes de seguir. No es bloqueante: si falla, se avisa y se puede
    // reintentar o continuar igual (por si el motor arranca más tarde).
    DbInstallerTmp := ExpandConstant('{tmp}\{#DbInstallerExeName}');
    if FileExists(DbInstallerTmp) then
    begin
      WizardForm.StatusLabel.Caption := 'Probando conexión...';
      LogPath := ExpandConstant('{tmp}\preflight.log');
      if not (Exec(DbInstallerTmp, 'test-connection "' + ServidorElegido + '" "' + LogPath + '"',
                    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0)) then
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
      end;
    end;
  end;
end;

// ---------------------------------------------------------------------
// Ejecucion del script SQL / actualizacion del connection string
// ---------------------------------------------------------------------

// Busca SubStr en S empezando en la posicion Desde (1-based). Se usa en vez
// de PosEx porque no esta garantizado que este disponible en Pascal Script.
// AnsiString (no String/Unicode) a proposito: opera byte a byte para no
// tener que decodificar/recodificar el archivo (ver ActualizarConnectionString).
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
// ".\SQLEXPRESS" fijo como en la Entrega 2.
procedure ActualizarConnectionString(const NuevoServidor: String);
var
  RutaConfig: String;
  Contenido: AnsiString;
begin
  RutaConfig := ExpandConstant('{app}\{#MyAppExeName}.config');
  if LoadStringFromFile(RutaConfig, Contenido) then
    SaveStringToFile(RutaConfig, ReemplazarDataSource(Contenido, AnsiString(NuevoServidor)), False);
end;

// Ejecuta un script .sql contra ServidorElegido usando el cliente SQL
// embebido (DbInstaller.exe, ADO.NET/SqlClient) copiado en {app}\BD. No
// depende de sqlcmd ni de ninguna herramienta externa. El log queda en
// {app}\install.log (persistente) para poder diagnosticar después.
function EjecutarScriptSql(NombreArchivo: String; var ErrMsg: String): Boolean;
var
  ResultCode: Integer;
  DbInstallerPath, ScriptPath, LogPath, Params: String;
begin
  DbInstallerPath := ExpandConstant('{app}\BD\{#DbInstallerExeName}');
  ScriptPath := ExpandConstant('{app}\BD\') + NombreArchivo;
  LogPath := ExpandConstant('{app}\install.log');

  Params := 'run-script "' + ServidorElegido + '" "' + ScriptPath + '" "' + LogPath + '"';

  Result := Exec(DbInstallerPath, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

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

// Rollback automático (PPT, slide "Resiliencia y Flujo UX": "Rollback
// Automático: Deshacer cambios si la creación de la BD o las tablas falla
// a mitad del proceso"). En vez de dejar la app instalada pero rota:
//   1) Preserva el install.log FUERA de {app} (en Documentos), porque
//      {app} está por desaparecer.
//   2) Avisa con un mensaje claro.
//   3) Corre el desinstalador silenciosamente (a esta altura ya existe:
//      Inno lo genera durante la copia de archivos, antes de ssPostInstall).
//      Con /SUPPRESSMSGBOXES, la pregunta de "¿borrar también la BD?" del
//      desinstalador se autorresponde con su default (No) — no tiene
//      sentido preguntar por una BD que puede ni haberse llegado a crear.
//   4) Termina el proceso: nunca vuelve, así que no hace falta "exit"
//      después de llamarla.
procedure RevertirInstalacion(const MensajeError: String);
var
  LogPreservado: String;
  ResultCode: Integer;
begin
  LogPreservado := ExpandConstant('{userdocs}\WardrobeFlow_instalacion_fallida.log');
  if FileExists(ExpandConstant('{app}\install.log')) then
    CopyFile(ExpandConstant('{app}\install.log'), LogPreservado, False);

  SuppressibleMsgBox(
    MensajeError + #13#13 +
    'Se va a deshacer la instalación (rollback automático) para no dejar la aplicación a medio instalar.' + #13#13 +
    'El detalle quedó guardado en: ' + LogPreservado + #13#13 +
    'Solucioná el problema y volvé a ejecutar el instalador.',
    mbError, MB_OK, IDOK);

  Exec(ExpandConstant('{uninstallexe}'), '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  ExitProcess(1);
end;

// ---------------------------------------------------------------------
// Instalacion: reescribir config, chequear servicio, crear la BD
// ---------------------------------------------------------------------
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrMsg: String;
  ResultCode: Integer;
  LogPath: String;
begin
  if CurStep = ssPostInstall then
  begin
    ActualizarConnectionString(ServidorElegido);

    // Servicio SQL Detenido (PPT slide "A01.1: Servicio SQL Detenido"):
    // solo aplica a instancias detectadas localmente (ServicioWindowsElegido
    // vacío = instancia manual/posiblemente remota, no hay servicio local
    // que chequear). Esto es una SEGUNDA verificación — la primera ya pasó
    // en el wizard (VerificarServicioPreflight, antes de copiar archivos);
    // esta es la red de seguridad final antes de tocar la base de datos.
    if ServicioWindowsElegido <> '' then
    begin
      WizardForm.StatusLabel.Caption := 'Verificando el servicio de SQL Server...';
      LogPath := ExpandConstant('{app}\install.log');
      if not Exec(ExpandConstant('{app}\BD\{#DbInstallerExeName}'),
                  'check-service "' + ServicioWindowsElegido + '" "' + LogPath + '"',
                  '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        ResultCode := -1; // no se pudo ni siquiera lanzar DbInstaller.exe

      if ResultCode = 2 then
        RevertirInstalacion(
          'No se encontró el servicio de Windows ''' + ServicioWindowsElegido + ''' en este equipo. ' +
          'La instancia de SQL Server puede haberse desinstalado.')
      else if ResultCode <> 0 then
        RevertirInstalacion(
          'El servicio de SQL Server (' + ServicioWindowsElegido + ') está detenido y no se pudo iniciar ' +
          'automáticamente (¿faltan permisos de administrador?).');
    end;

    WizardForm.StatusLabel.Caption := 'Creando la base de datos WardrobeFlowDB...';

    // Script único (BD/00_Instalacion_Completa.sql): crea la base y TODOS
    // los módulos de una sola pasada.
    if not EjecutarScriptSql('00_Instalacion_Completa.sql', ErrMsg) then
      RevertirInstalacion('No se pudo completar la creación de la base de datos.' + #13#13 + ErrMsg);
  end;
end;

// Guarda el servidor elegido para poder usarlo despues, al desinstalar
// (ver CurUninstallStepChanged) — se llama automaticamente justo antes de
// empezar a copiar archivos, cuando ServidorElegido ya esta definido.
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
  LogPath, ServidorGuardado: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Se pregunta ANTES de que Inno borre los archivos — DbInstaller.exe
    // todavía está en {app}\BD en este punto, lo necesitamos para poder
    // borrar la base. Por defecto NO se borra (acción destructiva e
    // irreversible: el foco del MsgBox queda en "No").
    if SuppressibleMsgBox(
         '¿Querés borrar también la base de datos WardrobeFlowDB?' + #13#13 +
         'Esta acción NO se puede deshacer. Si no estás seguro, elegí "No": la base va a quedar en el servidor aunque desinstales la aplicación.',
         mbConfirmation, MB_YESNO, IDNO) = IDYES then
    begin
      ServidorGuardado := GetPreviousData('ServidorSql', '{#MySqlInstanceSugerido}');
      LogPath := ExpandConstant('{app}\uninstall.log');
      Exec(ExpandConstant('{app}\BD\{#DbInstallerExeName}'),
           'drop-database "' + ServidorGuardado + '" "{#MyDatabaseName}" "' + LogPath + '"',
           '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end;
  end;
end;
