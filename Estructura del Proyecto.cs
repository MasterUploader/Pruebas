using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Services
{
    /// <summary>
    /// Cliente para conexiones a RabbitMQ.
    /// </summary>
    public class RabbitMQConnectionProvider : IMessageQueueConnection
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMQConnectionProvider(string hostName)
        {
            var factory = new ConnectionFactory() { HostName = hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public Task PublishMessageAsync(string queueName, string message)
        {
            _channel.QueueDeclare(queueName, false, false, false, null);
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish("", queueName, null, body);
            return Task.CompletedTask;
        }

        public Task<string> ConsumeMessageAsync(string queueName)
        {
            _channel.QueueDeclare(queueName, false, false, false, null);
            var consumer = new EventingBasicConsumer(_channel);
            var tcs = new TaskCompletionSource<string>();

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                tcs.SetResult(message);
            };

            _channel.BasicConsume(queueName, true, consumer);
            return tcs.Task;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
