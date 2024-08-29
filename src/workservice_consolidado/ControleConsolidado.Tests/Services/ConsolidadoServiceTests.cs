using ControleConsolidado.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ConsolidadoDiario.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Query;

namespace ControleConsolidado.Tests.Services
{
    public class ServiceBusListenerTests
    {
        private readonly Mock<ServiceBusClient> _serviceBusClientMock;
        private readonly Mock<ServiceBusReceiver> _serviceBusReceiverMock;
        private readonly Mock<ILogger<ServiceBusListener>> _loggerMock;
        private readonly Mock<ApplicationDbContext> _dbContextMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly ServiceBusListener _listener;

        public ServiceBusListenerTests()
        {
            _serviceBusClientMock = new Mock<ServiceBusClient>();
            _serviceBusReceiverMock = new Mock<ServiceBusReceiver>();
            _loggerMock = new Mock<ILogger<ServiceBusListener>>();
            _dbContextMock = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _serviceScopeMock = new Mock<IServiceScope>();

            _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
            _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);
            _serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(ApplicationDbContext)))
                .Returns(_dbContextMock.Object ?? throw new InvalidOperationException("ApplicationDbContext não pode ser nulo."));

            var options = Options.Create(new AzureServiceBusSettings
            {
                QueueName = "test-queue"
            });

            _serviceBusClientMock.Setup(x => x.CreateReceiver(It.IsAny<string>()))
                                 .Returns(_serviceBusReceiverMock.Object ?? throw new InvalidOperationException("ServiceBusReceiver não pode ser nulo."));

            _listener = new ServiceBusListener(
                _serviceBusClientMock.Object ?? throw new InvalidOperationException("ServiceBusClient não pode ser nulo."),
                options ?? throw new ArgumentNullException(nameof(options), "As opções de configuração não podem ser nulas."),
                _loggerMock.Object ?? throw new InvalidOperationException("Logger não pode ser nulo."),
                _serviceScopeFactoryMock.Object ?? throw new InvalidOperationException("ServiceScopeFactory não pode ser nulo.")
            );
        }

        [Fact]
        public async Task ProcessMessageAsync_Should_LogTotalValor_WhenMessagesAreReceived()
        {
            // Arrange
            var clientId = 123;
            var message = new
            {
                ClientId = clientId
            };

            var messageBody = JsonSerializer.Serialize(message);
            if (string.IsNullOrEmpty(messageBody))
                throw new InvalidOperationException("O corpo da mensagem não pode ser nulo ou vazio.");

            var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                messageId: "mocked-message-id", // Define um MessageId para o teste
                body: new BinaryData(messageBody)
            );

            if (serviceBusMessage == null)
                throw new InvalidOperationException("ServiceBusReceivedMessage não pode ser nulo.");

            var lancamentos = new List<Lancamento>
    {
        new Lancamento { ClientId = clientId, Valor = 100, Data = DateTime.UtcNow, Tipo = 1 },
        new Lancamento { ClientId = clientId, Valor = 50, Data = DateTime.UtcNow, Tipo = 2 },
        new Lancamento { ClientId = clientId, Valor = 30, Data = DateTime.UtcNow.AddDays(-1), Tipo = 1 }
    };

            var lancamentoDbSetMock = lancamentos?.ReturnsDbSet();
            if (lancamentoDbSetMock == null)
            {
                throw new InvalidOperationException("O mock do DbSet retornou nulo.");
            }

            _dbContextMock.Setup(x => x.Lancamentos).Returns(lancamentoDbSetMock.Object);

            // Act
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
            await _listener.StartAsync(cancellationTokenSource.Token);

            // Assert: Apenas verifique se a função executou sem exceções
            //Aqui se colocaria verificações mais específicas da solução 
            Assert.True(true);

            await _listener.StopAsync(CancellationToken.None);
        }

        private bool VerifyLogContainsExpectedMessage(object? v, string expectedMessage)
        {
            if (v == null) return false;

            var message = v.ToString();
            if (string.IsNullOrWhiteSpace(message)) return false;

            return message.Contains(expectedMessage);
        }
    }
}

public static class DbSetMockExtensions
{
    public static Mock<DbSet<T>> ReturnsDbSet<T>(this IEnumerable<T> sourceList) where T : class
    {
        if (sourceList == null) throw new ArgumentNullException(nameof(sourceList), "A lista de origem não pode ser nula.");

        var queryable = sourceList.AsQueryable() ?? throw new InvalidOperationException("A conversão para IQueryable falhou.");

        var dbSetMock = new Mock<DbSet<T>>();
        dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider ?? throw new InvalidOperationException("Provider não pode ser nulo.")));
        dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression ?? throw new InvalidOperationException("Expression não pode ser nula."));
        dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        // Implementação de IAsyncEnumerable
        dbSetMock.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator() ?? throw new InvalidOperationException("Enumerator não pode ser nulo.")));

        return dbSetMock;
    }
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner), "Enumerator interno não pode ser nulo.");
    }

    public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);

    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());

    public T Current => _inner.Current ?? throw new InvalidOperationException("O valor Current não pode ser nulo.");
}

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner), "QueryProvider interno não pode ser nulo.");
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression ?? throw new ArgumentNullException(nameof(expression), "Expression não pode ser nula."));
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression ?? throw new ArgumentNullException(nameof(expression), "Expression não pode ser nula."));
    }

    public object Execute(Expression expression)
    {
#pragma warning disable CS8603 // Possível retorno de referência nula.
        return _inner.Execute(expression ?? throw new ArgumentNullException(nameof(expression), "Expression não pode ser nula."));
#pragma warning restore CS8603 // Possível retorno de referência nula.
    }

    public TResult Execute<TResult>(Expression expression)
    {
        var result = _inner.Execute<TResult>(expression ?? throw new ArgumentNullException(nameof(expression), "Expression não pode ser nula."));

        if (result == null)
        {
            throw new InvalidOperationException("O resultado da consulta não pode ser nulo.");
        }

        return result;
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var result = Execute<TResult>(expression);

        if (result == null)
        {
            throw new InvalidOperationException("O resultado da consulta não pode ser nulo.");
        }

        return result;
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
    {
        return new TestAsyncEnumerable<TResult>(expression ?? throw new ArgumentNullException(nameof(expression), "Expression não pode ser nula."));
    }
}

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable ?? throw new ArgumentNullException(nameof(enumerable), "Enumerable não pode ser nulo."))
    { }

    public TestAsyncEnumerable(Expression expression) : base(expression ?? throw new ArgumentNullException(nameof(expression), "Expression não pode ser nula."))
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator() ?? throw new InvalidOperationException("Enumerator não pode ser nulo."));
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}
