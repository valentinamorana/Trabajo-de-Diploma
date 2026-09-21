// Resuelve los modelos de diagrama a una forma "plana" que consumen los emisores (Mermaid y draw.io).
// Así ambos formatos salen exactamente de la misma información, que a su vez sale del código y de la BD.
const fs = require('fs');
const path = require('path');
const cs = require('./csharp');

const ESQUEMA = JSON.parse(fs.readFileSync(path.resolve(__dirname, '..', 'schema.json'), 'utf8').replace(/^﻿/, ''));

// ───────────────────────────── ER ─────────────────────────────
function tipoSql(c) {
  const t = c.tipo;
  if (['nvarchar', 'varchar', 'nchar', 'char'].includes(t)) {
    const n = c.len === -1 ? 'max' : (t.startsWith('n') ? c.len / 2 : c.len);
    return `${t}(${n})`;
  }
  if (t === 'decimal' || t === 'numeric') return `decimal(${c.prec},${c.sc})`;
  return t;
}

function resolverER(m) {
  const set = new Set(m.tablas);
  const pks = {};
  for (const p of ESQUEMA.pks) (pks[p.tabla] = pks[p.tabla] || new Set()).add(p.col);
  const fkCols = {};
  for (const f of ESQUEMA.fks) (fkCols[f.hija] = fkCols[f.hija] || new Set()).add(f.colhija);
  const tablas = m.tablas.map(nombre => {
    const cols = ESQUEMA.cols.filter(c => c.tabla === nombre);
    if (!cols.length) throw new Error('Tabla inexistente en el esquema: ' + nombre);
    return {
      nombre,
      columnas: cols.map(c => ({
        nombre: c.col, tipo: tipoSql(c), nulo: !!c.nulo,
        pk: !!(pks[nombre] && pks[nombre].has(c.col)),
        fk: !!(fkCols[nombre] && fkCols[nombre].has(c.col))
      }))
    };
  });
  // Una relación por FK (columna única). Cardinalidad: padre 1 (o 0..1 si nulo) — hija 0..*.
  const relaciones = [];
  for (const f of ESQUEMA.fks) {
    if (!set.has(f.hija) || !set.has(f.padre)) continue;
    const col = ESQUEMA.cols.find(c => c.tabla === f.hija && c.col === f.colhija);
    relaciones.push({ padre: f.padre, hija: f.hija, columna: f.colhija, opcional: !!(col && col.nulo) });
  }
  return { ...m, tablas, relaciones };
}

// ───────────────────────────── Clases ─────────────────────────────
function tipoBase(t) {
  return t.replace(/[?\[\]]/g, '').replace(/^(?:List|IList|IEnumerable|ICollection|HashSet)<(.+)>$/, '$1').trim();
}
// "decimal bruto, int? idPlan, IEnumerable<Promocion> promos = null" -> "decimal, int?, IEnumerable<Promocion>"
function soloTipos(params) {
  const partes = []; let prof = 0, act = '';
  for (const ch of params) {
    if (ch === '<' || ch === '(') prof++; else if (ch === '>' || ch === ')') prof--;
    if (ch === ',' && prof === 0) { partes.push(act); act = ''; } else act += ch;
  }
  if (act.trim()) partes.push(act);
  return partes.map(p => p.replace(/=.*$/, '').trim().replace(/^(?:ref|out|params)\s+/, '').split(/\s+/).slice(0, -1).join(' ')).filter(Boolean).join(', ');
}
function esColeccion(t) { return /^(?:List|IList|IEnumerable|ICollection|HashSet)<|\[\]$/.test(t.trim()); }

function resolverClases(m) {
  const items = [];
  const idDe = (c) => (c.ns === 'BE' || c.ns.startsWith('BE.')) ? c.nombre : (c.ns + '_' + c.nombre).replace(/\./g, '_');
  for (const spec of m.clases) {
    const s = typeof spec === 'string' ? { ref: spec } : spec;
    const c = cs.buscar(s.ref);
    if (!c) throw new Error('Clase no encontrada en el código: ' + s.ref);
    let props = c.props.filter(p => !p.join);
    if (Array.isArray(s.attrs)) props = props.filter(p => s.attrs.includes(p.nombre));
    else if (s.attrs === 'none') props = [];
    else if (s.attrs === 'keys') props = props.filter(p => /^Id|^Estado$|^Nombre$|^Fecha/.test(p.nombre)).slice(0, 8);
    let metodos = c.metodos;
    if (Array.isArray(s.metodos)) metodos = metodos.filter(x => s.metodos.includes(x.nombre));
    else if (s.metodos !== 'all') metodos = [];
    metodos = metodos.map(x => ({ ...x, params: soloTipos(x.params), retorno: x.retorno.replace(/[()]/g, '').replace(/\s+/g, ' ') }));
    items.push({
      id: idDe(c), ref: s.ref, nombre: c.nombre, ns: c.ns, tipo: c.tipo, abstracta: c.abstracta, archivo: c.archivo,
      etiqueta: (c.ns === 'BE' || c.ns.startsWith('BE.')) ? c.nombre : c.ns + '.' + c.nombre,
      estereotipo: s.estereotipo || (c.tipo === 'interface' ? 'interface' : c.tipo === 'enum' ? 'enumeration' : (c.abstracta ? 'abstract' : (c.estatica ? 'static' : ''))),
      props, metodos, valores: c.valores, bases: c.bases, ctor: c.ctor
    });
  }
  const porNombre = {};
  for (const it of items) (porNombre[it.nombre] = porNombre[it.nombre] || []).push(it);
  const esBE = (it) => it.ns === 'BE' || it.ns.startsWith('BE.');
  const elegir = (nombre, prefiereBE) => {
    const l = porNombre[nombre]; if (!l) return null;
    return (prefiereBE ? l.find(esBE) : l.find(i => !esBE(i))) || l[0];
  };
  const rel = [], vistos = new Set();
  const add = (r) => { const k = [r.tipo, r.de, r.a, r.etiqueta || ''].join('|'); if (!vistos.has(k)) { vistos.add(k); rel.push(r); } };
  const alias = Object.assign({ Plan: 'PlanSuscripcion', Vendedor: 'Empleado', Caja: 'Empleado', Empleado: 'Empleado', ClienteReferente: 'Cliente',
    UltimoCliente: 'Cliente', ClienteActual: 'Cliente', SugerenciaOrigen: 'SugerenciaPromocion', PlanAnterior: 'PlanSuscripcion', PlanNuevo: 'PlanSuscripcion' }, m.alias || {});

  for (const it of items) {
    for (const b of it.bases) {
      const base = tipoBase(b);
      const d = elegir(base, false);
      if (d && d.id !== it.id) add({ tipo: d.tipo === 'interface' ? 'implementa' : 'hereda', de: it.id, a: d.id });
    }
    if (m.autoAsociaciones !== false && esBE(it)) {
      for (const p of it.props) {
        const t = tipoBase(p.tipo);
        let destino = elegir(t, true);
        let tipoRel = null;
        if (destino && destino.id !== it.id) {
          tipoRel = (destino.tipo === 'enum') ? 'usaEnum' : 'asocia';
        } else if (m.asocPorId !== false && /^Id[A-Z]/.test(p.nombre) && p.nombre !== 'Id' + it.nombre) {
          const sufijo = p.nombre.slice(2);
          const nombreDest = alias[sufijo] || sufijo;
          destino = elegir(nombreDest, true);
          if (destino && esBE(destino) && destino.id !== it.id) tipoRel = 'asocia'; else destino = null;
        }
        if (destino && tipoRel && !m.sinAsociacion?.includes(it.id + '.' + p.nombre)) {
          add({ tipo: tipoRel, de: it.id, a: destino.id, etiqueta: p.nombre, mult: esColeccion(p.tipo) ? '*' : (p.tipo.endsWith('?') ? '0..1' : '1') });
        }
      }
    }
    if (m.autoDependencias !== false && it.ctor.length) {
      const ultimo = it.ctor.reduce((a, b) => (b.length > a.length ? b : a), []);
      for (const d of ultimo) {
        const t = elegir(tipoBase(d), false);
        if (t && t.id !== it.id) add({ tipo: 'depende', de: it.id, a: t.id });
      }
    }
  }
  for (const r of (m.relaciones || [])) add(r);
  return { ...m, items, relaciones: rel };
}

module.exports = { resolverER, resolverClases, ESQUEMA };
