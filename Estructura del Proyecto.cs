using System;
using System.Threading.Tasks;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Define los métodos estándar para conexiones a colas de mensajes (RabbitMQ, Kafka).
    /// </summary>
    public interface IMessageQueueConnection : IDisposable
    {
        /// <summary>
        /// Publica un mensaje en la cola.
        /// </summary>
        /// <param name="queueName">Nombre de la cola.</param>
        /// <param name="message">Mensaje a enviar.</param>
        Task PublishMessageAsync(string queueName, string message);

        /// <summary>
        /// Consume un mensaje de la cola.
        /// </summary>
        /// <param name="queueName">Nombre de la cola.</param>
        /// <returns>Mensaje recibido.</returns>
        Task<string> ConsumeMessageAsync(string queueName);
    }
}
