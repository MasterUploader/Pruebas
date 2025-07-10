using System.Collections.Generic;

namespace QueryBuilder.Models;

/// <summary>
/// Extiende el contexto de traducción para admitir operaciones INSERT, UPDATE y METADATA.
/// </summary>
public class ExtendedQueryTranslationContext : QueryTranslationContext
{
    /// <summary>
    /// Columnas a insertar (INSERT INTO ...).
    /// </summary>
    public List<string> InsertColumns { get; set; } = [];

    /// <summary>
    /// Valores de los parámetros para el INSERT.
    /// </summary>
    public List<string> InsertValues { get; set; } = [];

    /// <summary>
    /// Columnas que serán actualizadas (UPDATE ... SET ...).
    /// </summary>
    public List<string> UpdateColumns { get; set; } = [];

    /// <summary>
    /// Valores de los parámetros para el UPDATE.
    /// </summary>
    public List<string> UpdateValues { get; set; } = [];

    /// <summary>
    /// Indica si se desea obtener únicamente metadata de la tabla.
    /// </summary>
    public bool MetadataOnly { get; set; }
}


using QueryBuilder.Attributes;
using System;
using System.Linq;
using System.Reflection;

namespace QueryBuilder.Utils;

/// <summary>
/// Ayudante para obtener metadatos desde atributos en clases de modelo.
/// </summary>
public static class SqlMetadataHelper
{
    /// <summary>
    /// Obtiene el nombre completo de la tabla a partir del atributo SqlTableAttribute.
    /// </summary>
    public static string GetFullTableName<T>()
    {
        var attr = typeof(T).GetCustomAttribute<SqlTableAttribute>();

        if (attr == null)
            throw new InvalidOperationException($"El modelo {typeof(T).Name} no contiene el atributo [SqlTable].");

        return !string.IsNullOrWhiteSpace(attr.Schema)
            ? $"{attr.Schema}.{attr.TableName}"
            : attr.TableName;
    }

    /// <summary>
    /// Obtiene las propiedades decoradas con [SqlColumnDefinition] del modelo.
    /// </summary>
    public static PropertyInfo[] GetColumnProperties<T>()
    {
        return typeof(T)
            .GetProperties()
            .Where(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>() != null)
            .ToArray();
    }
}


using QueryBuilder.Interfaces;
using QueryBuilder.Models;
using QueryBuilder.Utils;
using System.Linq;
using System.Text;

namespace QueryBuilder.Translators;

/// <summary>
/// Traductor de consultas específico para AS400 (DB2).
/// Soporta SELECT, INSERT y UPDATE utilizando el contexto de traducción.
/// </summary>
public class As400QueryTranslator : IQueryTranslator
{
    /// <inheritdoc />
    public string Translate(QueryTranslationContext context)
    {
        return context.Operation switch
        {
            QueryOperation.Select => BuildSelect(context),
            QueryOperation.Insert => BuildInsert(context),
            QueryOperation.Update => BuildUpdate(context),
            _ => throw new NotSupportedException($"Operación SQL no soportada: {context.Operation}")
        };
    }

    private string BuildSelect(QueryTranslationContext context)
    {
        var sb = new StringBuilder();
        sb.Append("SELECT ");
        sb.Append(string.Join(", ", context.SelectColumns));
        sb.Append(" FROM ");
        sb.Append(context.TableName);

        if (!string.IsNullOrWhiteSpace(context.WhereClause))
        {
            sb.Append(" WHERE ");
            sb.Append(context.WhereClause);
        }

        if (!string.IsNullOrWhiteSpace(context.OrderByClause))
        {
            sb.Append(" ORDER BY ");
            sb.Append(context.OrderByClause);
        }

        if (context.Offset.HasValue && context.Limit.HasValue)
        {
            sb.Append($" OFFSET {context.Offset.Value} ROWS FETCH NEXT {context.Limit.Value} ROWS ONLY");
        }

        return sb.ToString();
    }

    private string BuildInsert(QueryTranslationContext context)
    {
        var columns = context.InsertValues!.Keys.ToList();
        var columnList = string.Join(", ", columns);
        var paramList = string.Join(", ", columns.Select(_ => "?"));

        return $"INSERT INTO {context.TableName} ({columnList}) VALUES ({paramList})";
    }

    private string BuildUpdate(QueryTranslationContext context)
    {
        var columns = context.UpdateValues!.Keys.ToList();
        var setList = string.Join(", ", columns.Select(c => $"{c} = ?"));

        var sb = new StringBuilder();
        sb.Append($"UPDATE {context.TableName} SET ");
        sb.Append(setList);

        if (!string.IsNullOrWhiteSpace(context.WhereClause))
        {
            sb.Append(" WHERE ");
            sb.Append(context.WhereClause);
        }

        return sb.ToString();
    }
}
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

    /// <summary>
    /// Indica el tipo de operación SQL (SELECT, INSERT, UPDATE).
    /// </summary>
    public QueryOperation Operation { get; set; } = QueryOperation.Select;

    /// <summary>
    /// Diccionario de valores utilizados en una sentencia INSERT.
    /// </summary>
    public Dictionary<string, object>? InsertValues { get; set; }

    /// <summary>
    /// Diccionario de valores utilizados en una sentencia UPDATE.
    /// </summary>
    public Dictionary<string, object>? UpdateValues { get; set; }
}


namespace QueryBuilder.Models;

/// <summary>
/// Enum que define el tipo de operación SQL a ejecutar.
/// </summary>
public enum QueryOperation
{
    Select,
    Insert,
    Update
}

using QueryBuilder.Interfaces;
using QueryBuilder.Metadata;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QueryBuilder.Engines;

/// <summary>
/// Motor SQL especializado para generar consultas compatibles con AS400.
/// </summary>
public class As400SqlEngine : ISqlEngine
{
    private readonly IQueryTranslator _translator;

    /// <summary>
    /// Inicializa una nueva instancia del motor con el traductor AS400 especificado.
    /// </summary>
    /// <param name="translator">Traductor que convierte el contexto en una sentencia SQL específica.</param>
    public As400SqlEngine(IQueryTranslator translator)
    {
        _translator = translator;
    }

    /// <inheritdoc />
    public string GenerateSelectQuery<TModel>(Expression<Func<TModel, bool>>? filter = null)
    {
        var tableName = SqlMetadataHelper.GetFullTableName<TModel>();
        var columnNames = SqlMetadataHelper.GetSqlColumns<TModel>();

        var context = new QueryTranslationContext
        {
            TableName = tableName,
            SelectColumns = columnNames,
            Operation = QueryOperation.Select,
            WhereClause = filter != null ? SqlExpressionParser.Parse(filter) : null
        };

        return _translator.Translate(context);
    }

    /// <inheritdoc />
    public string GenerateInsertQuery<TModel>(TModel insertValues)
    {
        var tableName = SqlMetadataHelper.GetFullTableName<TModel>();
        var columnValues = SqlMetadataHelper.GetColumnValuePairs(insertValues);

        var context = new QueryTranslationContext
        {
            TableName = tableName,
            InsertValues = columnValues,
            Operation = QueryOperation.Insert
        };

        return _translator.Translate(context);
    }

    /// <inheritdoc />
    public string GenerateUpdateQuery<TModel>(TModel updateValues, Expression<Func<TModel, bool>> filter)
    {
        var tableName = SqlMetadataHelper.GetFullTableName<TModel>();
        var columnValues = SqlMetadataHelper.GetColumnValuePairs(updateValues);
        var whereClause = SqlExpressionParser.Parse(filter);

        var context = new QueryTranslationContext
        {
            TableName = tableName,
            UpdateValues = columnValues,
            WhereClause = whereClause,
            Operation = QueryOperation.Update
        };

        return _translator.Translate(context);
    }
}

using QueryBuilder.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QueryBuilder.Metadata;

/// <summary>
/// Utilidad para extraer metadatos de modelos decorados con atributos personalizados.
/// </summary>
public static class SqlMetadataHelper
{
    /// <summary>
    /// Obtiene el nombre completo de la tabla (esquema.tabla) a partir del modelo.
    /// </summary>
    /// <typeparam name="T">Tipo del modelo.</typeparam>
    /// <returns>Nombre completo de la tabla.</returns>
    public static string GetFullTableName<T>()
    {
        var type = typeof(T);
        var tableAttr = type.GetCustomAttribute<SqlTableAttribute>();
        if (tableAttr == null)
            throw new InvalidOperationException($"El modelo '{type.Name}' no tiene el atributo SqlTableAttribute.");

        return $"{tableAttr.Schema}.{tableAttr.TableName}";
    }

    /// <summary>
    /// Obtiene la lista de nombres de columnas SQL del modelo decoradas con SqlColumnDefinitionAttribute.
    /// </summary>
    /// <typeparam name="T">Tipo del modelo.</typeparam>
    /// <returns>Lista de nombres de columnas.</returns>
    public static List<string> GetSqlColumns<T>()
    {
        return typeof(T)
            .GetProperties()
            .Where(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>() != null)
            .Select(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>()!.ColumnName)
            .ToList();
    }

    /// <summary>
    /// Obtiene los pares columna-valor desde una instancia de modelo con atributos SqlColumnDefinitionAttribute.
    /// </summary>
    /// <param name="model">Instancia del modelo.</param>
    /// <returns>Diccionario de columnas con sus respectivos valores.</returns>
    public static Dictionary<string, object?> GetColumnValuePairs(object model)
    {
        var type = model.GetType();
        var props = type.GetProperties()
            .Where(p => p.GetCustomAttribute<SqlColumnDefinitionAttribute>() != null);

        var result = new Dictionary<string, object?>();

        foreach (var prop in props)
        {
            var columnAttr = prop.GetCustomAttribute<SqlColumnDefinitionAttribute>()!;
            var value = prop.GetValue(model);
            result[columnAttr.ColumnName] = value;
        }

        return result;
    }
}

using System;

namespace QueryBuilder.Attributes;

/// <summary>
/// Atributo para definir el nombre, tipo y longitud de una columna SQL asociada a una propiedad de un modelo.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SqlColumnDefinitionAttribute : Attribute
{
    /// <summary>
    /// Nombre de la columna en la base de datos.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Tipo de dato SQL.
    /// </summary>
    public SqlDataType DataType { get; }

    /// <summary>
    /// Longitud máxima del campo (opcional, útil para cadenas).
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Inicializa una nueva instancia del atributo.
    /// </summary>
    /// <param name="columnName">Nombre de la columna en la base de datos.</param>
    /// <param name="dataType">Tipo de dato SQL.</param>
    /// <param name="length">Longitud del campo.</param>
    public SqlColumnDefinitionAttribute(string columnName, SqlDataType dataType, int length)
    {
        ColumnName = columnName;
        DataType = dataType;
        Length = length;
    }
}
namespace QueryBuilder.Attributes;

/// <summary>
/// Representa los tipos de datos SQL utilizados en atributos de metadatos.
/// </summary>
public enum SqlDataType
{
    Char,
    Varchar,
    Int,
    Decimal,
    Date,
    Time,
    Timestamp
}

