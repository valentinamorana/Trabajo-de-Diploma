#!/usr/bin/env node
// Genera TODOS los diagramas (Mermaid + draw.io) desde una única fuente:
//   • DER            → fuente/schema.json (esquema real de la BD instalada)
//   • Clases         → código C# real (BE/BLL/DAL) leído por lib/csharp.js
//   • Secuencia, actividad y casos de uso → fuente/modelos/*.js (verificados contra el código)
// Uso:  node generar.js [filtro]
const fs = require('fs');
const path = require('path');
const modelo = require('./lib/modelo');
const mmd = require('./lib/mermaid');
const dio = require('./lib/drawio');

const SALIDA = path.resolve(__dirname, '..');
const filtro = process.argv[2];

const modelos = [].concat(
  require('./modelos/er'),
  require('./modelos/er_extra'),
  require('./modelos/clases'),
  require('./modelos/casos'),
  require('./modelos/actividades'),
  require('./modelos/secuencias')
);

for (const d of ['mermaid', 'drawio']) fs.mkdirSync(path.join(SALIDA, d), { recursive: true });

const indice = [];
for (const m0 of modelos) {
  if (filtro && !m0.id.includes(filtro)) continue;
  let m = m0;
  if (m0.tipo === 'er') m = modelo.resolverER(m0);
  else if (m0.tipo === 'clases') m = modelo.resolverClases(m0);
  const f = { er: 'er', clases: 'clases', secuencia: 'secuencia', actividad: 'actividad', casos: 'casos' }[m0.tipo];
  fs.writeFileSync(path.join(SALIDA, 'mermaid', m0.id + '.mmd'), mmd[f](m), 'utf8');
  fs.writeFileSync(path.join(SALIDA, 'drawio', m0.id + '.drawio'), dio[f](m), 'utf8');
  indice.push({ id: m0.id, tipo: m0.tipo, titulo: m0.titulo });
}
fs.writeFileSync(path.join(SALIDA, 'indice.json'), JSON.stringify(indice, null, 2), 'utf8');
console.log(`Generados ${indice.length} diagramas (Mermaid + draw.io) en ${SALIDA}`);
