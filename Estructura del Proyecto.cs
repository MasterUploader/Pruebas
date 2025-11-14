/// <summary>
/// Registra un log de SQL con error y lo encola con el INICIO real para mantener el orden cronológico.
/// Completa información de base de datos y biblioteca a partir de la cadena de conexión, del DbConnection
/// y del propio comando SQL.
/// </summary>
public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
{
    try
    {
        var conn = command.Connection;
        var info = LogHelper.ExtractDbConnectionInfo(conn?.ConnectionString);

        // 1) Tabla/esquema a partir del SQL: puede venir "bcah96dta.iposre01g1" o solo "iposre01g1".
        var rawTable = LogHelper.ExtractTableName(command.CommandText);

        string schema = info.Library;   // Biblioteca desde connection string (si existe).
        string tableName = rawTable;

        if (!string.IsNullOrWhiteSpace(rawTable))
        {
            var parts = rawTable.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                // Si no hay biblioteca en el connection string, la inferimos del SQL.
                if (string.IsNullOrWhiteSpace(schema))
                {
                    schema = parts[0];          // bcah96dta
                }

                tableName = parts[^1];          // iposre01g1
            }
        }

        if (string.IsNullOrWhiteSpace(schema))
        {
            schema = "Desconocida";
        }

        // 2) Base de datos:
        //    - Primero lo que venga del connection string.
        //    - Si viene vacío, usar conn.Database (que en tu log es DVHNDEV).
        //    - Si aún está vacío, usar el propio esquema como "database lógica".
        var databaseName = !string.IsNullOrWhiteSpace(info.Database)
            ? info.Database
            : (!string.IsNullOrWhiteSpace(conn?.Database)
                ? conn!.Database
                : schema);

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            databaseName = "Desconocida";
        }

        // 3) IP: si no está en info.Ip, usamos DataSource como fallback.
        var ip = !string.IsNullOrWhiteSpace(info.Ip)
            ? info.Ip
            : (conn?.DataSource ?? "Desconocida");

        // 4) Construimos el bloque de error usando los valores reforzados.
        var formatted = LogFormatter.FormatDbExecutionError(
            nombreBD: databaseName,
            ip: ip,
            puerto: info.Port,
            biblioteca: schema,
            tabla: tableName,
            sentenciaSQL: command.CommandText,
            exception: ex,
            horaError: DateTime.Now
        );

        if (context is not null)
        {
            var startedUtc = context.Items.TryGetValue("__SqlStartedUtc", out var o) && o is DateTime dt
                ? dt
                : DateTime.UtcNow;

            if (!context.Items.ContainsKey("HttpClientLogsTimed"))
            {
                context.Items["HttpClientLogsTimed"] = new List<object>();
            }

            if (context.Items["HttpClientLogsTimed"] is List<object> timed)
            {
                timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
            }
        }
        else
        {
            WriteLog(context, formatted);
        }

        AddExceptionLog(ex);
    }
    catch (Exception fail)
    {
        LogInternalError(fail);
    }
}
