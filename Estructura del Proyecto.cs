Ahora me da este error Error al guardar: 'IBMDASQL.DataSource.1' failed with no error message available, result code: DB_E_NOCOMMAND(0x80040E0C).

    Así tengo el codigo actualmente

       public bool InsertarAgencia(AgenciaModel agencia)
   {
       try
       {
           _as400.Open();

       // Generar la sentencia SQL con QueryBuilder
       var query = new InsertQueryBuilder("RSAGE01", "BCAH96DTA")
           .Values(
               ("CODCCO", agencia.Codcco),
               ("NOMAGE", agencia.NomAge),
               ("ZONA", agencia.Zona),
               ("MARQUESINA", agencia.Marquesina),
               ("RSTBRANCH", agencia.RstBranch),
               ("NOMBD", agencia.NomBD),
               ("NOMSER", agencia.NomSer),
               ("IPSER", agencia.IpSer)
           )
           .Build();

       // Crear y ejecutar el comando
       using var command = _as400.GetDbCommand(null);

       return command.ExecuteNonQuery() > 0;
       }
       finally
       {
           _as400.Close();
       }
   }


using Connections.Interfaces;
using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System.Data.Common;
using System.Data.OleDb;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión a base de datos AS400 utilizando OleDb.
/// Esta implementación permite la ejecución de comandos SQL con o sin logging estructurado.
/// </summary>
public partial class AS400ConnectionProvider : IDatabaseConnection, IDisposable
{
    private readonly OleDbConnection _oleDbConnection;
    private readonly ILoggingService? _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AS400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400 en formato OleDb.</param>
    /// <param name="loggingService">Servicio de logging estructurado (opcional).</param>
    /// <param name="httpContextAccessor">Accessor del contexto HTTP (opcional).</param>
    public AS400ConnectionProvider(
        string connectionString,
        ILoggingService? loggingService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _oleDbConnection = new OleDbConnection(connectionString);
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public void Open()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <inheritdoc />
    public void Close()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Closed)
            _oleDbConnection.Close();
    }

    /// <inheritdoc />
    public bool IsConnected => _oleDbConnection?.State == System.Data.ConnectionState.Open;

    /// <inheritdoc />
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _oleDbConnection.CreateCommand();

        // Si el servicio de logging está disponible, devolvemos el comando decorado
        if (_loggingService != null)
        {
            return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);
        }

        // En caso contrario, devolvemos el comando básico
        return command;
    }

    /// <summary>
    /// Crea un comando configurado con la consulta SQL generada por QueryBuilder y sus parámetros asociados.
    /// </summary>
    /// <param name="queryResult">Objeto que contiene el SQL generado y la lista de parámetros.</param>
    /// <param name="context">Contexto HTTP actual para trazabilidad opcional.</param>
    /// <returns>DbCommand listo para ejecución.</returns>
    public DbCommand GetDbCommand(QueryResult queryResult, HttpContext? context)
    {
        var command = GetDbCommand(context);

        // Establece el SQL
        command.CommandText = queryResult.Sql;

        // Limpia cualquier parámetro anterior
        command.Parameters.Clear();

        // Agrega los parámetros a la posición correspondiente
        if (queryResult.Parameters is not null && queryResult.Parameters.Count > 0)
        {
            foreach (var paramValue in queryResult.Parameters)
            {
                var parameter = command.CreateParameter();
                parameter.Value = paramValue ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        return command;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _oleDbConnection?.Dispose();
    }
}

using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias INSERT compatibles con AS400 y otros motores.
/// Utiliza parámetros seguros y garantiza la asignación correcta a los comandos.
/// </summary>
public class InsertQueryBuilder(string _tableName, string? _library = null)
{
    private readonly List<string> _columns = [];
    private readonly List<List<object?>> _rows = [];
    private readonly List<List<object>> _valuesRaw = [];
    private SelectQueryBuilder? _selectSource;
    private string? _comment;
    private string? _whereClause;
    private bool _insertIgnore = false;
    private readonly Dictionary<string, object?> _onDuplicateUpdate = [];

    /// <summary>
    /// Agrega un comentario SQL al inicio del INSERT para trazabilidad o debugging.
    /// </summary>
    public InsertQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

    /// <summary>
    /// Indica que se debe usar INSERT IGNORE para omitir errores de duplicado.
    /// </summary>
    public InsertQueryBuilder InsertIgnore()
    {
        _insertIgnore = true;
        return this;
    }

    /// <summary>
    /// Define columnas que deben actualizarse si hay conflicto de clave duplicada.
    /// </summary>
    public InsertQueryBuilder OnDuplicateKeyUpdate(string column, object? value)
    {
        _onDuplicateUpdate[column] = value;
        return this;
    }

    /// <summary>
    /// Define múltiples columnas para actualizar en caso de duplicado.
    /// </summary>
    public InsertQueryBuilder OnDuplicateKeyUpdate(Dictionary<string, object?> updates)
    {
        foreach (var kvp in updates)
            _onDuplicateUpdate[kvp.Key] = kvp.Value;
        return this;
    }

    /// <summary>
    /// Define las columnas y valores a insertar. El orden será tomado desde el primer uso.
    /// </summary>
    public InsertQueryBuilder Values(params (string Column, object? Value)[] values)
    {
        if (_columns.Count == 0)
            _columns.AddRange(values.Select(v => v.Column));
        else if (_columns.Count != values.Length)
            throw new InvalidOperationException("El número de columnas no coincide con los valores proporcionados.");

        _rows.Add(values.Select(v => v.Value).ToList());
        return this;
    }

    /// <summary>
    /// Agrega valores sin parámetros (SQL raw).
    /// </summary>
    public InsertQueryBuilder ValuesRaw(params string[] rawValues)
    {
        _valuesRaw.Add(rawValues.Cast<object>().ToList());
        return this;
    }

    /// <summary>
    /// Define un SELECT como origen del INSERT.
    /// </summary>
    public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
    {
        _selectSource = select;
        _valuesRaw.Clear();
        return this;
    }

    /// <summary>
    /// Condición NOT EXISTS para INSERT ... SELECT.
    /// </summary>
    public InsertQueryBuilder WhereNotExists(Subquery subquery)
    {
        _whereClause = $"NOT EXISTS ({subquery.Sql})";
        return this;
    }

    /// <summary>
    /// Genera el SQL y los parámetros asociados.
    /// </summary>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificar un nombre de tabla.");

        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar columnas para el INSERT.");

        if (_selectSource != null && _rows.Count > 0)
            throw new InvalidOperationException("No puede combinar VALUES con FROM SELECT.");

        var sb = new StringBuilder();
        var parameters = new List<object?>();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        sb.Append("INSERT ");
        if (_insertIgnore) sb.Append("IGNORE ");
        sb.Append("INTO ");
        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");
        sb.Append(_tableName);
        sb.Append(" (").Append(string.Join(", ", _columns)).Append(")");

        if (_selectSource != null)
        {
            sb.AppendLine().Append(_selectSource.Build().Sql);
            if (!string.IsNullOrWhiteSpace(_whereClause))
                sb.Append(" WHERE ").Append(_whereClause);
        }
        else
        {
            sb.Append(" VALUES ");
            var valueLines = new List<string>();

            foreach (var row in _rows)
            {
                var placeholders = new List<string>();
                foreach (var val in row)
                {
                    placeholders.Add("?");
                    parameters.Add(val);
                }
                valueLines.Add($"({string.Join(", ", placeholders)})");
            }

            foreach (var row in _valuesRaw)
            {
                valueLines.Add($"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");
            }

            sb.Append(string.Join(", ", valueLines));
        }

        if (_onDuplicateUpdate.Count > 0)
        {
            sb.Append(" ON DUPLICATE KEY UPDATE ");
            sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
                $"{kv.Key} = {SqlHelper.FormatValue(kv.Value)}")));
        }

        return new QueryResult
        {
            Sql = sb.ToString(),
            Parameters = parameters
        };
    }
}
