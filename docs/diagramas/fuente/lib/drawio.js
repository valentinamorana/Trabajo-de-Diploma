// Emisor draw.io (mxGraph XML). Recibe los MISMOS modelos resueltos que el emisor Mermaid.
const esc = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const html = (s) => esc(s); // el contenido HTML del label se escapa una vez para el atributo XML

class Lienzo {
  constructor(nombre) { this.nombre = nombre; this.celdas = []; this.n = 2; this.maxX = 0; this.maxY = 0; }
  nid() { return 'c' + (this.n++); }
  vertice(valor, estilo, x, y, w, h, parent = '1') {
    const id = this.nid();
    this.celdas.push(`<mxCell id="${id}" value="${valor}" style="${estilo}" vertex="1" parent="${parent}"><mxGeometry x="${Math.round(x)}" y="${Math.round(y)}" width="${Math.round(w)}" height="${Math.round(h)}" as="geometry"/></mxCell>`);
    if (parent === '1') { this.maxX = Math.max(this.maxX, x + w); this.maxY = Math.max(this.maxY, y + h); }
    return id;
  }
  borde(valor, estilo, origen, destino, puntos = []) {
    const id = this.nid();
    const pts = puntos.length ? `<Array as="points">${puntos.map(p => `<mxPoint x="${Math.round(p[0])}" y="${Math.round(p[1])}"/>`).join('')}</Array>` : '';
    this.celdas.push(`<mxCell id="${id}" value="${valor}" style="${estilo}" edge="1" parent="1" source="${origen}" target="${destino}"><mxGeometry relative="1" as="geometry">${pts}</mxGeometry></mxCell>`);
    return id;
  }
  bordeLibre(valor, estilo, p1, p2) {
    const id = this.nid();
    this.celdas.push(`<mxCell id="${id}" value="${valor}" style="${estilo}" edge="1" parent="1"><mxGeometry relative="1" as="geometry"><mxPoint x="${Math.round(p1[0])}" y="${Math.round(p1[1])}" as="sourcePoint"/><mxPoint x="${Math.round(p2[0])}" y="${Math.round(p2[1])}" as="targetPoint"/></mxGeometry></mxCell>`);
    this.maxX = Math.max(this.maxX, p1[0], p2[0]); this.maxY = Math.max(this.maxY, p1[1], p2[1]);
    return id;
  }
  xml() {
    const w = Math.max(1200, Math.round(this.maxX + 80)), h = Math.max(800, Math.round(this.maxY + 80));
    return `<?xml version="1.0" encoding="UTF-8"?>\n<mxfile host="app.diagrams.net" version="24.0.0"><diagram id="d1" name="${esc(this.nombre)}"><mxGraphModel dx="${w}" dy="${h}" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="${w}" pageHeight="${h}" math="0" shadow="0"><root><mxCell id="0"/><mxCell id="1" parent="0"/>\n${this.celdas.join('\n')}\n</root></mxGraphModel></diagram></mxfile>\n`;
  }
}

// ───────────────────────────── ER ─────────────────────────────
function er(m) {
  const L = new Lienzo(m.titulo);
  const cajas = {};
  const n = m.tablas.length;
  const cols = m.columnas || Math.max(2, Math.ceil(Math.sqrt(n * 1.4)));
  const ancho = (t) => m.sinColumnas ? Math.max(170, t.nombre.length * 11 + 30) : Math.max(210, Math.max(...t.columnas.map(c => (c.nombre.length + c.tipo.length + 8) * 7.2), t.nombre.length * 9) + 24);
  const alto = (t) => m.sinColumnas ? 46 : 30 + t.columnas.length * 17;
  // Filas: calcula alto de cada fila y posiciona
  const filas = [];
  m.tablas.forEach((t, i) => { const f = Math.floor(i / cols); (filas[f] = filas[f] || []).push(t); });
  let y = 30;
  for (const fila of filas) {
    let x = 30; const hMax = Math.max(...fila.map(alto));
    for (const t of fila) {
      const w = ancho(t), h = alto(t);
      const cols_ = m.sinColumnas ? [] : t.columnas;
      const filasHtml = cols_.map(c => {
        const marca = (c.pk ? 'PK ' : '') + (c.fk ? 'FK ' : '');
        const txt = `${marca}${c.nombre} : ${c.tipo}${c.nulo ? ' (null)' : ''}`;
        return `<div style="text-align:left;white-space:nowrap;${c.pk ? 'font-weight:bold;' : ''}">${html(txt)}</div>`;
      }).join('');
      const valor = `<div style="text-align:center;font-weight:bold;background:#EADCE6;padding:3px;border-bottom:1px solid #9a8a96;">${html(t.nombre)}</div><div style="padding:2px 6px;font-size:11px;">${filasHtml}</div>`;
      cajas[t.nombre] = L.vertice(valor, 'rounded=0;whiteSpace=wrap;html=1;align=left;verticalAlign=top;overflow=hidden;fillColor=#FFFFFF;strokeColor=#7a3d68;', x, y, w, h);
      x += w + 90;
    }
    y += hMax + 80;
  }
  for (const r of m.relaciones) {
    const estilo = `edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;fontSize=10;startArrow=ERzeroToMany;startFill=0;endArrow=${r.opcional ? 'ERzeroToOne' : 'ERmandOne'};endFill=0;strokeColor=#7a3d68;`;
    L.borde(html(r.columna), estilo, cajas[r.hija], cajas[r.padre]);
  }
  return L.xml();
}

// ───────────────────────────── Clases ─────────────────────────────
function clases(m) {
  const L = new Lienzo(m.titulo);
  const cajas = {};
  const n = m.items.length;
  const cols = m.columnas || Math.max(2, Math.ceil(Math.sqrt(n * 1.5)));
  const lineasDe = (c) => {
    const at = c.tipo === 'enum' ? c.valores : c.props.map(p => `+ ${p.nombre} : ${p.tipo}`);
    const me = c.metodos.map(x => `+ ${x.nombre}(${x.params.replace(/\b(?:ref|out)\s+/g, '')}) : ${x.retorno}`);
    return { at, me };
  };
  const dim = (c) => {
    const { at, me } = lineasDe(c);
    const todas = [c.etiqueta, ...at, ...me];
    const w = Math.max(190, Math.min(520, Math.max(...todas.map(s => s.length)) * 6.6 + 24));
    const h = 30 + (c.estereotipo ? 14 : 0) + Math.max(1, at.length) * 15 + (me.length ? me.length * 15 + 8 : 0) + 8;
    return { w, h, at, me };
  };
  const filas = [];
  m.items.forEach((c, i) => { const f = Math.floor(i / cols); (filas[f] = filas[f] || []).push(c); });
  let y = 30;
  for (const fila of filas) {
    let x = 30; const dims = fila.map(dim); const hMax = Math.max(...dims.map(d => d.h));
    fila.forEach((c, i) => {
      const d = dims[i];
      const et = c.estereotipo ? `<div style="text-align:center;font-size:10px;">&lt;&lt;${html(c.estereotipo)}&gt;&gt;</div>` : '';
      const cab = `<div style="text-align:center;font-weight:bold;${c.abstracta ? 'font-style:italic;' : ''}padding:3px 0;">${html(c.etiqueta)}</div>`;
      const at = `<div style="border-top:1px solid #7a3d68;padding:2px 6px;font-size:11px;text-align:left;">${d.at.map(s => `<div style="white-space:nowrap;">${html(s)}</div>`).join('') || '&nbsp;'}</div>`;
      const me = d.me.length ? `<div style="border-top:1px solid #7a3d68;padding:2px 6px;font-size:11px;text-align:left;">${d.me.map(s => `<div style="white-space:nowrap;">${html(s)}</div>`).join('')}</div>` : '';
      cajas[c.id] = L.vertice(et + cab + at + me, 'rounded=0;whiteSpace=wrap;html=1;align=left;verticalAlign=top;overflow=hidden;fillColor=#FFFFFF;strokeColor=#7a3d68;', x, y, d.w, d.h);
      x += d.w + 100;
    });
    y += hMax + 90;
  }
  for (const r of m.relaciones) {
    const o = cajas[r.de], d = cajas[r.a];
    if (!o || !d) continue;
    let est = 'edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;fontSize=10;strokeColor=#7a3d68;';
    let val = '';
    switch (r.tipo) {
      case 'hereda': est += 'endArrow=block;endFill=0;'; break;
      case 'implementa': est += 'endArrow=block;endFill=0;dashed=1;'; break;
      case 'depende': est += 'endArrow=open;endFill=0;dashed=1;'; val = 'usa'; break;
      case 'compone': est += 'endArrow=none;startArrow=diamondThin;startFill=1;'; val = r.etiqueta || ''; break;
      case 'usaEnum': est += 'endArrow=open;endFill=0;dashed=1;'; val = r.etiqueta || ''; break;
      default: est += 'endArrow=open;endFill=0;'; val = (r.etiqueta ? r.etiqueta + ' ' : '') + `[${r.mult || '1'}]`;
    }
    L.borde(html(val), est, o, d);
  }
  return L.xml();
}

// ───────────────────────────── Secuencia ─────────────────────────────
function secuencia(m) {
  const L = new Lienzo(m.titulo);
  const sep = Math.max(190, Math.max(...m.participantes.map(p => p.etiqueta.length)) * 8 + 40);
  const px = {}; m.participantes.forEach((p, i) => { px[p.id] = 60 + i * sep + sep / 2; });
  const topY = 20, boxH = 44, arranque = topY + boxH + 40, paso = 42;
  // primer pase: calcular y de cada paso y marcos
  let y = arranque, num = 0;
  const msgs = [], marcos = [], notas = [];
  const usados = (pasos, acc = new Set()) => { for (const s of pasos) { if (s.de) { acc.add(s.de); acc.add(s.a); } if (s.sobre) s.sobre.forEach(x => acc.add(x)); if (s.pasos) usados(s.pasos, acc); for (const e of (s.sino || [])) usados(e.pasos, acc); } return acc; };
  const recorrer = (pasos, prof) => {
    for (const s of pasos) {
      if (s.alt || s.opt || s.loop) {
        const tipo = s.alt ? 'alt' : s.opt ? 'opt' : 'loop';
        const cond = s.alt || s.opt || s.loop;
        const part = usados([s]); const y0 = y - 18;
        const seps = [];
        y += 8; recorrer(s.pasos, prof + 1);
        for (const e of (s.sino || [])) { y += 10; seps.push({ y: y - 14, etiqueta: e.etiqueta }); recorrer(e.pasos, prof + 1); }
        y += 12;
        marcos.push({ tipo, cond, y0, y1: y - 12, part, seps, prof });
      } else if (s.nota) {
        notas.push({ y: y - 14, texto: s.nota, sobre: s.sobre }); y += paso;
      } else {
        const esRet = !!s.ret; if (!esRet) num++;
        msgs.push({ y, de: s.de, a: s.a, texto: esRet ? s.ret : `${num}: ${s.msg}`, ret: esRet }); y += paso;
      }
    }
  };
  recorrer(m.pasos, 0);
  const finY = y + 10;
  // participantes y líneas de vida
  for (const p of m.participantes) {
    const x = px[p.id];
    if (p.actor) {
      L.vertice(html(p.etiqueta), 'shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;fontSize=11;', x - 15, topY - 4, 30, 50);
    } else {
      L.vertice(html(p.etiqueta), 'rounded=1;whiteSpace=wrap;html=1;fillColor=#EADCE6;strokeColor=#7a3d68;fontSize=11;', x - (sep - 40) / 2, topY, sep - 40, boxH);
    }
    L.bordeLibre('', 'endArrow=none;dashed=1;html=1;strokeColor=#9a8a96;', [x, topY + boxH + (p.actor ? 18 : 0)], [x, finY]);
  }
  // marcos (dibujados antes de los mensajes para quedar detrás)
  for (const f of marcos) {
    const xs = [...f.part].map(id => px[id]).filter(v => v !== undefined);
    const x0 = Math.min(...xs) - 60 + f.prof * 6, x1 = Math.max(...xs) + 60 - f.prof * 6;
    L.vertice(html(`${f.tipo} [${f.cond}]`), 'shape=umlFrame;whiteSpace=wrap;html=1;fillColor=none;strokeColor=#7a3d68;align=left;verticalAlign=top;fontSize=10;width=90;height=22;', x0, f.y0, x1 - x0, f.y1 - f.y0);
    for (const s of f.seps) L.bordeLibre(html(`[${s.etiqueta}]`), 'endArrow=none;dashed=1;html=1;fontSize=10;align=left;strokeColor=#7a3d68;', [x0, s.y], [x1, s.y]);
  }
  for (const n of notas) {
    const xs = n.sobre.map(id => px[id]);
    const x0 = Math.min(...xs) - 80, x1 = Math.max(...xs) + 80;
    L.vertice(html(n.texto), 'shape=note;whiteSpace=wrap;html=1;fillColor=#FFF2CC;strokeColor=#d6b656;fontSize=10;size=8;', x0, n.y, Math.max(160, x1 - x0), 34);
  }
  for (const s of msgs) {
    const a = px[s.de], b = px[s.a];
    const est = s.ret ? 'html=1;verticalAlign=bottom;endArrow=open;endFill=0;dashed=1;fontSize=10;strokeColor=#444;' : 'html=1;verticalAlign=bottom;endArrow=block;endFill=1;fontSize=10;strokeColor=#444;';
    L.bordeLibre(html(s.texto), est, [a, s.y], [b, s.y]);
  }
  return L.xml();
}

// ───────────────────────────── Actividad ─────────────────────────────
function actividad(m) {
  const L = new Lienzo(m.titulo);
  const idx = {}; m.nodos.forEach(n => { idx[n.id] = n; });
  const ady = {}; m.nodos.forEach(n => { ady[n.id] = []; });
  for (const f of m.flujos) ady[f.de].push(f.a);
  // rango por DFS ignorando aristas de retroceso
  const rango = {}, estado = {}, atras = new Set();
  const inicio = m.nodos.find(n => n.tipo === 'inicio') || m.nodos[0];
  (function dfs(u) { estado[u] = 1; for (const v of ady[u]) { if (estado[v] === 1) atras.add(u + '>' + v); else if (!estado[v]) dfs(v); } estado[u] = 2; })(inicio.id);
  m.nodos.forEach(n => { rango[n.id] = 0; });
  let cambio = true, it = 0;
  while (cambio && it++ < 200) { cambio = false; for (const f of m.flujos) { if (atras.has(f.de + '>' + f.a)) continue; if (rango[f.a] < rango[f.de] + 1) { rango[f.a] = rango[f.de] + 1; cambio = true; } } }
  const carrilIdx = {}; m.carriles.forEach((c, i) => { carrilIdx[c.id] = i; });
  const LANE_W = 260, GAP = 92, TOP = 60;
  const ocupado = {};
  const pos = {};
  const dimN = (n) => n.tipo === 'inicio' || n.tipo === 'fin' ? [26, 26] : n.tipo === 'decision' ? [190, 84] : [200, 52];
  for (const n of m.nodos) {
    const r = rango[n.id]; const k = n.carril + ':' + r;
    const off = ocupado[k] = (ocupado[k] || 0);
    ocupado[k]++;
    const [w, h] = dimN(n);
    const cx = carrilIdx[n.carril] * LANE_W + LANE_W / 2 + off * 30;
    pos[n.id] = { x: cx - w / 2, y: TOP + r * GAP + (off * 0), w, h };
  }
  const maxR = Math.max(...Object.values(rango));
  const alto = TOP + (maxR + 1) * GAP + 40;
  m.carriles.forEach((c, i) => {
    L.vertice(html(c.nombre), 'swimlane;html=1;horizontal=1;startSize=30;fillColor=#F7F1F5;strokeColor=#9a8a96;fontStyle=1;fontSize=12;', i * LANE_W, 0, LANE_W, alto);
  });
  const ids = {};
  for (const n of m.nodos) {
    const p = pos[n.id];
    let est;
    if (n.tipo === 'inicio') est = 'ellipse;html=1;fillColor=#222222;strokeColor=#222222;';
    else if (n.tipo === 'fin') est = 'ellipse;html=1;fillColor=#FFFFFF;strokeColor=#222222;strokeWidth=4;';
    else if (n.tipo === 'decision') est = 'rhombus;whiteSpace=wrap;html=1;fillColor=#FFF2CC;strokeColor=#d6b656;fontSize=10;';
    else est = 'rounded=1;whiteSpace=wrap;html=1;fillColor=#FFFFFF;strokeColor=#7a3d68;fontSize=10;arcSize=20;';
    ids[n.id] = L.vertice(n.tipo === 'inicio' || n.tipo === 'fin' ? '' : html(n.texto), est, p.x, p.y, p.w, p.h);
  }
  for (const f of m.flujos) {
    L.borde(html(f.texto || ''), 'edgeStyle=orthogonalEdgeStyle;rounded=1;html=1;endArrow=block;endFill=1;fontSize=10;strokeColor=#444;', ids[f.de], ids[f.a]);
  }
  return L.xml();
}

// ───────────────────────────── Casos de uso ─────────────────────────────
function casos(m) {
  const L = new Lienzo(m.titulo);
  const nC = m.casos.length, alturaCaso = 62, sepC = 26;
  const sisX = 250, sisW = 420, sisY = 30, sisH = nC * (alturaCaso + sepC) + 60;
  L.vertice(html(m.sistema), 'rounded=0;whiteSpace=wrap;html=1;fillColor=none;strokeColor=#7a3d68;verticalAlign=top;fontStyle=1;fontSize=12;', sisX, sisY, sisW, sisH);
  const cy = {}, cid = {};
  m.casos.forEach((c, i) => {
    const y = sisY + 40 + i * (alturaCaso + sepC);
    cy[c.id] = y + alturaCaso / 2;
    cid[c.id] = L.vertice(html(c.nombre), 'ellipse;whiteSpace=wrap;html=1;fillColor=#FFFFFF;strokeColor=#7a3d68;fontSize=11;', sisX + 40, y, sisW - 80, alturaCaso);
  });
  const aid = {};
  m.actores.forEach((a, i) => {
    const ligados = m.enlaces.filter(e => e.actor === a.id).map(e => cy[e.caso]);
    const yc = ligados.length ? ligados.reduce((s, v) => s + v, 0) / ligados.length : sisY + sisH / 2;
    const izq = i % 2 === 0;
    aid[a.id] = L.vertice(html(a.nombre), 'shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;fontSize=11;', izq ? 70 : sisX + sisW + 90, yc - 30, 34, 60);
  });
  for (const e of m.enlaces) L.borde('', 'html=1;endArrow=none;strokeColor=#444;', aid[e.actor], cid[e.caso]);
  for (const i of (m.incluye || [])) L.borde(html('«include»'), 'html=1;dashed=1;endArrow=open;endFill=0;fontSize=10;strokeColor=#444;edgeStyle=orthogonalEdgeStyle;', cid[i.de], cid[i.a]);
  for (const i of (m.extiende || [])) L.borde(html('«extend»'), 'html=1;dashed=1;endArrow=open;endFill=0;fontSize=10;strokeColor=#444;edgeStyle=orthogonalEdgeStyle;', cid[i.de], cid[i.a]);
  return L.xml();
}

module.exports = { er, clases, secuencia, actividad, casos };
