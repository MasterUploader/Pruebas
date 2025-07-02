using System;

namespace Logging.Models
{
    /// <summary>
    /// Modelo que encapsula los detalles de una ejecución SQL para ser utilizado en el logging estructurado.
    /// </summary>
    public class SqlLogModel
    {
        /// <summary>
        /// Sentencia SQL ejecutada (puede incluir placeholders si se usan parámetros).
        /// </summary>
        public string Sql { get; set; } = string.Empty;

        /// <summary>
        /// Número total de veces que se ejecutó el mismo comando SQL (por ejemplo, múltiples inserts con el mismo comando).
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Número total de filas afectadas por todas las ejecuciones del comando.
        /// </summary>
        public int TotalAffectedRows { get; set; }

        /// <summary>
        /// Fecha y hora en que inició la primera ejecución del comando.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Duración total acumulada de todas las ejecuciones del comando.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Nombre de la base de datos donde se ejecutó la sentencia.
        /// </summary>
        public string DatabaseName { get; set; } = "Desconocida";

        /// <summary>
        /// Dirección IP o nombre del host de la base de datos.
        /// </summary>
        public string Ip { get; set; } = "Desconocida";

        /// <summary>
        /// Puerto de conexión a la base de datos (si se conoce).
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Nombre de la tabla sobre la que se ejecutó el comando, si puede determinarse.
        /// </summary>
        public string TableName { get; set; } = "Desconocida";

        /// <summary>
        /// Nombre del esquema o biblioteca en AS400 u otros motores.
        /// </summary>
        public string Schema { get; set; } = "Desconocido";
    }
}
