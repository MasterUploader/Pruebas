/// <summary>
/// Registra un log de SQL con error y lo encola con el INICIO real para mantener el orden cronológico.
/// Completa información de base de datos y biblioteca a partir de la cadena de conexión, del DbConnection
/// y del propio comando SQL.
/// </summary>
/// <param name="command">Comando de base de datos que produjo el error.</param>
/// <param name="ex">Excepción lanzada por el proveedor de datos.</param>
/// <param name="context">Contexto HTTP actual (si existe) para asociar el log al TraceId de la petición.</param>
public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
{
    try
    {
        // Información básica de la conexión (IP, puerto, database, library si está en el connection string).
        var conn = command.Connection;
        var info = LogHelper.ExtractDbConnectionInfo(conn?.ConnectionString);

        // 1) Tabla/esquema a partir del SQL.
        //    ExtractTableName puede devolver "bcah96dta.iposre01g1" o solo "iposre01g1".
        var rawTable = LogHelper.ExtractTableName(command.CommandText);

        string schema = info.Library;     // Biblioteca/esquema desde la cadena de conexión (si existe).
        string tableName = rawTable;      // Nombre de tabla tal cual lo devolvió el helper.

        if (!string.IsNullOrWhiteSpace(rawTable))
        {
            var parts = rawTable.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                // Si no venía biblioteca en el connection string, la inferimos del SQL.
                if (string.IsNullOrWhiteSpace(schema))
                {
                    schema = parts[0];    // bcah96dta
                }

                tableName = parts[^1];    // iposre01g1
            }
        }

        if (string.IsNullOrWhiteSpace(schema))
        {
            schema = "Desconocida";
        }

        // 2) Nombre de base de datos:
        //    - Preferir lo que venga del connection string.
        //    - Si está vacío, usar conn.Database (como en el log de ejecución SQL).
        //    - Si aún está vacío, usar el esquema como "database lógica".
        var databaseName = !string.IsNullOrWhiteSpace(info.Database)
            ? info.Database
            : (!string.IsNullOrWhiteSpace(conn?.Database)
                ? conn!.Database
                : schema);

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            databaseName = "Desconocida";
        }

        // 3) IP:
        //    - Preferir la IP interpretada desde la cadena de conexión.
        //    - Si viene vacía, usar DataSource como fallback.
        var ip = !string.IsNullOrWhiteSpace(info.Ip)
            ? info.Ip
            : (conn?.DataSource ?? "Desconocida");

        // 4) Construir el bloque de error reutilizando el formateador estándar.
        var formatted = LogFormatter.FormatDbExecutionError(
            nombreBD: databaseName,
            ip: ip,
            puerto: info.Port,   // Si no se pudo parsear, será 0 y se muestra tal cual.
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

        // Además del bloque estructurado, mantenemos el rastro transversal de la excepción.
        AddExceptionLog(ex);
    }
    catch (Exception fail)
    {
        // El logging nunca debe romper la aplicación; se registra el fallo interno y se continúa.
        LogInternalError(fail);
    }
}
