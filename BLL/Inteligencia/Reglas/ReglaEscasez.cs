using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia.Reglas
{
    /// <summary>
    /// Regla «Escasez con demanda» → reponer prendas (orden de compra sugerida en PDF).
    ///
    /// Por categoría y talle (prendas que no están de baja): hay escasez si quedan menos disponibles que
    /// EscasezMinimo Y hay demanda (clientes en lista de espera, o al menos DemandaEscasez pedidos en el
    /// período). Sin demanda, poco stock no es un problema: no se recomienda comprar.
    ///   cantidad a reponer = (mínimo − disponibles) + clientes en lista de espera
    ///   costo unitario     = precio de reposición promedio de la combinación (o del catálogo)
    ///   inversión          = Σ cantidad × costo unitario
    ///   ingreso protegido  = clientes en lista de espera × ticket promedio × HorizonteMeses
    ///                        (la prenda se compra una vez y se alquila durante meses: se compara la
    ///                        inversión con el ingreso del mismo horizonte que usan las demás reglas)
    ///   impacto            = ingreso protegido − inversión
    /// Si ninguna opción recupera la inversión en el horizonte, la recomendación baja a prioridad Baja
    /// y lo dice: reponer igual puede tener sentido (servicio), pero ya no es una decisión de rentabilidad.
    /// Alternativa: reponer solo lo que pide la lista de espera (menos inversión, menos cobertura).
    /// </summary>
    public class ReglaEscasez : ReglaBase
    {
        public const string Codigo = "Escasez";
        public override string Tipo => Codigo;

        private sealed class Faltante
        {
            public string Categoria, Talle;
            public int Disponibles, Espera, Pedidos, Cantidad;
            public decimal CostoUnitario;
        }

        public override IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx)
        {
            int minimo = (int)U(ctx, CatalogoUmbrales.EscasezMinimo);
            int demandaMinima = (int)U(ctx, CatalogoUmbrales.DemandaEscasez);
            decimal promedioCatalogo = PrecioReposicionPromedio(ctx);

            var faltantes = ctx.Prendas
                .Where(p => p.Estado != BE.EstadoPrenda.Baja)
                .GroupBy(p => new { Categoria = string.IsNullOrWhiteSpace(p.Categoria) ? "—" : p.Categoria.Trim(),
                                    Talle = string.IsNullOrWhiteSpace(p.Talle) ? "—" : p.Talle.Trim() })
                .Select(g =>
                {
                    var conPrecio = g.Where(p => p.PrecioReposicion.HasValue && p.PrecioReposicion.Value > 0).ToList();
                    return new Faltante
                    {
                        Categoria = g.Key.Categoria, Talle = g.Key.Talle,
                        Disponibles = g.Count(p => p.Estado == BE.EstadoPrenda.Disponible),
                        Espera = g.Sum(p => p.EnListaEspera),
                        Pedidos = g.Sum(p => p.PedidosPeriodo),
                        CostoUnitario = conPrecio.Count > 0 ? Math.Round(conPrecio.Average(p => p.PrecioReposicion.Value), 2) : promedioCatalogo
                    };
                })
                .Where(f => f.Disponibles < minimo && (f.Espera > 0 || f.Pedidos >= demandaMinima))
                .OrderByDescending(f => f.Espera).ThenBy(f => f.Disponibles)
                .ToList();
            if (faltantes.Count == 0) yield break;
            foreach (var f in faltantes) f.Cantidad = (minimo - f.Disponibles) + f.Espera;

            int espera = faltantes.Sum(f => f.Espera);
            decimal horizonte = U(ctx, CatalogoUmbrales.HorizonteMeses);
            decimal protegido = Math.Round(espera * ctx.TicketPromedio * horizonte, 2);

            var lineas = faltantes.Select(f => new BE.LineaOrdenCompra
            {
                Categoria = f.Categoria, Talle = f.Talle, Cantidad = f.Cantidad, CostoUnitario = f.CostoUnitario
            }).ToList();
            decimal inversion = lineas.Sum(l => l.Subtotal);
            int cantidad = lineas.Sum(l => l.Cantidad);

            var lineasEspera = faltantes.Where(f => f.Espera > 0).Select(f => new BE.LineaOrdenCompra
            {
                Categoria = f.Categoria, Talle = f.Talle, Cantidad = f.Espera, CostoUnitario = f.CostoUnitario
            }).ToList();
            decimal inversionEspera = lineasEspera.Sum(l => l.Subtotal);

            var rec = new BE.Recomendacion
            {
                TipoRegla = Codigo,
                Clave = Codigo,
                Prioridad = espera > 0 ? BE.PrioridadRecomendacion.Alta : BE.PrioridadRecomendacion.Media,
                Confianza = espera > 0 ? BE.NivelConfianza.Alta : BE.NivelConfianza.Media,
                Titulo = T("rec.escasez.titulo", "Reponer {0} prenda(s) en {1} combinación(es) de categoría y talle", cantidad, faltantes.Count),
                Diagnostico = T("rec.escasez.diag",
                    "{0} combinación(es) quedaron por debajo del mínimo de {1} prenda(s) disponibles y tienen demanda: {2} cliente(s) en lista de espera y {3} pedido(s) en el período.",
                    faltantes.Count, minimo, espera, faltantes.Sum(f => f.Pedidos)),
                Motivo = T("rec.escasez.motivo",
                    "Sin stock, la demanda se pierde: un cliente que espera una prenda que no llega es un candidato a darse de baja."),
                Detalle = faltantes.Select(f => $"{f.Categoria} / {f.Talle}: {f.Disponibles} disp., {f.Espera} en espera, {f.Pedidos} ped. → {f.Cantidad} × {f.CostoUnitario:C2}").ToList()
            };
            var opciones = new List<BE.OpcionRecomendacion>
            {
                Opcion(T("rec.escasez.opcion", "Generar la orden de compra sugerida por {0} prenda(s)", cantidad),
                    protegido - inversion,
                    T("rec.formula.escasez", "{0} clientes en espera × {1:C2} de ticket × {5:0} meses − {2} prendas × costo de reposición ({3:C2}) = {4:C2}",
                      espera, ctx.TicketPromedio, cantidad, inversion, protegido - inversion, horizonte),
                    BE.AccionRecomendacion.OrdenCompra, new BE.ParametrosAccion { Lineas = lineas })
            };
            if (lineasEspera.Count > 0 && lineasEspera.Sum(l => l.Cantidad) < cantidad)
                opciones.Add(Opcion(T("rec.escasez.opcion.espera", "Reponer solo lo que pide la lista de espera ({0} prenda(s))", lineasEspera.Sum(l => l.Cantidad)),
                    protegido - inversionEspera,
                    T("rec.formula.escasez", "{0} clientes en espera × {1:C2} de ticket × {5:0} meses − {2} prendas × costo de reposición ({3:C2}) = {4:C2}",
                      espera, ctx.TicketPromedio, lineasEspera.Sum(l => l.Cantidad), inversionEspera, protegido - inversionEspera, horizonte),
                    BE.AccionRecomendacion.OrdenCompra, new BE.ParametrosAccion { Lineas = lineasEspera }));
            // La orden completa es la propuesta mientras se recupere: cubre también la demanda por pedidos.
            // Si no se recupera en el horizonte y la reposición acotada a la lista de espera sí, se propone
            // esa (la completa queda como alternativa, con su número a la vista).
            if (opciones[0].ImpactoEstimado <= 0 && opciones.Skip(1).Any(o => o.ImpactoEstimado > 0))
                ElegirMejorOpcion(rec, opciones.ToArray());
            else
            {
                rec.Principal = opciones[0];
                rec.Alternativas = opciones.Skip(1).ToList();
            }
            // El título habla de la opción propuesta, no de la orden completa.
            var lineasPropuestas = rec.Principal.Parametros.Lineas;
            rec.Titulo = T("rec.escasez.titulo", "Reponer {0} prenda(s) en {1} combinación(es) de categoría y talle",
                lineasPropuestas.Sum(l => l.Cantidad), lineasPropuestas.Count);
            if (opciones.All(o => o.ImpactoEstimado <= 0))
            {
                rec.Prioridad = BE.PrioridadRecomendacion.Baja;
                rec.Motivo = T("rec.escasez.motivo.norentable",
                    "Sin stock, la demanda se pierde; pero con los supuestos actuales la compra no se recupera en el horizonte configurado. Reponer puede tener sentido para no perder clientes: decídalo mirando la lista de espera.");
            }
            yield return rec;
        }
    }
}
