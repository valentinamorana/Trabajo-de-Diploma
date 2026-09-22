// Emisor Mermaid: convierte los modelos resueltos en texto .mmd
const q = (s) => String(s).replace(/"/g, "'").replace(/[\r\n]+/g, ' ');
// En secuencia el ';' y '#' rompen el parser.
const seq = (s) => String(s).replace(/;/g, ',').replace(/#/g, 'nro.').replace(/[\r\n]+/g, ' ');
// Parte etiquetas largas sin espacios en el límite de una mayúscula (RenovacionSuscripcionForm -> Renovacion / SuscripcionForm)
const partir = (t) => {
  t = String(t);
  if (t.length <= 15 || /\s/.test(t)) return t;
  let mejor = -1, dist = 1e9;
  for (let i = 4; i < t.length - 3; i++) if (/[A-Z]/.test(t[i]) && /[a-z.]/.test(t[i - 1]) && Math.abs(i - t.length / 2) < dist) { mejor = i; dist = Math.abs(i - t.length / 2); }
  return mejor < 0 ? t : t.slice(0, mejor) + '<br/>' + t.slice(mejor);
};
const gen = (t) => t.replace(/</g, '~').replace(/>/g, '~');

// ───────────────────────────── ER ─────────────────────────────
function er(m) {
  const L = ['erDiagram'];
  for (const r of m.relaciones) {
    // padre ||--o{ hija  (opcional: |o--o{)
    L.push(`  ${r.padre} ${r.opcional ? '|o' : '||'}--o{ ${r.hija} : "${r.columna}"`);
  }
  for (const t of m.tablas) {
    if (m.sinColumnas) { L.push(`  ${t.nombre}`); continue; }
    L.push(`  ${t.nombre} {`);
    for (const c of t.columnas) {
      const claves = [c.pk ? 'PK' : null, c.fk ? 'FK' : null].filter(Boolean).join(',');
      L.push(`    ${c.tipo.replace(/,/g, '_')} ${c.nombre}${claves ? ' ' + claves : ''}${c.nulo ? ' "NULL"' : ''}`);
    }
    L.push('  }');
  }
  return L.join('\n') + '\n';
}

// ───────────────────────────── Clases ─────────────────────────────
function tipoMmd(t) { return gen(t); }
function clases(m) {
  const L = ['classDiagram', '  direction TB'];
  for (const c of m.items) {
    L.push(c.etiqueta && c.etiqueta !== c.id ? `  class ${c.id}["${c.etiqueta}"] {` : `  class ${c.id} {`);
    if (c.estereotipo) L.push(`    <<${c.estereotipo}>>`);
    if (c.tipo === 'enum') for (const v of c.valores) L.push(`    ${v}`);
    for (const p of c.props) L.push(`    +${tipoMmd(p.tipo)} ${p.nombre}`);
    for (const x of c.metodos) L.push(`    +${x.nombre}(${gen(x.params).replace(/,/g, ', ')}) ${tipoMmd(x.retorno)}`);
    L.push('  }');
  }
  for (const r of m.relaciones) {
    switch (r.tipo) {
      case 'hereda': L.push(`  ${r.a} <|-- ${r.de}`); break;
      case 'implementa': L.push(`  ${r.a} <|.. ${r.de}`); break;
      case 'depende': L.push(`  ${r.de} ..> ${r.a} : usa`); break;
      case 'compone': L.push(`  ${r.de} *-- ${r.a}${r.etiqueta ? ' : ' + q(r.etiqueta) : ''}`); break;
      case 'usaEnum': L.push(`  ${r.de} ..> ${r.a} : ${q(r.etiqueta)}`); break;
      default: L.push(`  ${r.de} --> "${r.mult || '1'}" ${r.a}${r.etiqueta ? ' : ' + q(r.etiqueta) : ''}`);
    }
  }
  return L.join('\n') + '\n';
}

// ───────────────────────────── Secuencia ─────────────────────────────
function secuencia(m) {
  const L = ['sequenceDiagram', '  autonumber'];
  for (const p of m.participantes) L.push(`  ${p.actor ? 'actor' : 'participant'} ${p.id} as ${partir(seq(p.etiqueta))}`);
  const emitir = (pasos, sang) => {
    const pad = '  '.repeat(sang);
    for (const s of pasos) {
      if (s.alt) {
        L.push(`${pad}alt ${seq(s.alt)}`); emitir(s.pasos, sang + 1);
        for (const e of (s.sino || [])) { L.push(`${pad}else ${seq(e.etiqueta)}`); emitir(e.pasos, sang + 1); }
        L.push(`${pad}end`);
      } else if (s.opt) {
        L.push(`${pad}opt ${seq(s.opt)}`); emitir(s.pasos, sang + 1); L.push(`${pad}end`);
      } else if (s.loop) {
        L.push(`${pad}loop ${seq(s.loop)}`); emitir(s.pasos, sang + 1); L.push(`${pad}end`);
      } else if (s.nota) {
        L.push(`${pad}Note over ${s.sobre.join(',')}: ${seq(s.nota)}`);
      } else if (s.ret) {
        L.push(`${pad}${s.de}-->>${s.a}: ${seq(s.ret)}`);
      } else {
        L.push(`${pad}${s.de}->>${s.a}: ${seq(s.msg)}`);
      }
    }
  };
  emitir(m.pasos, 1);
  return L.join('\n') + '\n';
}

// ───────────────────────────── Actividad ─────────────────────────────
function actividad(m) {
  const L = ['flowchart TD'];
  const forma = (n) => {
    const t = q(n.texto || '');
    switch (n.tipo) {
      case 'inicio': return `${n.id}((" "))`;
      case 'fin': return `${n.id}(((" ")))`;
      case 'decision': return `${n.id}{"${t}"}`;
      default: return `${n.id}["${t}"]`;
    }
  };
  for (const c of m.carriles) {
    L.push(`  subgraph ${c.id}["${q(c.nombre)}"]`);
    L.push('    direction TB');
    for (const n of m.nodos.filter(n => n.carril === c.id)) L.push('    ' + forma(n));
    L.push('  end');
  }
  for (const f of m.flujos) L.push(`  ${f.de} ${f.texto ? `-- "${q(f.texto)}" -->` : '-->'} ${f.a}`);
  L.push('  classDef ini fill:#222,stroke:#222,color:#222');
  const ini = m.nodos.filter(n => n.tipo === 'inicio' || n.tipo === 'fin').map(n => n.id);
  if (ini.length) L.push(`  class ${ini.join(',')} ini`);
  return L.join('\n') + '\n';
}

// ───────────────────────────── Casos de uso ─────────────────────────────
function casos(m) {
  const L = ['flowchart LR'];
  for (const a of m.actores) L.push(`  ${a.id}["${q(a.nombre)}"]:::actor`);
  L.push(`  subgraph SIS["${q(m.sistema)}"]`);
  for (const c of m.casos) L.push(`    ${c.id}(["${q(c.nombre)}"])`);
  L.push('  end');
  for (const e of m.enlaces) L.push(`  ${e.actor} --- ${e.caso}`);
  for (const i of (m.incluye || [])) L.push(`  ${i.de} -. "«include»" .-> ${i.a}`);
  for (const i of (m.extiende || [])) L.push(`  ${i.de} -. "«extend»" .-> ${i.a}`);
  L.push('  classDef actor fill:#fff7e6,stroke:#b8860b');
  return L.join('\n') + '\n';
}

module.exports = { er, clases, secuencia, actividad, casos };
