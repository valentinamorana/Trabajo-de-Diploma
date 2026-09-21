// Diagramas de clases de los módulos de seguridad y base (T02–T08). Las clases y sus miembros se leen del código.
const cs = require('../lib/csharp');
const E = (ref, extra = {}) => ({ ref, ...extra });
// Identificador de la caja en el diagrama (igual criterio que lib/modelo.js)
const idDe = (ref) => { const c = cs.buscar(ref); if (!c) throw new Error('Clase no encontrada: ' + ref); return (c.ns === 'BE' || c.ns.startsWith('BE.')) ? c.nombre : (c.ns + '_' + c.nombre).replace(/./g, '_'); };
// Dependencias de uso entre las clases de cada diagrama (quién invoca a quién en el código)
const USOS = {
  CLASES_T02_login_logout: [['GUI.Login', 'BLL.Usuario'], ['BLL.Usuario', 'IUsuarioDAL'], ['BLL.Usuario', 'SessionManager'], ['BLL.Usuario', 'ContadorSesion'], ['BLL.Usuario', 'Encriptador'], ['BLL.Usuario', 'Servicios.Bitacora'], ['BLL.Usuario', 'BE.LoginException'], ['SessionManager', 'BE.SesionException'], ['SessionManager', 'BE.Usuario']],
  CLASES_T04_perfiles: [['GUI.GestorPermisos', 'BLL.Familia'], ['BLL.Familia', 'IPermisoDAL'], ['BLL.Familia', 'BE.Componente']],
  CLASES_T05_idiomas: [['GUI.FormIdiomas', 'BLL.IdiomaService'], ['GUI.FormIdiomas', 'GestorIdioma'], ['GestorIdioma', 'IIdiomaObserver'], ['GestorIdioma', 'Servicios.Multiidioma.Idioma'], ['Traductor', 'GestorIdioma'], ['BLL.IdiomaService', 'BE.Control'], ['BLL.IdiomaService', 'BE.FilaTraduccion']],
  CLASES_T06a_bitacora: [['GUI.Bitacora', 'BLL.Bitacora'], ['BLL.Bitacora', 'Servicios.Bitacora'], ['BLL.Bitacora', 'Servicios.BitacoraNegocio'], ['Servicios.Bitacora', 'BE.Bitacora'], ['Servicios.BitacoraNegocio', 'BE.BitacoraNegocio']],
  CLASES_T06b_control_cambios: [['GUI.VersionHistorialForm', 'BLL.VersionUsuario'], ['BLL.VersionUsuario', 'BLL.CuidadorHistorial'], ['BLL.VersionUsuario', 'BE.Usuario'], ['BLL.VersionUsuario', 'DAL.VersionUsuario'], ['BLL.CuidadorHistorial', 'IMemento'], ['BE.Usuario', 'IMemento']],
  CLASES_T07_digitos_verificadores: [['BLL.Configuracion', 'CalculadorDV'], ['BLL.Configuracion', 'DAL.DigitoVerificador'], ['BLL.RecuperacionIntegridad', 'DAL.DigitoVerificador'], ['BLL.Configuracion', 'DAL.HistorialIntegridad'], ['CalculadorDV', 'ICalculadorDV'], ['CalculadorDV', 'Seguridad.DigitoVerificador'], ['DAL.DigitoVerificador', 'BE.FilaUsuarioDV']],
  CLASES_T08_backup: [['GUI.BackupForm', 'BLL.Backup'], ['BLL.Backup', 'IBackupDAL'], ['BLL.Backup', 'CifradorArchivos'], ['BLL.Backup', 'Servicios.Bitacora'], ['DAL.Backup', 'IBackupDAL']]
};

module.exports = [
  {
    tipo: 'clases', id: 'CLASES_T02_login_logout', titulo: 'Diagrama de clases — T02 Login y Logout', columnas: 3,
    clases: [
      E('GUI.Login', { metodos: 'all' }), E('BLL.Usuario', { metodos: ['Login', 'Logout', 'ObtenerUsuarioActivo', 'ObtenerFechaInicioSesion', 'GuardarPreferenciaIdioma'] }),
      E('BE.Usuario', { attrs: ['Id', 'Username', 'Rol', 'Permisos', 'Bloqueado', 'IntentosFallidos', 'CantidadBloqueos', 'FechaBloqueo', 'IdIdioma'] }),
      E('BE.LoginException', { attrs: 'all' }), E('BE.SesionException', { attrs: 'none' }),
      E('SessionManager', { attrs: 'all', metodos: 'all' }), E('ContadorSesion', { metodos: 'all' }), E('Encriptador', { metodos: ['VerificarContrasena', 'VerificacionSenuelo', 'Hash'] }),
      E('IUsuarioDAL', { metodos: ['ObtenerPorUsername', 'ResetearIntentosFallidos', 'IncrementarIntentosFallidos', 'BloquearConTiempo', 'AutoDesbloquear'] }),
      E('Servicios.Bitacora', { metodos: ['Registrar', 'RegistrarSinSesion'] })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_T04_perfiles', titulo: 'Diagrama de clases — T04 Gestión de perfiles (patrón Composite)', columnas: 3,
    clases: [
      E('GUI.GestorPermisos', { metodos: 'none' }), E('BLL.Familia', { metodos: ['ObtenerArbol', 'ObtenerPermisosEfectivos', 'CrearRol', 'AgregarComponente', 'QuitarComponente', 'GuardarAsignacionRol', 'NoEscalaPrivilegios'] }),
      E('BLL.PermisosAccion', { metodos: 'all' }), E('BE.Componente', { metodos: 'all' }), E('BE.Familia', { metodos: 'all' }), E('BE.Patente', { metodos: 'all' }), E('BE.Rol', { metodos: 'all' }),
      E('IPermisoDAL', { metodos: ['AgregarRelacion', 'ObtenerIdRol'] })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_T05_idiomas', titulo: 'Diagrama de clases — T05 Gestión de múltiples idiomas (patrón Observer)', columnas: 3,
    clases: [
      E('GUI.FormIdiomas', { metodos: 'all' }), E('BLL.IdiomaService', { metodos: ['CargarTraducciones', 'GuardarTraduccion', 'CrearIdioma', 'ActivarIdioma', 'DesactivarIdioma', 'ContarTraduccionesFaltantes'] }),
      E('GestorIdioma', { metodos: 'all' }), E('IIdiomaObserver', { metodos: 'all' }), E('Traductor', { metodos: ['Resolver', 'ObtenerTraducciones', 'ObtenerIdiomaDefault'] }),
      E('Servicios.Multiidioma.Idioma', { attrs: 'all' }), E('BE.Control', { attrs: 'all' }), E('BE.FilaTraduccion', { attrs: 'all' })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_T06a_bitacora', titulo: 'Diagrama de clases — T06a Gestión de bitácora', columnas: 3,
    clases: [
      E('GUI.Bitacora', { metodos: 'none' }), E('BLL.Bitacora', { metodos: 'all' }), E('Servicios.Bitacora', { metodos: 'all' }), E('Servicios.BitacoraNegocio', { metodos: 'all' }),
      E('BE.Bitacora', { attrs: 'all' }), E('BE.BitacoraNegocio', { attrs: 'all' }), E('BE.ActividadesBitacora', { attrs: 'none' })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_T06b_control_cambios', titulo: 'Diagrama de clases — T06b Control de cambios de usuarios (patrón Memento)', columnas: 3,
    clases: [
      E('GUI.VersionHistorialForm', { metodos: 'none' }), E('BLL.VersionUsuario', { metodos: 'all' }), E('BLL.CuidadorHistorial', { metodos: 'all' }),
      E('BE.Usuario', { attrs: ['Id', 'Username', 'Nombre', 'Apellido', 'Email', 'FechaNacimiento'], metodos: ['CrearMemento', 'RestaurarDesde'] }),
      E('BE.VersionUsuario', { attrs: 'all' }), E('IMemento', { metodos: 'all' }), E('DAL.VersionUsuario', { metodos: 'all' })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_T07_digitos_verificadores', titulo: 'Diagrama de clases — T07 Dígitos verificadores', columnas: 3,
    clases: [
      E('BLL.Configuracion', { metodos: ['VerificarIntegridadDV', 'RecalcularIntegridadDV', 'AsegurarIntegridadUsuarios'] }), E('BLL.RecuperacionIntegridad', { metodos: 'all' }),
      E('ICalculadorDV', { metodos: 'all' }), E('CalculadorDV', { metodos: 'all' }), E('Seguridad.DigitoVerificador', { metodos: 'all' }),
      E('DAL.DigitoVerificador', { metodos: 'all' }), E('DAL.HistorialIntegridad', { metodos: 'all' }), E('BE.FilaUsuarioDV', { attrs: 'all' })
    ]
  },
  {
    tipo: 'clases', id: 'CLASES_T08_backup', titulo: 'Diagrama de clases — T08 Backup y restauración', columnas: 3,
    clases: [
      E('GUI.BackupForm', { metodos: 'none' }), E('BLL.Backup', { metodos: 'all' }), E('IBackupDAL', { metodos: 'all' }), E('DAL.Backup', { metodos: ['DirectorioTempSeguro'] }),
      E('CifradorArchivos', { metodos: 'all' }), E('Servicios.Bitacora', { metodos: ['Registrar'] })
    ]
  },
  // DER por módulo de seguridad (las tablas salen de la base real)
  { tipo: 'er', id: 'DER_T02_login', titulo: 'DER — T02 Login y Logout', columnas: 3, tablas: ['Usuario', 'Usuario_Seguridad', 'Empleado', 'Preferencia', 'Bitacora'] },
  { tipo: 'er', id: 'DER_T04_perfiles', titulo: 'DER — T04 Gestión de perfiles', columnas: 3, tablas: ['Permiso', 'PermisoRelacion', 'RolPermiso', 'Usuario'] },
  { tipo: 'er', id: 'DER_T05_idiomas', titulo: 'DER — T05 Gestión de múltiples idiomas', columnas: 3, tablas: ['Idioma', 'Traduccion', 'Control', 'ControlMapeado', 'Preferencia'] },
  { tipo: 'er', id: 'DER_T06_bitacora', titulo: 'DER — T06 Bitácora y control de cambios', columnas: 3, tablas: ['Bitacora', 'BitacoraNegocio', 'HistorialUsuario', 'Usuario'] },
  { tipo: 'er', id: 'DER_T07_dv', titulo: 'DER — T07 Dígitos verificadores', columnas: 3, tablas: ['Usuario', 'DVVertical', 'HistorialIntegridad'] },
  { tipo: 'er', id: 'DER_T08_backup', titulo: 'DER — T08 Backup y restauración (bitácora de las operaciones)', columnas: 2, tablas: ['Bitacora', 'Usuario'] }
];

for (const m of module.exports) {
  if (m.tipo === 'clases' && USOS[m.id]) m.relaciones = USOS[m.id].map(([de, a]) => ({ tipo: (de === 'DAL.Backup' || de === 'CalculadorDV' && a === 'ICalculadorDV') ? 'implementa' : 'depende', de: idDe(de), a: idDe(a) }));
}
