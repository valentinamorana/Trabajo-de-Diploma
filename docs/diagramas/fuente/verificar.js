#!/usr/bin/env node
// Verifica que los diagramas sigan coincidiendo con el código y la base:
//   1) el esquema (schema.json) corresponde al script de instalación actual (hash);
//   2) todo método o clase citado en un diagrama de secuencia existe en el código;
//   3) las clases de los diagramas de clases existen (lo comprueba el generador);
//   4) con --regenerar: vuelve a generar mermaid/ y drawio/ (queda todo al día).
// Sale con código 1 si algo está desactualizado. Lo usa el hook pre-commit (.githooks/pre-commit).
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { execFileSync } = require('child_process');
const cs = require('./lib/csharp');

const regenerar = process.argv.includes('--regenerar');
const errores = [];
const avisos = [];

// 1) esquema vs script de instalación
const sqlPath = path.join(cs.RAIZ, 'BD', '00_Instalacion_Completa.sql');
const hashSql = crypto.createHash('sha1').update(fs.readFileSync(sqlPath, 'utf8').replace(/\r\n/g, '\n')).digest('hex');
const esquema = JSON.parse(fs.readFileSync(path.join(__dirname, 'schema.json'), 'utf8').replace(/^﻿/, ''));
if (esquema.bdHash !== hashSql) {
  errores.push('El script BD/00_Instalacion_Completa.sql cambió desde que se extrajo fuente/schema.json.\n' +
    '    Ejecutar:  node docs/diagramas/fuente/extraer-esquema.js   (necesita SQL Server y el proyecto compilado en Release)');
}

// 2) métodos y clases citados en las secuencias
const idx = cs.indice();
const metodos = new Set(), clases = new Set();
for (const lista of Object.values(idx)) for (const c of lista) { clases.add(c.nombre); c.metodos.forEach(m => metodos.add(m.nombre)); }
const permitidos = new Set(['AppException']);
const secuencias = require('./modelos/secuencias');
const recorrer = (pasos, cb) => { for (const s of pasos) { cb(s); if (s.pasos) recorrer(s.pasos, cb); for (const e of (s.sino || [])) recorrer(e.pasos, cb); } };
for (const d of secuencias) {
  for (const p of d.participantes) {
    const m = /^(?:BLL|DAL|BE)\.(\w+)$/.exec(p.etiqueta) || (/^[A-Za-z]\w*$/.test(p.etiqueta) && !p.actor ? [null, p.etiqueta] : null);
    if (m && !clases.has(m[1])) errores.push(`${d.id}: la clase "${p.etiqueta}" ya no existe en el código.`);
  }
  recorrer(d.pasos, (s) => {
    if (!s.msg) return;
    for (const m of s.msg.matchAll(/\b([A-Z][A-Za-z0-9_]+)\(/g)) {
      const n = m[1];
      if (!metodos.has(n) && !clases.has(n) && !permitidos.has(n)) errores.push(`${d.id}: el mensaje "${s.msg.slice(0, 60)}" cita ${n}(), que no existe en el código.`);
    }
  });
}

// 4) regenerar
if (regenerar) {
  try { execFileSync(process.execPath, [path.join(__dirname, 'generar.js')], { stdio: 'inherit' }); }
  catch (e) { errores.push('El generador falló (probablemente una clase de clases.js ya no existe): ' + e.message.split('\n')[0]); }
}

if (avisos.length) console.warn(avisos.map(a => 'AVISO: ' + a).join('\n'));
if (errores.length) {
  console.error('\nDIAGRAMAS DESACTUALIZADOS:\n  - ' + errores.join('\n  - ') + '\n\nActualizar docs/diagramas/fuente/modelos/*.js y volver a ejecutar node docs/diagramas/fuente/verificar.js --regenerar');
  process.exit(1);
}
console.log('Diagramas verificados: coinciden con el código y con la base.');
