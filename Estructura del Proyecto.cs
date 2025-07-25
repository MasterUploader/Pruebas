Tengo este codigo que se encarga de actualizar un valor, esto ya funcionaba, pero actualice para que utilice RestUtilities.Connections y RestUtilities.QueryBuilder, y dejo de funcionar parcialmente.

public bool ActualizarAgencia(AgenciaModel agencia)
{
    try
    {
        _as400.Open();

        //Construimos el Query
        var query = new UpdateQueryBuilder("RSAGE01", "BCAH96DTA")
            .Set("NOMAGE", agencia.NomAge)
            .Set("ZONA", agencia.Zona)
            .Set("MARQUESINA", agencia.Marquesina)
            .Set("RSTBRANCH", agencia.RstBranch)
            .Set("NOMBD", agencia.NomBD)
            .Set("NOMSER", agencia.NomSer)
            .Set("IPSER", agencia.IpSer)
            .Where<RSAGE01>(x => x.CODCCO == agencia.Codcco)
            .Build();

        using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);
        command.CommandText = query.Sql;
        
        return command.ExecuteNonQuery() > 0;
    }
    finally
    {
        _as400.Close();
    }
}
Al Ejecutar el comando command.ExecuteNonQuery() > 0, valido y veo que la tabla recibe cambios, es decir el comando se ejecuto correctamente, y veo que si yo hago algo como int rows =  command.ExecuteNonQuery(), me dice que rows
    es igual a 1, lo cual esta bien, pero al realizar la validación que ya realizaba  me lanza la excepción System.NullReferenceException: 'Object reference not set to an instance of an object.'. 
Yo valido  y veo que _httpContextAccessor.HttpContext no esta null, en otro punto donde hago un select usando la libreria, la ejecución es correcta.

Acá coloco codigo adicional que les puede servir para que me ayudes a decifrar el error.

        using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Logging.Decorators;

/// <summary>
/// Decorador para interceptar y registrar automáticamente logs SQL al ejecutar comandos de base de datos.
/// Guarda el log acumulado al hacer <see cref="Dispose"/>.
/// </summary>
public class LoggingDbCommandWrapper : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly ILoggingService _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly Stopwatch _stopwatch = new();
    private readonly object _lock = new();

    private int _executionCount = 0;
    private int _totalAffectedRows = 0;
    private DateTime _startTime;
    private string _commandText = string.Empty;
    private bool _isFinalized = false;

    /// <summary>
    /// Inicializa una nueva instancia del decorador <see cref="LoggingDbCommandWrapper"/>.
    /// </summary>
    /// <param name="innerCommand">Comando original a decorar.</param>
    /// <param name="loggingService">Servicio de logging estructurado.</param>
    /// <param name="httpContextAccessor">Accessor para el contexto HTTP, útil para trazabilidad.</param>
    public LoggingDbCommandWrapper(DbCommand innerCommand, ILoggingService loggingService, IHttpContextAccessor? httpContextAccessor = null)
    {
        _innerCommand = innerCommand;
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Ejecuta el lector de datos con comportamiento específico y registra la ejecución.
    /// </summary>
    /// <param name="behavior">Comportamiento del lector (por ejemplo, CloseConnection).</param>
    /// <returns>Lector de datos de la consulta ejecutada.</returns>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        StartIfNeeded();

        try
        {
            var reader = _innerCommand.ExecuteReader(behavior);
            return reader;
        }
        catch (Exception ex)
        {
            _loggingService.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
            throw;
        }
    }

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        StartIfNeeded();
        var result = _innerCommand.ExecuteNonQuery();
        RegisterExecution(result);
        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        StartIfNeeded();
        var result = _innerCommand.ExecuteScalar();
        RegisterExecution(result != null ? 1 : 0);
        return result;
    }

    /// <summary>
    /// Guarda automáticamente el log al liberar el comando si hubo ejecuciones.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _innerCommand.Dispose();
        FinalizeAndLog();
    }

    /// <summary>
    /// Consolida y guarda el log estructurado si no ha sido registrado aún.
    /// </summary>
    private void FinalizeAndLog()
    {
        lock (_lock)
        {
            if (_isFinalized || _executionCount == 0)
                return;

            _stopwatch.Stop();
            _isFinalized = true;

            var log = new SqlLogModel
            {
                Sql = _commandText,
                ExecutionCount = _executionCount,
                TotalAffectedRows = _totalAffectedRows,
                StartTime = _startTime,
                Duration = _stopwatch.Elapsed,
                DatabaseName = _innerCommand.Connection?.Database ?? "Desconocida",
                Ip = _innerCommand.Connection?.DataSource ?? "Desconocida",
                Port = 0,
                TableName = ExtraerNombreTablaDesdeSql(_commandText),
                Schema = ExtraerEsquemaDesdeSql(_commandText)
            };

            _loggingService.LogDatabaseSuccess(log, _httpContextAccessor?.HttpContext);
        }
    }

    private void StartIfNeeded()
    {
        lock (_lock)
        {
            if (_executionCount == 0)
            {
                _startTime = DateTime.Now;
                _commandText = _innerCommand.CommandText;
                _stopwatch.Restart();
            }
        }
    }

    private void RegisterExecution(int affectedRows)
    {
        lock (_lock)
        {
            _executionCount++;
            if (affectedRows > 0)
                _totalAffectedRows += affectedRows;
        }
    }

    private static string ExtraerNombreTablaDesdeSql(string sql)
    {
        try
        {
            var tokens = sql.ToLower().Split(' ');
            var index = Array.FindIndex(tokens, t => t == "into" || t == "from" || t == "update");
            return index >= 0 && tokens.Length > index + 1 ? tokens[index + 1] : "Desconocida";
        }
        catch { return "Desconocida"; }
    }

    private static string ExtraerEsquemaDesdeSql(string sql)
    {
        var tabla = ExtraerNombreTablaDesdeSql(sql);
        var partes = tabla.Split('.');
        return partes.Length > 1 ? partes[0] : "Desconocida";
    }

    #region Delegación al comando interno

    public override string CommandText { get => _innerCommand.CommandText; set => _innerCommand.CommandText = value; }
    public override int CommandTimeout { get => _innerCommand.CommandTimeout; set => _innerCommand.CommandTimeout = value; }
    public override System.Data.CommandType CommandType { get => _innerCommand.CommandType; set => _innerCommand.CommandType = value; }
    public override bool DesignTimeVisible { get => _innerCommand.DesignTimeVisible; set => _innerCommand.DesignTimeVisible = value; }
    public override UpdateRowSource UpdatedRowSource { get => _innerCommand.UpdatedRowSource; set => _innerCommand.UpdatedRowSource = value; }
    protected override DbConnection DbConnection { get => _innerCommand.Connection!; set => _innerCommand.Connection = value; }
    protected override DbTransaction? DbTransaction { get => _innerCommand.Transaction; set => _innerCommand.Transaction = value; }
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;
    public override void Cancel() => _innerCommand.Cancel();
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();
    public override void Prepare() => _innerCommand.Prepare();

    #endregion
}

using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias UPDATE compatibles con AS400 y otros motores.
/// Permite actualizar registros con columnas específicas y condiciones.
/// </summary>
public class UpdateQueryBuilder(string _tableName, string? _library = null)
{
    private readonly Dictionary<string, object?> _setColumns = new();
    private string? _whereClause;
    private string? _comment;

    /// <summary>
    /// Agrega un comentario SQL al inicio del UPDATE para trazabilidad o debugging.
    /// </summary>
    /// <param name="comment">Texto del comentario.</param>
    /// <returns>Instancia modificada de <see cref="UpdateQueryBuilder"/>.</returns>
    public UpdateQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

    /// <summary>
    /// Define una columna y su nuevo valor a actualizar.
    /// </summary>
    /// <param name="column">Nombre de la columna.</param>
    /// <param name="value">Valor a establecer.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public UpdateQueryBuilder Set(string column, object? value)
    {
        _setColumns[column] = value;
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE para el UPDATE usando SQL crudo.
    /// </summary>
    /// <param name="sql">Condición SQL como cadena.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public UpdateQueryBuilder Where(string sql)
    {
        _whereClause = sql;
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE para el UPDATE utilizando expresiones lambda.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto sobre el cual se basa la expresión.</typeparam>
    /// <param name="expression">Expresión lambda que representa la condición WHERE.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public UpdateQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        _whereClause = ExpressionToSqlConverter.Convert(expression);
        return this;
    }

    /// <summary>
    /// Construye y retorna la consulta UPDATE generada.
    /// </summary>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para UPDATE.");

        if (_setColumns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para actualizar.");

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        var fullTable = string.IsNullOrWhiteSpace(_library) ? _tableName : $"{_library}.{_tableName}";
        sb.Append($"UPDATE {fullTable} SET ");

        var sets = _setColumns
            .Select(pair => $"{pair.Key} = {SqlHelper.FormatValue(pair.Value)}");

        sb.Append(string.Join(",", sets));

        if (!string.IsNullOrWhiteSpace(_whereClause))
        {
            sb.Append(" WHERE ");
            sb.Append(_whereClause);
        }

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}

using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using QueryBuilder.Translators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de consultas SELECT compatible con AS400.
/// Soporta DISTINCT, alias, JOINs, GROUP BY, HAVING, ORDER BY y funciones agregadas.
/// </summary>
public class SelectQueryBuilder
{
    internal string? WhereClause { get; set; }
    internal string? HavingClause { get; set; }

    private int? _offset;
    private int? _fetch;
    private readonly string? _tableName;
    private readonly string? _library;
    private string? _tableAlias;
    private readonly List<(string Column, string? Alias)> _columns = [];
    private readonly List<(string Column, SortDirection Direction)> _orderBy = [];
    private readonly List<string> _groupBy = [];
    private readonly List<JoinClause> _joins = [];
    private readonly List<CommonTableExpression> _ctes = [];

    private readonly Dictionary<string, string> _aliasMap = [];
    private int? _limit;
    private bool _distinct = false;
    private readonly Subquery? _derivedTable;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/> con una tabla derivada.
    /// </summary>
    /// <param name="derivedTable">Subconsulta que actúa como tabla.</param>
    public SelectQueryBuilder(Subquery derivedTable)
    {
        _derivedTable = derivedTable;
    }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/>.
    /// </summary>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }
    /// <summary>
    /// Agrega una condición WHERE del tipo "CAMPO IN (...)".
    /// </summary>
    /// <param name="column">Nombre de la columna a comparar.</param>
    /// <param name="values">Lista de valores a incluir.</param>
    public SelectQueryBuilder WhereIn(string column, IEnumerable<object> values)
    {
        if (values is null || !values.Any()) return this;

        string formatted = string.Join(", ", values.Select(SqlHelper.FormatValue));
        string clause = $"{column} IN ({formatted})";

        WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE del tipo "CAMPO NOT IN (...)".
    /// </summary>
    /// <param name="column">Nombre de la columna a comparar.</param>
    /// <param name="values">Lista de valores a excluir.</param>
    public SelectQueryBuilder WhereNotIn(string column, IEnumerable<object> values)
    {
        if (values is null || !values.Any()) return this;

        string formatted = string.Join(", ", values.Select(SqlHelper.FormatValue));
        string clause = $"{column} NOT IN ({formatted})";

        WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE del tipo "CAMPO BETWEEN VALOR1 AND VALOR2".
    /// </summary>
    /// <param name="column">Nombre de la columna a comparar.</param>
    /// <param name="start">Valor inicial del rango.</param>
    /// <param name="end">Valor final del rango.</param>
    public SelectQueryBuilder WhereBetween(string column, object start, object end)
    {
        string formattedStart = SqlHelper.FormatValue(start);
        string formattedEnd = SqlHelper.FormatValue(end);
        string clause = $"{column} BETWEEN {formattedStart} AND {formattedEnd}";

        WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
        return this;
    }


    /// <summary>
    /// Agrega un JOIN con una subconsulta como tabla.
    /// </summary>
    /// <param name="subquery">Instancia de subconsulta a usar como tabla.</param>
    /// <param name="alias">Alias de la tabla derivada.</param>
    /// <param name="left">Columna izquierda para la condición ON.</param>
    /// <param name="right">Columna derecha para la condición ON.</param>
    /// <param name="joinType">Tipo de JOIN: INNER, LEFT, etc.</param>
    public SelectQueryBuilder Join(Subquery subquery, string alias, string left, string right, string joinType = "INNER")
    {
        _joins.Add(new JoinClause
        {
            JoinType = joinType.ToUpperInvariant(),
            TableName = $"({subquery.Sql})",
            Alias = alias,
            LeftColumn = left,
            RightColumn = right
        });
        return this;
    }

    /// <summary>
    /// Agrega una o más expresiones CTE a la consulta.
    /// </summary>
    /// <param name="ctes">CTEs a incluir en la cláusula WITH.</param>
    /// <returns>Instancia actual de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder With(params CommonTableExpression[] ctes)
    {
        if (ctes != null && ctes.Length > 0)
            _ctes.AddRange(ctes);

        return this;
    }

    /// <summary>
    /// Indica que se desea una consulta SELECT DISTINCT.
    /// </summary>
    public SelectQueryBuilder Distinct()
    {
        _distinct = true;
        return this;
    }

    /// <summary>
    /// Define un alias para la tabla.
    /// </summary>
    public SelectQueryBuilder As(string alias)
    {
        _tableAlias = alias;
        return this;
    }

    /// <summary>
    /// Agrega una subconsulta como una columna seleccionada.
    /// </summary>
    /// <param name="subquery">Subconsulta construida previamente.</param>
    /// <param name="alias">Alias de la columna resultante.</param>
    public SelectQueryBuilder Select(Subquery subquery, string alias)
    {
        _columns.Add(($"({subquery.Sql})", alias));
        return this;
    }

    /// <summary>
    /// Agrega una expresión CASE WHEN como columna seleccionada, con un alias explícito.
    /// </summary>
    /// <param name="caseExpression">Expresión CASE generada con <see cref="CaseWhenBuilder"/>.</param>
    /// <param name="alias">Alias para la columna resultante.</param>
    public SelectQueryBuilder SelectCase(string caseExpression, string alias)
    {
        _columns.Add((caseExpression, alias));
        _aliasMap[caseExpression] = alias;
        return this;
    }

    /// <summary>
    /// Agrega una o varias expresiones CASE WHEN al SELECT.
    /// </summary>
    /// <param name="caseColumns">
    /// Tuplas donde el primer valor es la expresión CASE WHEN generada por <see cref="CaseWhenBuilder"/>,
    /// y el segundo valor es el alias de la columna.
    /// </param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder SelectCase(params (string ColumnSql, string? Alias)[] caseColumns)
    {
        foreach (var (column, alias) in caseColumns)
        {
            _columns.Add((column, alias));
            if (!string.IsNullOrWhiteSpace(alias))
                _aliasMap[column] = alias;
        }

        return this;
    }

    /// <summary>
    /// Define las columnas a seleccionar (sin alias explícito).
    /// Si detecta funciones agregadas, genera alias automáticos como "SUM_CAMPO".
    /// </summary>
    /// <param name="columns">Nombres de columnas o funciones agregadas.</param>
    public SelectQueryBuilder Select(params string[] columns)
    {
        foreach (var column in columns)
        {
            if (TryGenerateAlias(column, out var alias))
                _columns.Add((column, alias));
            else
                _columns.Add((column, null));
        }

        return this;
    }

    /// <summary>
    /// Define columnas con alias.
    /// </summary>
    public SelectQueryBuilder Select(params (string Column, string Alias)[] columns)
    {
        foreach (var (column, alias) in columns)
        {
            _columns.Add((column, alias));
            _aliasMap[column] = alias;
        }
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE con una función SQL directamente como string.
    /// Ejemplo: "UPPER(NOMBRE) = 'PEDRO'"
    /// </summary>
    /// <param name="sqlFunctionCondition">Condición completa en SQL.</param>
    public SelectQueryBuilder WhereFunction(string sqlFunctionCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
            return this;

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = sqlFunctionCondition;
        else
            WhereClause += $" AND {sqlFunctionCondition}";

        return this;
    }

    /// <summary>
    /// Agrega una condición HAVING con una función SQL directamente como string.
    /// Ejemplo: "SUM(CANTIDAD) > 10"
    /// </summary>
    /// <param name="sqlFunctionCondition">Condición completa en SQL.</param>
    public SelectQueryBuilder HavingFunction(string sqlFunctionCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
            return this;

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = sqlFunctionCondition;
        else
            HavingClause += $" AND {sqlFunctionCondition}";

        return this;
    }


    /// <summary>
    /// Agrega una cláusula WHERE con una expresión CASE WHEN directamente como string.
    /// Ej: "CASE WHEN TIPO = 'A' THEN 1 ELSE 0 END = 1"
    /// </summary>
    /// <param name="sqlCaseCondition">Condición CASE completa.</param>
    public SelectQueryBuilder WhereCase(string sqlCaseCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlCaseCondition))
            return this;

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = sqlCaseCondition;
        else
            WhereClause += $" AND {sqlCaseCondition}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE con una expresión CASE WHEN generada desde <see cref="CaseWhenBuilder"/>.
    /// </summary>
    /// <param name="caseBuilder">Instancia del builder de CASE.</param>
    /// <param name="comparison">Condición adicional (por ejemplo: "= 1").</param>
    public SelectQueryBuilder WhereCase(CaseWhenBuilder caseBuilder, string comparison)
    {
        if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison))
            return this;

        string expression = $"{caseBuilder.Build()} {comparison}";

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = expression;
        else
            WhereClause += $" AND {expression}";

        return this;
    }


    /// <summary>
    /// Agrega una condición WHERE.
    /// </summary>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        LambdaWhereTranslator.Translate(this, expression);
        return this;
    }

    /// <summary>
    /// Agrega una cláusula HAVING EXISTS con una subconsulta.
    /// </summary>
    /// <param name="subquery">Subconsulta que se evaluará con EXISTS.</param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder HavingExists(Subquery subquery)
    {
        if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql))
            return this;

        var clause = $"EXISTS ({subquery.Sql})";

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = clause;
        else
            HavingClause += $" AND {clause}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula HAVING NOT EXISTS con una subconsulta.
    /// </summary>
    /// <param name="subquery">Subconsulta que se evaluará con NOT EXISTS.</param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder HavingNotExists(Subquery subquery)
    {
        if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql))
            return this;

        var clause = $"NOT EXISTS ({subquery.Sql})";

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = clause;
        else
            HavingClause += $" AND {clause}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula HAVING con una expresión CASE WHEN directamente como string.
    /// Ejemplo: "CASE WHEN SUM(CANTIDAD) > 10 THEN 1 ELSE 0 END = 1"
    /// </summary>
    /// <param name="sqlCaseCondition">Expresión CASE completa en SQL.</param>
    public SelectQueryBuilder HavingCase(string sqlCaseCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlCaseCondition))
            return this;

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = sqlCaseCondition;
        else
            HavingClause += $" AND {sqlCaseCondition}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula HAVING con una expresión CASE WHEN generada con <see cref="CaseWhenBuilder"/>.
    /// </summary>
    /// <param name="caseBuilder">Builder de CASE WHEN.</param>
    /// <param name="comparison">Comparación adicional (ej: "= 1").</param>
    public SelectQueryBuilder HavingCase(CaseWhenBuilder caseBuilder, string comparison)
    {
        if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison))
            return this;

        string expression = $"{caseBuilder.Build()} {comparison}";

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = expression;
        else
            HavingClause += $" AND {expression}";

        return this;
    }

    /// <summary>
    /// Agrega una condición HAVING.
    /// </summary>
    public SelectQueryBuilder Having<T>(Expression<Func<T, bool>> expression)
    {
        LambdaHavingTranslator.Translate(this, expression);
        return this;
    }

    /// <summary>
    /// Agrega una condición EXISTS a la cláusula WHERE con una subconsulta generada dinámicamente.
    /// </summary>
    /// <param name="subqueryBuilderAction">
    /// Acción que configura un nuevo <see cref="SelectQueryBuilder"/> para representar la subconsulta dentro de EXISTS.
    /// </param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder WhereExists(Action<SelectQueryBuilder> subqueryBuilderAction)
    {
        var subqueryBuilder = new SelectQueryBuilder("DUMMY"); // El nombre se sobreescribirá
        subqueryBuilderAction(subqueryBuilder);
        var subquerySql = subqueryBuilder.Build().Sql;

        var existsClause = $"EXISTS ({subquerySql})";

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = existsClause;
        else
            WhereClause += $" AND {existsClause}";

        return this;
    }

    /// <summary>
    /// Agrega una condición NOT EXISTS a la cláusula WHERE con una subconsulta generada dinámicamente.
    /// </summary>
    /// <param name="subqueryBuilderAction">
    /// Acción que configura un nuevo <see cref="SelectQueryBuilder"/> para representar la subconsulta dentro de NOT EXISTS.
    /// </param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder WhereNotExists(Action<SelectQueryBuilder> subqueryBuilderAction)
    {
        var subqueryBuilder = new SelectQueryBuilder("DUMMY");
        subqueryBuilderAction(subqueryBuilder);
        var subquerySql = subqueryBuilder.Build().Sql;

        var notExistsClause = $"NOT EXISTS ({subquerySql})";

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = notExistsClause;
        else
            WhereClause += $" AND {notExistsClause}";

        return this;
    }

    /// <summary>
    /// Establece las columnas para agrupar (GROUP BY).
    /// </summary>
    public SelectQueryBuilder GroupBy(params string[] columns)
    {
        _groupBy.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Establece el límite de resultados (FETCH FIRST para AS400).
    /// </summary>
    public SelectQueryBuilder Limit(int rowCount)
    {
        _limit = rowCount;
        return this;
    }

    /// <summary>
    /// Ordena por una o varias columnas.
    /// </summary>
    public SelectQueryBuilder OrderBy(params (string Column, SortDirection Direction)[] columns)
    {
        _orderBy.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega un JOIN a la consulta.
    /// </summary>
    public SelectQueryBuilder Join(string table, string? library, string alias, string left, string right, string joinType = "INNER")
    {
        _joins.Add(new JoinClause
        {
            JoinType = joinType.ToUpper(),
            TableName = table,
            Library = library,
            Alias = alias,
            LeftColumn = left,
            RightColumn = right
        });
        return this;
    }

    /// <summary>
    /// Define el valor de desplazamiento de filas (OFFSET).
    /// </summary>
    /// <param name="offset">Cantidad de filas a omitir.</param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder Offset(int offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    /// Define la cantidad de filas a recuperar después del OFFSET (FETCH NEXT).
    /// </summary>
    /// <param name="rowCount">Cantidad de filas a recuperar.</param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder FetchNext(int rowCount)
    {
        _fetch = rowCount;
        return this;
    }


    /// <summary>
    /// Construye y retorna el SQL.
    /// </summary>
    public QueryResult Build()
    {
        var sb = new StringBuilder();

        // Si hay CTEs, agregarlas antes del SELECT
        if (_ctes.Count > 0)
        {
            sb.Append("WITH ");
            sb.Append(string.Join(", ", _ctes.Select(cte => cte.ToString())));
            sb.AppendLine();
        }

        sb.Append("SELECT ");
        if (_distinct) sb.Append("DISTINCT ");

        if (_columns.Count == 0)
            sb.Append('*');
        else
        {
            var colParts = _columns.Select(c =>
                string.IsNullOrWhiteSpace(c.Alias)
                    ? c.Column
                    : $"{c.Column} AS {c.Alias}"
            );
            sb.Append(string.Join(", ", colParts));
        }

        sb.Append(" FROM ");
        if (_derivedTable != null)
        {
            sb.Append(_derivedTable.ToString());
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(_library))
                sb.Append($"{_library}.");
            sb.Append(_tableName);
            if (!string.IsNullOrWhiteSpace(_tableAlias))
                sb.Append($" {_tableAlias}");
        }

        if (!string.IsNullOrWhiteSpace(_tableAlias))
            sb.Append($" {_tableAlias}");

        foreach (var join in _joins)
        {
            sb.Append($" {join.JoinType} JOIN ");
            if (!string.IsNullOrWhiteSpace(join.Library))
                sb.Append($"{join.Library}.");
            sb.Append($"{join.TableName} {join.Alias} ON {join.LeftColumn} = {join.RightColumn}");
        }

        if (!string.IsNullOrWhiteSpace(WhereClause))
            sb.Append($" WHERE {WhereClause}");

        if (_groupBy.Count > 0)
        {
            sb.Append(" GROUP BY ");
            var grouped = _groupBy.Select(col => _aliasMap.TryGetValue(col, out var alias) ? alias : col);
            sb.Append(string.Join(", ", grouped));
        }

        if (!string.IsNullOrWhiteSpace(HavingClause))
            sb.Append($" HAVING {HavingClause}");

        if (_orderBy.Count > 0)
        {
            sb.Append(" ORDER BY ");
            var ordered = _orderBy.Select(o =>
            {
                var col = _aliasMap.TryGetValue(o.Column, out var alias) ? alias : o.Column;
                return $"{col} {o.Direction.ToString().ToUpper()}";
            });
            sb.Append(string.Join(", ", ordered));
        }

        if (_limit.HasValue)
            sb.Append(GetLimitClause());

        return new QueryResult { Sql = sb.ToString() };
    }

    /// <summary>
    /// Genera un alias automático para funciones agregadas como SUM(CAMPO), COUNT(*), etc.
    /// </summary>
    /// <param name="column">Expresión de columna a analizar.</param>
    /// <param name="alias">Alias generado si aplica.</param>
    /// <returns>True si se generó un alias; false en caso contrario.</returns>
    private static bool TryGenerateAlias(string column, out string alias)
    {
        alias = string.Empty;

        if (string.IsNullOrWhiteSpace(column) || !column.Contains('(') || !column.Contains(')'))
            return false;

        int start = column.IndexOf('(');
        int end = column.IndexOf(')');

        if (start < 1 || end <= start)
            return false;

        var function = column[..start].Trim().ToUpperInvariant();
        var argument = column.Substring(start + 1, end - start - 1).Trim();

        var validFunctions = new[] { "SUM", "COUNT", "AVG", "MIN", "MAX" };
        if (!validFunctions.Contains(function))
            return false;

        if (string.IsNullOrWhiteSpace(argument))
            return false;

        alias = $"{function}_{argument.Replace("*", "ALL")}";
        return true;
    }

    private string GetLimitClause()
    {
        if (_offset.HasValue && _fetch.HasValue)
            return $" OFFSET {_offset.Value} ROWS FETCH NEXT {_fetch.Value} ROWS ONLY";
        if (_fetch.HasValue)
            return $" FETCH FIRST {_fetch.Value} ROWS ONLY";
        return string.Empty;
    }
}

using QueryBuilder.Builders;

namespace QueryBuilder.Core;

/// <summary>
/// Punto de entrada principal para construir consultas SQL.
/// </summary>
public static class QueryBuilder
{
    /// <summary>
    /// Inicia la construcción de una consulta SELECT.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre opcional de la biblioteca (solo para AS400).</param>
    /// <returns>Instancia de SelectQueryBuilder.</returns>
    public static SelectQueryBuilder From(string tableName, string? library = null)
    {
        return new SelectQueryBuilder(tableName, library);
    }
}

Me indicas si deseas ver algun otro codigo en particular.
