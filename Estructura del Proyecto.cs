/// <summary>
/// Registra un log de SQL con error y lo encola con el INICIO real para mantener el orden cronológico.
/// Completa información de base de datos y biblioteca a partir de la cadena de conexión y del propio comando.
/// </summary>
public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
{
    try
    {
        // Información básica de la conexión (IP, puerto, database, biblioteca si está en el connection string).
        var conn = command.Connection;
        var info = LogHelper.ExtractDbConnectionInfo(conn?.ConnectionString);

        // 1) Nombre de base de datos:
        //    - Preferir lo que venga del connection string.
        //    - Si no existe, usar la propiedad Database del DbConnection (como en el log de éxito).
        var databaseName = !string.IsNullOrWhiteSpace(info.Database)
            ? info.Database
            : (conn?.Database ?? "Desconocida");

        // 2) Tabla/esquema a partir del SQL.
        //    ExtractTableName puede devolver "schema.tabla" o solo "tabla".
        var rawTable = LogHelper.ExtractTableName(command.CommandText);
        string schema = info.Library;   // biblioteca/esquema desde connection string, si existe
        string tableName = rawTable;

        if (string.IsNullOrWhiteSpace(schema))
        {
            // Si no vino biblioteca en la cadena de conexión, la inferimos del SQL.
            var parts = rawTable.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                schema = parts[0];      // bcah96dta
                tableName = parts[1];   // iposre01g1
            }
        }

        if (string.IsNullOrWhiteSpace(schema))
        {
            schema = "Desconocida";
        }

        // 3) IP y puerto: si no se pudieron resolver desde el connection string,
        //    al menos rellenar IP con DataSource para tener algo útil en el log.
        var ip = !string.IsNullOrWhiteSpace(info.Ip)
            ? info.Ip
            : (conn?.DataSource ?? "Desconocida");

        var port = info.Port; // si no se pudo parsear, estará en 0 que es aceptable como "no definido"

        // 4) Construir el bloque de error estructurado reutilizando el formateador estándar.
        var formatted = LogFormatter.FormatDbExecutionError(
            nombreBD: databaseName,
            ip: ip,
            puerto: port,
            biblioteca: schema,
            tabla: tableName,
            sentenciaSQL: command.CommandText,
            exception: ex,
            horaError: DateTime.Now
        );

        if (context is not null)
        {
            // Preferimos el INICIO real que puso el wrapper; si no, ahora (UTC).
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
            // Fallback sin contexto HTTP: se escribe directo en el archivo del request actual.
            WriteLog(context, formatted);
        }

        // Mantener también el rastro transversal de la excepción.
        AddExceptionLog(ex);
    }
    catch (Exception fail)
    {
        LogInternalError(fail);
    }
}




