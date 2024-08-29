using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ControleConsolidado.Services
{
    public class ServiceBusListener : BackgroundService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly string _queueName;
        private readonly ILogger<ServiceBusListener> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceBusListener(ServiceBusClient serviceBusClient, IOptions<AzureServiceBusSettings> options, ILogger<ServiceBusListener> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient), "ServiceBusClient não pode ser nulo.");
            _queueName = options?.Value.QueueName ?? throw new ArgumentNullException(nameof(options), "QueueName não pode ser nulo.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger não pode ser nulo.");
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory), "ServiceScopeFactory não pode ser nulo.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service Bus Listener is starting.");

            var receiver = _serviceBusClient.CreateReceiver(_queueName) ?? throw new InvalidOperationException("Falha ao criar o ServiceBusReceiver.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Waiting for messages...");

                    var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10, cancellationToken: stoppingToken);

                    if (messages == null || !messages.Any())
                    {
                        _logger.LogInformation("Nenhuma mensagem recebida. Aguardando antes de tentar novamente.");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Espera de 5 segundos antes de tentar novamente
                        continue; // Continua o loop sem encerrar a aplicação
                    }

                    foreach (var message in messages)
                    {
                        try
                        {
                            if (message == null || message.Body == null)
                            {
                                _logger.LogWarning("Mensagem ou corpo da mensagem nulos recebidos, ignorando.");
                                continue;
                            }

                            _logger.LogInformation($"Received message: {message.Body}");

                            // Processa a mensagem e consulta o banco de dados
                            await ProcessMessageAsync(message);

                            if (string.IsNullOrWhiteSpace(message.MessageId))
                            {
                                _logger.LogError("Message ID is missing, cannot complete message.");
                                continue;
                            }

                            _logger.LogInformation($"Attempting to complete message with ID: {message.MessageId}");
                            await receiver.CompleteMessageAsync(message);
                            _logger.LogInformation($"Message with ID: {message.MessageId} completed successfully.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error processing message: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unhandled exception in ExecuteAsync: {ex.Message}");
                }
            }
        }

        private async Task ProcessMessageAsync(ServiceBusReceivedMessage message)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                               ?? throw new InvalidOperationException("ApplicationDbContext não pode ser nulo.");

                if (string.IsNullOrEmpty(message.Body.ToString()))
                {
                    _logger.LogWarning("Corpo da mensagem está vazio.");
                    return;
                }

                JsonElement dados;
                try
                {
                    dados = JsonSerializer.Deserialize<JsonElement>(message.Body.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Falha ao desserializar o corpo da mensagem: {ex.Message}");
                    return;
                }

                if (dados.TryGetProperty("ClientId", out JsonElement clientIdElement) && clientIdElement.TryGetInt32(out int clientId))
                {
                    var dataAtual = DateTime.UtcNow.Date;

                    var lancamentos = await dbContext.Lancamentos
                                                     .Where(l => l.ClientId == clientId && l.Data.Date == dataAtual)
                                                     .ToListAsync();

                    if (lancamentos == null || !lancamentos.Any())
                    {
                        _logger.LogInformation($"Nenhum lançamento encontrado para ClientId: {clientId} na data: {dataAtual.ToShortDateString()}.");
                        return;
                    }

                    var totalValor = lancamentos.Where(l => l.Tipo == 1).Sum(l => l.Valor) -
                                     lancamentos.Where(l => l.Tipo == 2).Sum(l => l.Valor);

                    _logger.LogInformation("\r==============================================================================================\r\r"+
                        " RESULTADO : \r\r"
                        + $" ClientId: {clientId}, Total Valor: {totalValor} para a data: {dataAtual.ToShortDateString()}\r\r" +
                        "==============================================================================================");
                }
                else
                {
                    _logger.LogWarning("ClientId não encontrado ou inválido na mensagem.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service Bus Listener is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}
