Ya tengo una clase QueryTranslationContext, la cual tiene 13 referencias, no puedo realizar un cambio sin alterar el codigo, revisa por favor, te muestro donde son las referencias.

using System.Collections.Generic;

namespace QueryBuilder.Models;

/// <summary>
/// Representa el contexto que contiene los elementos necesarios para construir una consulta SQL.
/// </summary>
public class QueryTranslationContext
{
    /// <summary>
    /// Nombre de la tabla sobre la que se ejecutará la consulta.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de columnas a seleccionar.
    /// </summary>
    public List<string> SelectColumns { get; set; } = [];

    /// <summary>
    /// Cláusula WHERE generada dinámicamente.
    /// </summary>
    public string? WhereClause { get; set; }

    /// <summary>
    /// Cláusula ORDER BY.
    /// </summary>
    public string? OrderByClause { get; set; }

    /// <summary>
    /// Número de filas a omitir (para paginación).
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// Número de filas a recuperar después del OFFSET.
    /// </summary>
    public int? Limit { get; set; }
}
