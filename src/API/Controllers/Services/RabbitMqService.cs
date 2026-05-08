using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using System.Threading.Tasks;

namespace WorldBank_CRUD.API.Services
{
    public class RabbitMqService
    {
        private readonly string _hostName = "localhost";

        public async Task PublishMessageAsync(object message, string queueName)
        {
            var factory = new ConnectionFactory() { HostName = _hostName };
            
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: queueName,
                                            durable: true,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null);

            var jsonPayload = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(jsonPayload);

            await channel.BasicPublishAsync(exchange: string.Empty,
                                            routingKey: queueName,
                                            body: body);
        }
    }
}