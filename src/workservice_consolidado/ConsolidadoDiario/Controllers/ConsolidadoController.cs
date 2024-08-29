using ControleConsolidado.Models;
using ControleConsolidado.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ControleConsolidado.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsolidadoController : ControllerBase
    {
        private readonly IConsolidadoService _consolidadoService;

        public ConsolidadoController(IConsolidadoService consolidadoService)
        {
            _consolidadoService = consolidadoService;
        }


        [HttpPost("alimentar-fila")]
        public async Task<IActionResult> AlimentarFila([FromBody] Consolidado consolidado)
        {
            // Este método envia o consolidado para o Service Bus sem processar imediatamente.
            await _consolidadoService.EnviarParaFilaAsync(consolidado);
            return Ok("Consolidado enviado para o Service Bus.");
        }
    }
}
