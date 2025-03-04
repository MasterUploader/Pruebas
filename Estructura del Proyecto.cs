
    /// <summary>
    /// Define la interfaz para el servicio de logging.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Agrega un log de texto simple.
        /// </summary>
        void AddSingleLog(string message);

        /// <summary>
        /// Agrega un log con un objeto serializado.
        /// </summary>
        void AddObjLog(string objectName, object obj);

        /// <summary>
        /// Escribe los logs en el archivo.
        /// </summary>
        void FlushLogs();

        /// <summary>
        /// Registra la entrada de un método con sus parámetros.
        /// </summary>
        void AddMethodEntryLog(string methodName, string parameters);

        /// <summary>
        /// Registra la salida de un método con su valor de retorno.
        /// </summary>
        void AddMethodExitLog(string methodName, string returnValue);

        /// <summary>
        /// Registra información del entorno (IP, Servidor, SO, etc.).
        /// </summary>
        void AddEnvironmentLog();

        /// <summary>
        /// Registra una excepción en los logs.
        /// </summary>
        void AddExceptionLog(Exception ex);
    }

