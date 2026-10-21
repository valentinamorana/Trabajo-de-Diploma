using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>
    /// PN03 — Contrato del acceso a datos de MetaComercial (metas mensuales de Gerencia; permite dobles
    /// de prueba). La unicidad "una meta por indicador y mes" la valida la BLL y la respalda la restricción
    /// UQ_MetaComercial_PeriodoIndicador de la base.
    /// </summary>
    public interface IMetaComercialDAL
    {
        /// <summary>Metas con Periodo entre los dos meses indicados (primer día de cada mes, inclusivos).</summary>
        List<BE.MetaComercial> Obtener(DateTime desdePeriodo, DateTime hastaPeriodo);

        /// <summary>Inserta la meta y devuelve el ID generado.</summary>
        int Alta(BE.MetaComercial meta);

        /// <summary>Cambia el objetivo (y quién y cuándo); false si la meta ya no existe.</summary>
        bool Modificar(BE.MetaComercial meta);

        /// <summary>Elimina la meta; false si ya no existía.</summary>
        bool Eliminar(int idMeta);
    }
}
