// Diagramas de secuencia de los módulos de seguridad y base (T02–T08). Cada mensaje corresponde a un método real del código.
const A = (id, etiqueta) => ({ id, etiqueta, actor: true });
const P = (id, etiqueta) => ({ id, etiqueta });
const c = (de, a, msg) => ({ de, a, msg });
const r = (de, a, ret) => ({ de, a, ret });
const nota = (texto, ...sobre) => ({ nota: texto, sobre });

module.exports = [
  {
    tipo: 'secuencia', id: 'DSS_T02_1_Login', titulo: 'T02.1 · Login del sistema',
    participantes: [A('U', 'Usuario'), P('F', 'Login'), P('B', 'BLL.Usuario'), P('S', 'ContadorSesion'), P('D', 'DAL.Usuario'), P('E', 'Encriptador'), P('M', 'SessionManager'), P('L', 'Bitácora')],
    pasos: [
      c('U', 'F', 'Ingresa usuario y contraseña (btnIngresar_Click)'),
      c('F', 'B', 'Login(modulo, username, contraseña)'),
      c('B', 'D', 'ObtenerPorUsername(username)'),
      r('D', 'B', 'usuario (o null)'),
      { alt: 'Usuario inexistente', pasos: [
        c('B', 'E', 'VerificacionSenuelo(contraseña)'), c('B', 'S', 'RegistrarIntento()'), c('B', 'L', 'RegistrarSinSesion(IntentoFallidoLogin)'),
        r('B', 'F', 'LoginException(CredencialesInvalidas)') ],
        sino: [{ etiqueta: 'Usuario existente', pasos: [
          c('B', 'E', 'VerificarContrasena(contraseña, hash)'),
          { alt: 'Contraseña correcta', pasos: [
            c('B', 'S', 'Resetear()'), c('B', 'D', 'ResetearIntentosFallidos(username)'),
            c('B', 'M', 'Login(usuario)  [permisos efectivos resueltos del árbol Composite]'), c('B', 'L', 'Registrar(InicioSesion)'),
            r('B', 'F', 'true'), r('F', 'U', 'Abre el menú principal') ],
            sino: [{ etiqueta: 'Contraseña incorrecta', pasos: [
              c('B', 'S', 'RegistrarIntento()'), c('B', 'D', 'IncrementarIntentosFallidos(username)'),
              { alt: '3 intentos fallidos', pasos: [ c('B', 'D', 'BloquearConTiempo(id)  [1, 5, 15 o 60 min; luego permanente]'), c('B', 'L', 'RegistrarSinSesion(BloqueoDeCuenta)'), r('B', 'F', 'LoginException(CuentaBloqueada)') ],
                sino: [{ etiqueta: 'Menos de 3', pasos: [ r('B', 'F', 'LoginException(CredencialesInvalidas)') ] }] }
            ] }] }
        ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T02_2_Logout', titulo: 'T02.2 · Logout del sistema',
    participantes: [A('U', 'Usuario'), P('F', 'Menu'), P('B', 'BLL.Usuario'), P('L', 'Bitácora'), P('M', 'SessionManager')],
    pasos: [
      c('U', 'F', 'Elige Cerrar sesión'),
      c('F', 'B', 'Logout(modulo)'),
      c('B', 'L', 'Registrar(CierreSesion)'),
      c('B', 'M', 'Logout()  [descarta la sesión; idempotente]'),
      r('B', 'F', 'sesión cerrada'),
      r('F', 'U', 'Vuelve a la pantalla de Login')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T04_AsignarPermiso', titulo: 'T04 · Asignar un permiso o rol a un rol (Composite)',
    participantes: [A('A', 'Administrador'), P('F', 'GestorPermisos'), P('B', 'BLL.Familia'), P('D', 'DAL.Permiso'), P('L', 'Bitácora')],
    pasos: [
      c('A', 'F', 'Selecciona un rol y un permiso o rol asignable (BtnAsignar_Click)'),
      c('F', 'B', 'AgregarComponente(idPadre, idHijo)'),
      nota('VerificarPuedeGestionar() · un componente no puede contenerse a sí mismo · ValidarSinCiclo()', 'B'),
      c('B', 'D', 'AgregarRelacion(idPadre, idHijo)'),
      c('B', 'L', 'Registrar(Relación creada, Criticidad.Alta)'),
      r('B', 'F', 'relación creada'),
      r('F', 'A', 'Refresca el árbol de permisos')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T05_CambiarIdioma', titulo: 'T05 · Cambio de idioma (Observer)',
    participantes: [A('U', 'Usuario'), P('F', 'Login'), P('I', 'BLL.IdiomaService'), P('T', 'DAL.Traduccion'), P('G', 'GestorIdioma'), P('O', 'IIdiomaObserver')],
    pasos: [
      c('U', 'F', 'Elige un idioma'),
      c('F', 'I', 'CargarTraducciones(codigoIdioma)'),
      c('I', 'T', 'ObtenerDiccionario(codigoIdioma)'),
      r('T', 'I', 'diccionario clave → texto'),
      r('I', 'F', 'traducciones'),
      c('F', 'G', 'CambiarIdioma(idioma, traducciones)'),
      c('G', 'O', 'UpdateLanguage(idioma)  [a cada formulario suscripto]'),
      r('O', 'U', 'Los formularios abiertos se muestran traducidos')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T05_GuardarTraduccion', titulo: 'T05 · Administrar traducciones',
    participantes: [A('A', 'Administrador'), P('F', 'FormIdiomas'), P('I', 'BLL.IdiomaService'), P('T', 'DAL.Traduccion')],
    pasos: [
      c('A', 'F', 'Edita los textos de un idioma y guarda (BtnGuardar_Click)'),
      c('F', 'I', 'GuardarTraduccion(idControl, idIdioma, texto)'),
      nota('exige el permiso Usuarios · un texto en blanco no se guarda (queda el fallback)', 'I'),
      c('I', 'T', 'GuardarTraduccion(idControl, idIdioma, texto)'),
      r('I', 'F', 'guardado'),
      r('F', 'A', 'Confirma')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T06a_ConsultarBitacora', titulo: 'T06a · Consultar la bitácora del sistema',
    participantes: [A('A', 'Administrador'), P('F', 'Bitacora'), P('B', 'BLL.Bitacora'), P('S', 'Servicios.Bitacora'), P('D', 'DAL.Bitacora')],
    pasos: [
      c('A', 'F', 'Abre la bitácora y filtra por días (BtnUltimosDias_Click)'),
      c('F', 'B', 'UsuarioPuedeVerSistema()'),
      c('F', 'B', 'ObtenerUltimosNDiasSistema(dias)'),
      c('B', 'S', 'ObtenerUltimosNDias(dias)'),
      c('S', 'D', 'ObtenerUltimosNDias(dias)'),
      r('D', 'F', 'registros de bitácora'),
      r('F', 'A', 'Muestra la grilla')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T06a_RegistrarBitacora', titulo: 'T06a · Registrar un evento en la bitácora',
    participantes: [P('B', 'Operación de la BLL'), P('S', 'Servicios.Bitacora'), P('M', 'SessionManager'), P('D', 'DAL.Bitacora')],
    pasos: [
      c('B', 'S', 'Registrar(modulo, actividad, criticidad)'),
      c('S', 'M', 'GetInstance()  [usuario en sesión]'),
      c('S', 'D', 'Registrar(registro)  [fecha, usuario, módulo, actividad, criticidad, IP local]'),
      r('D', 'S', 'ok')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T06b_RestaurarVersion', titulo: 'T06b · Restaurar una versión de un usuario (Memento)',
    participantes: [A('A', 'Administrador'), P('F', 'VersionHistorialForm'), P('B', 'BLL.VersionUsuario'), P('C', 'BLL.CuidadorHistorial'), P('U', 'BE.Usuario'), P('D', 'DAL.Usuario'), P('L', 'Bitácora')],
    pasos: [
      c('A', 'F', 'Elige una versión del historial y restaura (btnRestaurar_Click)'),
      c('F', 'B', 'RestaurarVersion(modulo, idVersion)'),
      nota('exige Administrador', 'B'),
      c('B', 'C', 'Obtener(idVersion)'),
      r('C', 'B', 'memento (IMemento)'),
      c('B', 'D', 'ObtenerPorId(idUsuario)'),
      c('B', 'U', 'RestaurarDesde(memento)'),
      c('B', 'D', 'RestaurarVersion(memento)'),
      c('B', 'C', 'Guardar(idUsuario, memento del estado restaurado)'),
      c('B', 'L', 'Registrar(Restauración a versión, Criticidad.Alta)'),
      r('B', 'F', 'usuario restaurado'),
      r('F', 'A', 'Confirma y actualiza el historial')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T07_VerificarIntegridad', titulo: 'T07 · Verificación de integridad al iniciar (DVH y DVV)',
    participantes: [P('P', 'Program'), P('B', 'BLL.Configuracion'), P('D', 'DAL.DigitoVerificador'), P('K', 'CalculadorDV'), P('V', 'Seguridad.DigitoVerificador')],
    pasos: [
      c('P', 'B', 'VerificarIntegridadDV(resultado)'),
      c('B', 'K', 'Crear()'),
      c('B', 'D', 'ObtenerFilasUsuario()'),
      r('D', 'B', 'filas con su DVH almacenado'),
      c('B', 'D', 'ObtenerDVV("Usuario")'),
      c('B', 'V', 'CalcularDVH(campos de cada fila)'),
      c('B', 'V', 'CalcularDVV(dvhs)'),
      { alt: 'DVH o DVV no coinciden', pasos: [ r('B', 'P', 'false (integridad comprometida; se abre el diagnóstico)') ],
        sino: [{ etiqueta: 'Todo coincide', pasos: [ r('B', 'P', 'true') ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T07_RecuperarIntegridad', titulo: 'T07 · Diagnóstico y reparación de la integridad',
    participantes: [A('A', 'Administrador'), P('F', 'DiagnosticoIntegridadForm'), P('R', 'BLL.RecuperacionIntegridad'), P('D', 'DAL.DigitoVerificador'), P('H', 'DAL.HistorialIntegridad')],
    pasos: [
      c('A', 'F', 'Abre el diagnóstico de integridad'),
      c('F', 'R', 'Diagnosticar()'),
      c('R', 'D', 'ObtenerFilasUsuario()'),
      c('R', 'D', 'ObtenerDVV("Usuario")'),
      r('R', 'F', 'DiagnosticoEspejo (filas alteradas, faltantes, DVV)'),
      { alt: 'Hay alteraciones', pasos: [
        c('A', 'F', 'Elige reparar desde el espejo o asumir la pérdida'),
        c('F', 'R', 'RepararDesdeEspejo() o AsumirPerdida()'),
        c('R', 'D', 'RecalcularTabla(tabla, pkCol, columnas)'),
        c('R', 'H', 'Insertar(entrada)'),
        r('R', 'F', 'integridad restablecida') ],
        sino: [{ etiqueta: 'Sin alteraciones', pasos: [ r('R', 'F', 'Integro = true') ] }] }
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T08_RealizarBackup', titulo: 'T08 · Realizar un backup cifrado',
    participantes: [A('A', 'Administrador'), P('F', 'BackupForm'), P('B', 'BLL.Backup'), P('D', 'DAL.Backup'), P('K', 'CifradorArchivos'), P('L', 'Bitácora')],
    pasos: [
      c('A', 'F', 'Elige carpeta destino y contraseña (btnCrear_Click)'),
      c('F', 'B', 'RealizarBackup(modulo, dirDestino, claveCifrado)'),
      nota('exige Administrador y verifica la integridad antes de respaldar', 'B'),
      c('B', 'D', 'RealizarBackup(tempPlano)'),
      c('B', 'K', 'Cifrar(tempPlano, rutaFinal, claveCifrado)  [.wfbak]'),
      c('B', 'L', 'Registrar(Backup cifrado generado, Criticidad.Alta)'),
      r('B', 'F', 'nombre del archivo'),
      r('F', 'A', 'Confirma (el temporal plano se borra siempre)')
    ]
  },
  {
    tipo: 'secuencia', id: 'DSS_T08_RestaurarBackup', titulo: 'T08 · Restaurar un backup',
    participantes: [A('A', 'Administrador'), P('F', 'BackupForm'), P('B', 'BLL.Backup'), P('K', 'CifradorArchivos'), P('D', 'DAL.Backup'), P('L', 'Bitácora')],
    pasos: [
      c('A', 'F', 'Elige el backup y la contraseña (btnRestaurar_Click)'),
      c('F', 'B', 'RestaurarBackup(modulo, rutaArchivo, claveCifrado)'),
      c('B', 'B', 'EsCifrado(rutaArchivo)'),
      { alt: 'Backup cifrado (.wfbak)', pasos: [
        c('B', 'K', 'Descifrar(rutaArchivo, tempPlano, claveCifrado)'),
        { alt: 'Contraseña incorrecta o archivo dañado', pasos: [ r('B', 'F', 'AppException(clave_invalida)') ],
          sino: [{ etiqueta: 'Descifrado correcto', pasos: [ c('B', 'D', 'RestaurarBackup(tempPlano)') ] }] } ],
        sino: [{ etiqueta: '.bak plano', pasos: [ c('B', 'D', 'RestaurarBackup(rutaArchivo)') ] }] },
      c('B', 'L', 'Registrar(Base de datos restaurada, Criticidad.Alta)'),
      r('B', 'F', 'restaurado'),
      r('F', 'A', 'Confirma')
    ]
  }
];
