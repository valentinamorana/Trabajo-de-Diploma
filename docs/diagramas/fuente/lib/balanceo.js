// Balanceo entre diagramas de secuencia y de clases (corrección del docente en la Entrega 1: "igualar
// métodos del diagrama de clases y del diagrama de secuencia").
// Para cada proceso (N01, PN01..PN04) calcula qué métodos de qué clases citan sus secuencias. El
// generador de clases usa este resultado (metodos: 'auto') y el verificador comprueba que ninguna
// clase o método citado en una secuencia falte en los diagramas de clases del mismo proceso.
const cs = require('./csharp');
const secuencias = require('../modelos/secuencias');

// Clase del código a la que corresponde la etiqueta de un participante, o null si no se puede resolver
// (pantallas, actores, etiquetas compuestas como "DAL.Cliente + DAL.Renovacion").
function claveDe(label) {
  let m;
  if ((m = /^BLL\.(\w+)$/.exec(label))) { const c = cs.buscar('BLL.' + m[1]); return c ? c.ns + '.' + c.nombre : null; }
  if ((m = /^DAL\.(\w+)$/.exec(label))) {
    const i = cs.buscar('I' + m[1] + 'DAL'); if (i) return i.ns + '.' + i.nombre;
    const d = cs.buscar('DAL.' + m[1]); return d ? d.ns + '.' + d.nombre : null;
  }
  if (/^[A-Z]\w*$/.test(label)) { const c = cs.buscar(label); if (c && !/^GUI/.test(c.ns)) return c.ns + '.' + c.nombre; }
  return null;
}

const proceso = (id) => (/^DSS_(N01|PN0\d)_/.exec(id) || [])[1];
const recorrer = (pasos, cb) => { for (const s of pasos) { cb(s); if (s.pasos) recorrer(s.pasos, cb); for (const e of (s.sino || [])) recorrer(e.pasos, cb); } };

let _cache = null;
// { N01: Map(clave -> Set(métodos)), PN01: ..., ... }
function citados() {
  if (_cache) return _cache;
  const res = {};
  for (const d of secuencias) {
    const p = proceso(d.id); if (!p) continue;
    const etiqueta = Object.fromEntries(d.participantes.map(x => [x.id, x.etiqueta]));
    recorrer(d.pasos, (s) => {
      if (!s.msg) return;
      const clave = claveDe(etiqueta[s.a] || '');
      if (!clave) return;
      const clase = cs.buscar(clave);
      const nombres = new Set(clase.metodos.map(x => x.nombre));
      for (const m of s.msg.matchAll(/\b([A-Z][A-Za-z0-9_]+)\(/g)) {
        if (nombres.has(m[1])) {
          res[p] = res[p] || new Map();
          if (!res[p].has(clave)) res[p].set(clave, new Set());
          res[p].get(clave).add(m[1]);
        }
      }
    });
  }
  _cache = res;
  return res;
}

module.exports = { citados, claveDe, proceso };
