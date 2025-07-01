using System;
using System.Collections.Generic;
using System.Text;

namespace RestUtilities.Logging.Helpers
{
    public static class LogFormatter
    {
        /// <summary>
        /// Formatea un bloque estructurado de ejecución SQL para incluirlo en los logs del sistema.
        /// El formato incluye información de la base de datos, dirección IP, puerto, sentencias SQL,
        /// cantidad de ejecuciones, resultado de la operación, hora de inicio y duración.
        /// </summary>
        /// <param name="nombreBD">Nombre de la base de datos utilizada en la ejecución.</param>
        /// <param name="ip">Dirección IP del servidor de base de datos.</param>
        /// <param name="puerto">Puerto utilizado para conectarse a la base de datos.</param>
        /// <param name="sentenciasSQL">Lista de sentencias SQL ejecutadas.</param>
        /// <param name="cantidadEjecuciones">Número total de ejecuciones realizadas.</param>
        /// <param name="resultado">Resultado final de la operación, por ejemplo número de registros afectados.</param>
        /// <param name="horaInicio">Fecha y hora en que inició la ejecución.</param>
        /// <param name="duracion">Duración total de la ejecución.</param>
        /// <returns>
        /// Cadena de texto formateada con el siguiente estilo:
        /// 
        /// ============= DB EXECUTION =============
        /// Nombre BD: BaseDedatos
        /// IP: localhost
        /// Puerto: 2020
        /// SQL:
        /// INSERT INTO tabla (...) VALUES (...)
        /// ...
        /// Cantidad de ejecuciones: 3
        /// Resultado: 1
        /// Hora de inicio: 2025-07-01 12:00:00
        /// Duración: 9 ms
        /// ============= END DB ===================
        /// </returns>
        public static string FormatDbExecution(
            string nombreBD,
            string ip,
            int puerto,
            List<string> sentenciasSQL,
            int cantidadEjecuciones,
            object resultado,
            DateTime horaInicio,
            TimeSpan duracion)
        {
            var sb = new StringBuilder();

            sb.AppendLine("============= DB EXECUTION =============");
            sb.AppendLine($"Nombre BD: {nombreBD}");
            sb.AppendLine($"IP: {ip}");
            sb.AppendLine($"Puerto: {puerto}");
            sb.AppendLine("SQL:");

            foreach (var sentencia in sentenciasSQL)
            {
                sb.AppendLine(sentencia);
            }

            sb.AppendLine();
            sb.AppendLine($"Cantidad de ejecuciones: {cantidadEjecuciones}");
            sb.AppendLine($"Resultado: {resultado}");
            sb.AppendLine($"Hora de inicio: {horaInicio:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Duración: {duracion.TotalMilliseconds} ms");
            sb.AppendLine("============= END DB ===================");

            return sb.ToString();
        }
    }
}
