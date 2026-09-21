// Lector liviano de C#: extrae clases/interfaces/enums con sus propiedades, métodos públicos,
// herencia y dependencias del constructor. Alcanza para diagramar; no es un parser completo.
const fs = require('fs');
const path = require('path');

const RAIZ = path.resolve(__dirname, '..', '..', '..', '..');

function listar(dir, acc = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    if (e.name === 'bin' || e.name === 'obj' || e.name === 'Properties') continue;
    const p = path.join(dir, e.name);
    if (e.isDirectory()) listar(p, acc);
    else if (e.name.endsWith('.cs')) acc.push(p);
  }
  return acc;
}

function limpiarTipo(t) {
  t = t.trim().replace(/\s+/g, ' ');
  // quita namespaces: BE.Builders.ModalidadCobro -> ModalidadCobro (respeta genéricos)
  return t.replace(/\b(?:[A-Za-z_]\w*\.)+([A-Za-z_]\w*)/g, '$1');
}

function cuerpo(texto, idxLlave) {
  let n = 0;
  for (let i = idxLlave; i < texto.length; i++) {
    const c = texto[i];
    if (c === '{') n++;
    else if (c === '}') { n--; if (n === 0) return texto.slice(idxLlave + 1, i); }
  }
  return texto.slice(idxLlave + 1);
}

function sinComentarios(s) {
  return s.replace(/\/\*[\s\S]*?\*\//g, '').replace(/^\s*\/\/.*$/gm, '');
}

function parsearArchivo(ruta) {
  const crudo = fs.readFileSync(ruta, 'utf8').replace(/^﻿/, '');
  const ns = (crudo.match(/namespace\s+([\w.]+)/) || [])[1] || '';
  const res = [];
  const re = /(?:^|\n)\s*(?:\[[^\]]*\]\s*)*(public|internal)\s+((?:abstract |sealed |static |partial )*)(class|interface|enum)\s+(\w+)(?:<[^>]*>)?\s*(?::\s*([^{]+?))?\s*(?:where[^{]+)?\{/g;
  let m;
  while ((m = re.exec(crudo))) {
    const [, vis, mods, tipo, nombre, base] = m;
    const idx = m.index + m[0].length - 1;
    const cruzo = cuerpo(crudo, idx);
    const limpio = sinComentarios(cruzo);
    const info = {
      nombre, tipo, ns, archivo: path.relative(RAIZ, ruta).replace(/\\/g, '/'),
      abstracta: /abstract/.test(mods), estatica: /static/.test(mods),
      bases: base ? base.split(',').map(s => limpiarTipo(s.replace(/<.*>/, m2 => m2))).filter(Boolean) : [],
      props: [], metodos: [], valores: [], ctor: []
    };

    if (tipo === 'enum') {
      info.valores = limpio.split(',').map(s => s.replace(/=[\s\S]*$/, '').trim()).filter(s => /^\w+$/.test(s));
      res.push(info); continue;
    }

    // Propiedades públicas { get; ... }. Se ignora las que el comentario marca como "no persiste".
    const reProp = /((?:\/\/\/[^\n]*\n\s*)*)public\s+(?:virtual\s+|static\s+|override\s+|abstract\s+)*([\w<>\[\],.? ]+?)\s+(\w+)\s*(?:\{\s*get;|=>)/g;
    let p;
    while ((p = reProp.exec(cruzo))) {
      const doc = p[1] || '';
      const nombreProp = p[3];
      if (['class', 'interface', 'enum'].includes(p[2].trim())) continue;
      info.props.push({ nombre: nombreProp, tipo: limpiarTipo(p[2]), join: /no persiste/i.test(doc), calculada: /=>/.test(p[0]) });
    }

    // Métodos públicos (incluye abstract/virtual/override; excluye constructores y propiedades).
    const reMet = /public\s+(?:virtual\s+|static\s+|override\s+|abstract\s+|async\s+)*([\w<>\[\],.?() ]+?)\s+(\w+)\s*\(([^)]*)\)\s*(?:\{|=>|;|:)/g;
    let q;
    while ((q = reMet.exec(limpio))) {
      if (q[2] === nombre) continue;
      if (['if', 'while', 'for', 'switch', 'return', 'new', 'using', 'catch', 'foreach', 'lock'].includes(q[2])) continue;
      if (q[1].trim() === 'new' || q[1].trim() === 'return') continue;
      info.metodos.push({ nombre: q[2], retorno: limpiarTipo(q[1]), params: limpiarTipo(q[3]) });
    }
    // Interfaces: firmas sin "public".
    if (tipo === 'interface') {
      const reI = /^\s*([\w<>\[\],.?() ]+?)\s+(\w+)\s*\(([^)]*)\)\s*;/gm;
      while ((q = reI.exec(limpio))) {
        info.metodos.push({ nombre: q[2], retorno: limpiarTipo(q[1]), params: limpiarTipo(q[3]) });
      }
    }

    // Constructores públicos: parámetros = dependencias.
    const reCtor = new RegExp('public\\s+' + nombre + '\\s*\\(([^)]*)\\)', 'g');
    let c;
    while ((c = reCtor.exec(limpio))) {
      const params = c[1].split(',').map(s => s.trim()).filter(Boolean).map(s => {
        const sinDef = s.replace(/=[\s\S]*$/, '').trim();
        const partes = sinDef.split(/\s+/);
        return limpiarTipo(partes.slice(0, -1).join(' '));
      }).filter(Boolean);
      info.ctor.push(params);
    }
    res.push(info);
  }
  return res;
}

let _cache = null;
function indice() {
  if (_cache) return _cache;
  const mapa = {};
  for (const carpeta of ['BE', 'BLL', 'DAL', 'Seguridad', 'Servicios', 'GUI']) {
    const dir = path.join(RAIZ, carpeta);
    if (!fs.existsSync(dir)) continue;
    for (const f of listar(dir)) {
      for (const c of parsearArchivo(f)) {
        const clave = c.ns + '.' + c.nombre;
        (mapa[c.nombre] = mapa[c.nombre] || []).push({ ...c, clave });
      }
    }
  }
  _cache = mapa;
  return mapa;
}

// Busca una clase por nombre; "BLL.Cliente" desambigua por namespace.
function buscar(ref) {
  const mapa = indice();
  const i = ref.lastIndexOf('.');
  const [a, b] = i >= 0 ? [ref.slice(0, i), ref.slice(i + 1)] : [null, ref];
  const lista = mapa[b] || [];
  if (!lista.length) return null;
  if (a) return lista.find(c => c.ns === a || c.ns.startsWith(a)) || null;
  return lista[0];
}

module.exports = { indice, buscar, RAIZ };

if (require.main === module) {
  const c = buscar(process.argv[2] || 'BE.Contratacion');
  console.log(JSON.stringify(c, null, 1).slice(0, 3000));
}
