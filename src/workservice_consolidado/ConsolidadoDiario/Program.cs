using Azure.Messaging.ServiceBus;
using ControleConsolidado.Repositories;
using ControleConsolidado.Services;
using ControleConsolidado.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddDbContext<ConsolidadoDbContext>(options =>
//       options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
//           new MySqlServerVersion(new Version(8, 0, 26))));
// Registro dos serviços
builder.Services.AddScoped<IConsolidadoService, ConsolidadoService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
            new MySqlServerVersion(new Version(8, 0, 26))));


builder.Services.Configure<AzureServiceBusSettings>(builder.Configuration.GetSection("AzureServiceBus"));

// Registra o ServiceBusClient
builder.Services.AddSingleton<ServiceBusClient>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<AzureServiceBusSettings>>().Value;
    return new ServiceBusClient(settings.ConnectionString);
});
var connectionString = builder.Configuration["AzureServiceBus:ConnectionString"];
var queueName = builder.Configuration["AzureServiceBus:QueueName"];

// Verificação de nulidade para garantir que os parâmetros não sejam nulos
if (string.IsNullOrEmpty(connectionString))
{
    throw new ArgumentNullException(nameof(connectionString), "ConnectionString cannot be null or empty.");
}

if (string.IsNullOrEmpty(queueName))
{
    throw new ArgumentNullException(nameof(queueName), "QueueName cannot be null or empty.");
}

var serviceBusService = new AzureServiceBusService(connectionString, queueName);
builder.Services.AddSingleton<IAzureServiceBusService>(provider =>
               new AzureServiceBusService(
                   connectionString,
                   queueName));



builder.Services.AddHostedService<ServiceBusListener>();

// Outras configurações...

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
