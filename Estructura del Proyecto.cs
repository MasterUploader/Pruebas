namespace QueryBuilder.Enums
{
    /// <summary>
    /// Dialectos soportados para ajustar ciertos fragmentos de SQL (p. ej., paginación).
    /// </summary>
    public enum SqlDialect
    {
        Db2i,       // IBM i (AS/400) / DB2 for i
        SqlServer,
        PostgreSql,
        MySql,
        Oracle,
        Generic
    }
}


private readonly SqlDialect _dialect = SqlDialect.Db2i; // Db2i por defecto (AS/400)


/// <summary>
/// Inicializa una nueva instancia para una tabla concreta, indicando dialecto.
/// </summary>
public SelectQueryBuilder(string tableName, string? library, SqlDialect dialect)
{
    _tableName = tableName;
    _library = library;
    _dialect = dialect;
}

/// <summary>
/// Inicializa una nueva instancia con tabla derivada (subconsulta), indicando dialecto.
/// </summary>
public SelectQueryBuilder(Subquery derivedTable, SqlDialect dialect)
{
    _derivedTable = derivedTable;
    _dialect = dialect;
}

// (Opcionales, si quieres mantener los ctors previos delegando al nuevo)
public SelectQueryBuilder(string tableName, string? library = null)
    : this(tableName, library, SqlDialect.Db2i) { }

public SelectQueryBuilder(Subquery derivedTable)
    : this(derivedTable, SqlDialect.Db2i) { }



// ----- Paginación según dialecto -----
switch (_dialect)
{
    case SqlDialect.Db2i:
    case SqlDialect.Oracle:
    case SqlDialect.Generic:
        if (_offset.HasValue)
            sb.Append($" OFFSET {_offset.Value} ROWS");
        if (_fetch.HasValue)
            sb.Append($" FETCH NEXT {_fetch.Value} ROWS ONLY");
        else if (_limit.HasValue)
            sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");
        break;

    case SqlDialect.SqlServer:
        // En SQL Server OFFSET/FETCH requiere ORDER BY
        if (_offset.HasValue || _fetch.HasValue || _limit.HasValue)
        {
            if (_orderBy.Count == 0)
                sb.Append(" ORDER BY (SELECT 1)");
            var offset = _offset ?? 0;
            sb.Append($" OFFSET {offset} ROWS");
            if (_fetch.HasValue)
                sb.Append($" FETCH NEXT {_fetch.Value} ROWS ONLY");
            else if (_limit.HasValue)
                sb.Append($" FETCH NEXT {_limit.Value} ROWS ONLY");
        }
        break;

    case SqlDialect.PostgreSql:
    case SqlDialect.MySql:
        // Forma portable: LIMIT n OFFSET m
        if (_fetch.HasValue)
            sb.Append($" LIMIT {_fetch.Value}");
        else if (_limit.HasValue)
            sb.Append($" LIMIT {_limit.Value}");
        if (_offset.HasValue)
            sb.Append($" OFFSET {_offset.Value}");
        break;
}


using QueryBuilder.Builders;
using QueryBuilder.Enums;

namespace QueryBuilder.Core
{
    /// <summary>
    /// Punto de entrada principal para construir consultas SQL.
    /// </summary>
    public static class QueryBuilder
    {
        /// <summary>
        /// Inicia la construcción de una consulta SELECT (dialecto por defecto: DB2 for i).
        /// </summary>
        public static SelectQueryBuilder From(string tableName, string? library = null)
            => new SelectQueryBuilder(tableName, library, SqlDialect.Db2i);

        /// <summary>
        /// Inicia la construcción de una consulta SELECT especificando el dialecto.
        /// </summary>
        public static SelectQueryBuilder From(string tableName, string? library, SqlDialect dialect)
            => new SelectQueryBuilder(tableName, library, dialect);

        /// <summary>
        /// Inicia la construcción de una consulta SELECT desde una subconsulta (opcionalmente con dialecto).
        /// </summary>
        public static SelectQueryBuilder From(Subquery derivedTable, SqlDialect dialect = SqlDialect.Db2i)
            => new SelectQueryBuilder(derivedTable, dialect);
    }
}




