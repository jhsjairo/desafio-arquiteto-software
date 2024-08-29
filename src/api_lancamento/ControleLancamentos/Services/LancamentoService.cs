using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILancamentoService
{
    Task AddLancamentoAsync(Lancamento lancamento);
    Task<Lancamento?> GetLancamentoByIdAsync(int id); 
}

public class LancamentoService : ILancamentoService
{
    private readonly ILancamentoRepository _lancamentoRepository;

    public LancamentoService(ILancamentoRepository lancamentoRepository)
    {
        _lancamentoRepository = lancamentoRepository;
    }

    public async Task AddLancamentoAsync(Lancamento lancamento)
    {
        if (lancamento.Valor <= 0)
        {
            throw new ArgumentException("O valor do lançamento deve ser positivo.");
        }

        await _lancamentoRepository.AddAsync(lancamento);
    }

    public async Task<Lancamento?> GetLancamentoByIdAsync(int id) // Modificado para lidar com a possibilidade de retorno nulo
    {
        return await _lancamentoRepository.GetByIdAsync(id);
    }

}
