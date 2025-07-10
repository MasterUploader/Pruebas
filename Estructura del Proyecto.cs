using QueryBuilder.Enums;
using QueryBuilder.Interfaces;
using QueryBuilder.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor de consultas SQL del tipo SELECT.
/// Permite construir dinámicamente sentencias SELECT con soporte para filtros, ordenamientos, paginación y joins.
/// </summary>
public class SelectQueryBuilder : IQueryBuilder
{
    private string _table = string.Empty;
    private readonly List<string> _columns = new();
    private readonly List<string> _whereConditions = new();
    private readonly List<string> _orderBy = new();
    private readonly List<JoinBuilder> _joins = new();
    private int? _offset;
    private int? _fetch;

    /// <summary>
    /// Establece la tabla desde la cual se seleccionarán los datos.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla base.</param>
    public IQueryBuilder From(string tableName)
    {
        _table = tableName;
        return this;
    }

    /// <summary>
    /// Especifica las columnas que serán seleccionadas en la consulta.
    /// </summary>
    /// <param name="columns">Lista de nombres de columnas.</param>
    public IQueryBuilder Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una condición a la cláusula WHERE.
    /// </summary>
    /// <param name="condition">Condición en formato SQL.</param>
    public IQueryBuilder Where(string condition)
    {
        _whereConditions.Add(condition);
        return this;
    }

    /// <summary>
    /// Agrega una cláusula ORDER BY para una columna específica.
    /// </summary>
    /// <param name="column">Nombre de la columna.</param>
    /// <param name="direction">Dirección de ordenamiento (ASC o DESC).</param>
    public IQueryBuilder OrderBy(string column, SqlSortDirection direction)
    {
        _orderBy.Add($"{column} {(direction == SqlSortDirection.Ascending ? "ASC" : "DESC")}");
        return this;
    }

    /// <summary>
    /// Establece el número de registros a omitir (OFFSET).
    /// </summary>
    /// <param name="offset">Cantidad de registros a omitir.</param>
    public IQueryBuilder Offset(int offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    /// Establece el número de registros a obtener después del OFFSET.
    /// </summary>
    /// <param name="size">Cantidad de registros a obtener.</param>
    public IQueryBuilder FetchNext(int size)
    {
        _fetch = size;
        return this;
    }

    /// <summary>
    /// Agrega una cláusula JOIN al SELECT.
    /// </summary>
    /// <param name="join">Objeto JoinBuilder que contiene tipo y condición del JOIN.</param>
    public IQueryBuilder Join(JoinBuilder join)
    {
        _joins.Add(join);
        return this;
    }

    /// <summary>
    /// Construye y retorna un QueryTranslationContext con los datos actuales del builder.
    /// </summary>
    public QueryTranslationContext BuildContext()
    {
        return new QueryTranslationContext
        {
            TableName = _table,
            SelectColumns = _columns,
            WhereClause = _whereConditions.Count > 0 ? string.Join(" AND ", _whereConditions) : null,
            OrderByClause = _orderBy.Count > 0 ? string.Join(", ", _orderBy) : null,
            Offset = _offset,
            Limit = _fetch,
            Operation = QueryOperation.Select,
            JoinClause = _joins.Count > 0 ? string.Join(" ", _joins.Select(j => j.Build())) : null
        };
    }
}
