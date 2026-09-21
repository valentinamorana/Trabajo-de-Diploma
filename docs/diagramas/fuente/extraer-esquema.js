#!/usr/bin/env node
// Instala BD/00_Instalacion_Completa.sql en una base temporal, extrae tablas, columnas, claves primarias y
// foráneas a fuente/schema.json (con el hash del script) y elimina la base temporal.
// Requisitos: SQL Server (.\SQLEXPRESS), sqlcmd y el instalador compilado (Instalador/DbInstaller/bin/Release).
const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');
const { execFileSync } = require('child_process');
const cs = require('./lib/csharp');

const SERVIDOR = process.env.WF_SQL_SERVER || '.\\SQLEXPRESS';
const DB = 'WFTest_Esquema';
const sqlcmdCandidatos = ['sqlcmd', 'C:\\Program Files\\Microsoft SQL Server\\Client SDK\\ODBC\\170\\Tools\\Binn\\SQLCMD.EXE', 'C:\\Program Files\\Microsoft SQL Server\\Client SDK\\ODBC\\180\\Tools\\Binn\\SQLCMD.EXE'];
const sqlcmd = sqlcmdCandidatos.find(c => { try { execFileSync(c, ['-?'], { stdio: 'ignore' }); return true; } catch { return false; } });
if (!sqlcmd) { console.error('No se encontró sqlcmd.'); process.exit(1); }
const dbInstaller = path.join(cs.RAIZ, 'Instalador', 'DbInstaller', 'bin', 'Release', 'DbInstaller.exe');
if (!fs.existsSync(dbInstaller)) { console.error('Falta compilar el proyecto en Release: ' + dbInstaller); process.exit(1); }

const q = (sql, db) => execFileSync(sqlcmd, ['-S', SERVIDOR, '-E', '-C', ...(db ? ['-d', db] : []), '-y', '0', '-f', '65001', '-Q', 'SET NOCOUNT ON; ' + sql], { encoding: 'utf8', maxBuffer: 1 << 26 });
const json = (sql) => JSON.parse(q(sql, DB).split(/\r?\n/).join('').trim());
const borrar = () => q(`IF DB_ID('${DB}') IS NOT NULL BEGIN ALTER DATABASE ${DB} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ${DB}; END`);

const scriptOriginal = fs.readFileSync(path.join(cs.RAIZ, 'BD', '00_Instalacion_Completa.sql'), 'utf8');
const hash = crypto.createHash('sha1').update(scriptOriginal.replace(/\r\n/g, '\n')).digest('hex');
const tmp = path.join(os.tmpdir(), 'wf_esquema.sql');
fs.writeFileSync(tmp, '\uFEFF' + scriptOriginal.replace(/WardrobeFlowDB/g, DB), 'utf8');

try {
  borrar();
  execFileSync(dbInstaller, ['run-script', SERVIDOR, tmp, path.join(os.tmpdir(), 'wf_esquema.log')], { stdio: 'ignore' });
  const cols = json(`SELECT t.name AS tabla, c.column_id AS ord, c.name AS col, ty.name AS tipo, c.max_length AS len, c.precision AS prec, c.scale AS sc, c.is_nullable AS nulo, c.is_identity AS ident FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id FOR JSON PATH`);
  const pks = json(`SELECT t.name AS tabla, c.name AS col FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id JOIN sys.tables t ON t.object_id=i.object_id WHERE i.is_primary_key=1 FOR JSON PATH`);
  const fks = json(`SELECT fk.name AS fk, tp.name AS hija, cp.name AS colhija, tr.name AS padre, cr.name AS colpadre FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=fk.object_id JOIN sys.tables tp ON tp.object_id=fk.parent_object_id JOIN sys.columns cp ON cp.object_id=fc.parent_object_id AND cp.column_id=fc.parent_column_id JOIN sys.tables tr ON tr.object_id=fk.referenced_object_id JOIN sys.columns cr ON cr.object_id=fc.referenced_object_id AND cr.column_id=fc.referenced_column_id ORDER BY tp.name FOR JSON PATH`);
  fs.writeFileSync(path.join(__dirname, 'schema.json'), JSON.stringify({ bdHash: hash, cols, pks, fks }, null, 1), 'utf8');
  console.log(`schema.json actualizado: ${new Set(cols.map(c => c.tabla)).size} tablas, ${cols.length} columnas, ${fks.length} claves foráneas.`);
} finally { try { borrar(); } catch { /* la base temporal no debe quedar */ } }
