/// <summary>
/// Formatea un bloque de log para registrar la ejecución de una operación SQL sobre una base de datos,
/// incluyendo los metadatos de conexión, el SQL ejecutado (una sola vez), la cantidad de ejecuciones
/// realizadas, los resultados obtenidos y el tiempo de duración.
/// </summary>
/// <param name="sql">Sentencia SQL base utilizada (no repetida).</param>
/// <param name="database">Nombre de la base de datos o biblioteca utilizada.</param>
/// <param name="table">Nombre de la tabla involucrada (si aplica).</param>
/// <param name="ip">IP o servidor de la base de datos.</param>
/// <param name="port">Puerto utilizado en la conexión.</param>
/// <param name="totalAffectedRows">Número total de filas afectadas por todas las ejecuciones.</param>
/// <param name="executionCount">Número de veces que se ejecutó la sentencia.</param>
/// <param name="startTime">Hora exacta en que se inició la operación.</param>
/// <param name="durationMs">Tiempo total de ejecución en milisegundos.</param>
/// <returns>Cadena formateada para el log.</returns>
public static string FormatDbExecution(
    string sql,
    string database,
    string table,
    string ip,
    string port,
    int totalAffectedRows,
    int executionCount,
    DateTime startTime,
    long durationMs)
{
    var sb = new StringBuilder();
    sb.AppendLine("============= DB EXECUTION =============");
    sb.AppendLine($"Nombre BD: {database}");
    sb.AppendLine($"IP: {ip}");
    sb.AppendLine($"Puerto: {port}");
    sb.AppendLine($"Biblioteca: {database}");
    sb.AppendLine($"Tabla: {table}");
    sb.AppendLine("SQL:");
    sb.AppendLine(sql);
    sb.AppendLine();
    sb.AppendLine($"Cantidad de ejecuciones: {executionCount}");
    sb.AppendLine($"Resultado: Filas afectadas: {totalAffectedRows}");
    sb.AppendLine($"Hora de inicio: {startTime:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine($"Duración: {durationMs} ms");
    sb.AppendLine("============= END DB ===================");

    return sb.ToString();
}

string log = LogFormatter.FormatDbExecution(
    sql: "INSERT INTO BCAH96DTA.UTH01CCC (campo1, campo2) VALUES (?, ?)",
    database: "BCAH96DTA",
    table: "UTH01CCC",
    ip: "localhost",
    port: "446",
    totalAffectedRows: 15,
    executionCount: 15,
    startTime: inicioOperacion,
    durationMs: stopwatch.ElapsedMilliseconds
);
