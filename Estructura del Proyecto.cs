using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Services
{
    /// <summary>
    /// Proveedor de conexión para RabbitMQ en .NET 8 basado en la documentación oficial.
    /// </summary>
    public class RabbitMQConnectionProvider : IMessageQueueConnection, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="RabbitMQConnectionProvider"/>.
        /// </summary>
        /// <param name="hostName">Dirección del servidor RabbitMQ.</param>
        /// <param name="userName">Nombre de usuario para autenticación.</param>
        /// <param name="password">Contraseña para autenticación.</param>
        public RabbitMQConnectionProvider(string hostName, string userName, string password)
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Publica un mensaje en la cola especificada de manera asincrónica.
        /// </summary>
        /// <param name="queueName">Nombre de la cola donde se publicará el mensaje.</param>
        /// <param name="message">Contenido del mensaje a publicar.</param>
        /// <returns>Una tarea que representa la operación asincrónica.</returns>
        public async Task PublishMessageAsync(string queueName, string message)
        {
            await _channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false, basicProperties: null, body);
        }

        /// <summary>
        /// Consume un mensaje de la cola especificada de manera asincrónica.
        /// </summary>
        /// <param name="queueName">Nombre de la cola desde donde se consumirá el mensaje.</param>
        /// <returns>Una tarea que representa la operación asincrónica y contiene el mensaje consumido.</returns>
        public async Task<string> ConsumeMessageAsync(string queueName)
        {
            await _channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            var tcs = new TaskCompletionSource<string>();

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                tcs.SetResult(message);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer);

            return await tcs.Task;
        }

        /// <summary>
        /// Libera los recursos asociados con la conexión a RabbitMQ.
        /// </summary>
        public void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
        }
    }
}
