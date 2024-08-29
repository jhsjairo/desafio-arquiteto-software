using ControleConsolidado.Models;
using ControleConsolidado.Utils;
using System.Threading.Tasks;

namespace ControleConsolidado.Services
{
    public class ConsolidadoService : IConsolidadoService
    {
        private readonly IAzureServiceBusService _serviceBusService;

        public ConsolidadoService(IAzureServiceBusService serviceBusService)
        {
            _serviceBusService = serviceBusService ?? throw new ArgumentNullException(nameof(serviceBusService));
        }


        public async Task EnviarParaFilaAsync(Consolidado consolidado)
        {
            // Apenas envia o dado para o Service Bus sem processar
            await _serviceBusService.SendMessageAsync(consolidado);
        }
    }

    public interface IConsolidadoService
    {
        Task EnviarParaFilaAsync(Consolidado consolidado);
    }
}
