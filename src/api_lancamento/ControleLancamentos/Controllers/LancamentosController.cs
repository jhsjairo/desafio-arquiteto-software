// Controlador responsável por expor as APIs relacionadas aos lançamentos
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LancamentosController : ControllerBase
{
    private readonly ILancamentoService _lancamentoService;

    public LancamentosController(ILancamentoService lancamentoService)
    {
        _lancamentoService = lancamentoService;
    }

    // Endpoint para criar um novo lançamento
    [HttpPost]
    public async Task<IActionResult> CreateLancamento([FromBody] Lancamento lancamento)
    {
        // Verifica se o modelo recebido é válido
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Chama o serviço para adicionar o lançamento
        await _lancamentoService.AddLancamentoAsync(lancamento);

        // Retorna o lançamento criado com o status 201 Created
        return CreatedAtAction(nameof(GetLancamento), new { id = lancamento.Id }, lancamento);
    }

    // Endpoint para obter um lançamento pelo ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLancamento(int id)
    {
        var lancamento = await _lancamentoService.GetLancamentoByIdAsync(id);
        if (lancamento == null)
            return NotFound();

        return Ok(lancamento);
    }

}
