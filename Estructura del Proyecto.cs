using QueryBuilder.Enums;
using QueryBuilder.Interfaces;
using QueryBuilder.Models;
using System.Collections.Generic;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor de consultas SQL del tipo SELECT.
/// Permite construir dinámicamente sentencias SELECT con soporte para filtros, ordenamientos, paginación, joins, etc.
/// </summary>
public class SelectQueryBuilder : IQueryBuilder
{
    private string _table = string.Empty;
    private readonly List<string> _columns = new();
    private readonly List<string> _whereConditions = new();
    private readonly List<string> _orderBy = new();
    private int? _offset;
    private int? _fetch;

    /// <summary>
    /// Establece la tabla desde la cual se seleccionarán los datos.
    /// </summary>
    public IQueryBuilder From(string tableName)
    {
        _table = tableName;
        return this;
    }

    /// <summary>
    /// Especifica las columnas que serán seleccionadas en la consulta.
    /// </summary>
    public IQueryBuilder Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una condición a la cláusula WHERE.
    /// </summary>
    public IQueryBuilder Where(string condition)
    {
        _whereConditions.Add(condition);
        return this;
    }

    /// <summary>
    /// Agrega una cláusula ORDER BY para una columna específica.
    /// </summary>
    public IQueryBuilder OrderBy(string column, SqlSortDirection direction)
    {
        _orderBy.Add($"{column} {(direction == SqlSortDirection.Ascending ? "ASC" : "DESC")}");
        return this;
    }

    /// <summary>
    /// Establece el número de registros a omitir (OFFSET).
    /// </summary>
    public IQueryBuilder Offset(int offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    /// Establece el número de registros a obtener después del OFFSET.
    /// </summary>
    public IQueryBuilder FetchNext(int size)
    {
        _fetch = size;
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
            Operation = QueryOperation.Select
        };
    }
}

using QueryBuilder.Models;
using QueryBuilder.Enums;
using System.Collections.Generic;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor de consultas SQL del tipo INSERT.
/// Permite construir dinámicamente sentencias INSERT INTO con columnas y valores parametrizados.
/// </summary>
public class InsertQueryBuilder
{
    /// <summary>Nombre de la tabla destino.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Lista de columnas a insertar.</summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>Lista de valores asociados a las columnas.</summary>
    public List<string> Values { get; set; } = new();

    /// <summary>
    /// Construye la consulta SQL INSERT basada en los valores proporcionados.
    /// </summary>
    /// <returns>Consulta SQL generada.</returns>
    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO {Table} ({string.Join(", ", Columns)}) ");
        sb.Append($"VALUES ({string.Join(", ", Values)})");
        return sb.ToString();
    }

    /// <summary>
    /// Genera una instancia de QueryTranslationContext con los datos del INSERT configurado.
    /// </summary>
    /// <returns>Contexto de traducción para consulta INSERT.</returns>
    public QueryTranslationContext BuildContext()
    {
        var insertValues = new Dictionary<string, object>();
        for (int i = 0; i < Columns.Count; i++)
        {
            var value = Values[i];
            insertValues[Columns[i]] = value;
        }

        return new QueryTranslationContext
        {
            TableName = Table,
            InsertValues = insertValues,
            Operation = QueryOperation.Insert
        };
    }
}



using QueryBuilder.Models;
using QueryBuilder.Enums;
using System.Collections.Generic;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor de consultas SQL del tipo DELETE.
/// Permite construir sentencias DELETE con condiciones WHERE.
/// </summary>
public class DeleteQueryBuilder
{
    /// <summary>Nombre de la tabla desde la que se eliminarán los registros.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Condiciones WHERE que limitan la eliminación.</summary>
    public List<string> WhereConditions { get; set; } = new();

    /// <summary>
    /// Construye la sentencia SQL DELETE.
    /// </summary>
    /// <returns>Consulta SQL generada.</returns>
    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append($"DELETE FROM {Table}");

        if (WhereConditions.Count > 0)
            sb.Append(" WHERE ").Append(string.Join(" AND ", WhereConditions));

        return sb.ToString();
    }

    /// <summary>
    /// Genera una instancia de QueryTranslationContext con los datos del DELETE configurado.
    /// </summary>
    /// <returns>Contexto de traducción para consulta DELETE.</returns>
    public QueryTranslationContext BuildContext()
    {
        return new QueryTranslationContext
        {
            TableName = Table,
            WhereClause = WhereConditions.Count > 0 ? string.Join(" AND ", WhereConditions) : null,
            Operation = QueryOperation.Delete
        };
    }
}
using QueryBuilder.Enums;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor para sentencias JOIN SQL.
/// Permite agregar JOINs con sus respectivos tipos y condiciones ON.
/// </summary>
public class JoinBuilder
{
    /// <summary>Tabla secundaria que se desea unir.</summary>
    public string JoinTable { get; set; } = string.Empty;

    /// <summary>Condición que relaciona ambas tablas.</summary>
    public string JoinCondition { get; set; } = string.Empty;

    /// <summary>Tipo de JOIN (INNER, LEFT, RIGHT, FULL, SELF).</summary>
    public SqlJoinType JoinType { get; set; } = SqlJoinType.Inner;

    /// <summary>
    /// Construye el fragmento de SQL JOIN correspondiente.
    /// </summary>
    /// <returns>Fragmento SQL del JOIN.</returns>
    public string Build()
    {
        var joinTypeStr = JoinType switch
        {
            SqlJoinType.Left => "LEFT JOIN",
            SqlJoinType.Right => "RIGHT JOIN",
            SqlJoinType.Full => "FULL JOIN",
            SqlJoinType.Self => "JOIN", // SELF JOIN requiere alias externos
            _ => "INNER JOIN"
        };

        return $"{joinTypeStr} {JoinTable} ON {JoinCondition}";
    }
}

using QueryBuilder.Enums;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor para sentencias JOIN SQL.
/// Permite agregar JOINs con sus respectivos tipos y condiciones ON.
/// </summary>
public class JoinBuilder
{
    /// <summary>Tabla secundaria que se desea unir.</summary>
    public string JoinTable { get; set; } = string.Empty;

    /// <summary>Condición que relaciona ambas tablas.</summary>
    public string JoinCondition { get; set; } = string.Empty;

    /// <summary>Tipo de JOIN (INNER, LEFT, RIGHT, FULL, SELF).</summary>
    public SqlJoinType JoinType { get; set; } = SqlJoinType.Inner;

    /// <summary>
    /// Construye el fragmento de SQL JOIN correspondiente.
    /// </summary>
    /// <returns>Fragmento SQL del JOIN.</returns>
    public string Build()
    {
        var joinTypeStr = JoinType switch
        {
            SqlJoinType.Left => "LEFT JOIN",
            SqlJoinType.Right => "RIGHT JOIN",
            SqlJoinType.Full => "FULL JOIN",
            SqlJoinType.Self => "JOIN", // SELF JOIN requiere alias externos
            _ => "INNER JOIN"
        };

        return $"{joinTypeStr} {JoinTable} ON {JoinCondition}";
    }
}
