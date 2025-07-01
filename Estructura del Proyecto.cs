namespace Logging.Models
{
    /// <summary>
    /// Representa la información de una ejecución SQL realizada mediante un comando.
    /// </summary>
    public class DbExecutionInfo
    {
        /// <summary>
        /// Comando SQL ejecutado.
        /// </summary>
        public string Sql { get; set; } = string.Empty;

        /// <summary>
        /// Tiempo total de ejecución en milisegundos.
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Resultado de la ejecución (cantidad de filas afectadas, lector, etc.).
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Hora de inicio de la ejecución.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Tipo de operación ejecutada (ExecuteNonQuery, ExecuteScalar, etc.).
        /// </summary>
        public string CommandType { get; set; } = string.Empty;
    }
}
