using Azure.Messaging.ServiceBus;
using System.Text.Json;
using System.Threading.Tasks;

namespace ControleConsolidado.Utils
{
    public class AzureServiceBusService : IAzureServiceBusService
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly string _queueName;

        public AzureServiceBusService(string connectionString, string queueName)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "ConnectionString cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName), "QueueName cannot be null or empty.");
            }

            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(queueName);
            _queueName = queueName;
        }

        public async Task SendMessageAsync<T>(T message)
        {
            string messageText = JsonSerializer.Serialize(message);
            ServiceBusMessage busMessage = new ServiceBusMessage(messageText);
            await _sender.SendMessageAsync(busMessage);
        }
    }

    public interface IAzureServiceBusService
    {
        Task SendMessageAsync<T>(T message);
    }
}
