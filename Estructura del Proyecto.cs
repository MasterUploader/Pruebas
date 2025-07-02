public static class LogFormatter
{
    /// <summary>
    /// Da formato al log estructurado de una ejecución SQL para fines de almacenamiento en log de texto plano.
    /// </summary>
    /// <param name="model">Modelo de log SQL estructurado.</param>
    /// <returns>Cadena con formato estándar para logging de SQL.</returns>
    public static string FormatDbExecution(SqlLogModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== LOG DE EJECUCIÓN SQL =====");
        sb.AppendLine($"Fecha y Hora      : {model.StartTime:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Duración          : {model.Duration.TotalMilliseconds} ms");
        sb.AppendLine($"Base de Datos     : {model.DatabaseName}");
        sb.AppendLine($"IP                : {model.Ip}");
        sb.AppendLine($"Puerto            : {model.Port}");
        sb.AppendLine($"Esquema           : {model.Schema}");
        sb.AppendLine($"Tabla             : {model.TableName}");
        sb.AppendLine($"Veces Ejecutado   : {model.ExecutionCount}");
        sb.AppendLine($"Filas Afectadas   : {model.TotalAffectedRows}");
        sb.AppendLine("SQL:");
        sb.AppendLine(model.Sql);
        sb.AppendLine("================================");

        return sb.ToString();
    }
}
