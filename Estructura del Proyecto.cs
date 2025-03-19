namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Interfaz para la fábrica de conexiones a servicios.
    /// </summary>
    public interface IServiceConnectionFactory
    {
        /// <summary>
        /// Crea una conexión genérica según el tipo especificado.
        /// </summary>
        /// <typeparam name="T">Tipo de conexión a crear.</typeparam>
        /// <returns>Instancia de la conexión solicitada.</returns>
        T CreateConnection<T>() where T : class;
    }
}
