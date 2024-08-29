using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILancamentoRepository
{
    Task AddAsync(Lancamento lancamento);
    Task<Lancamento?> GetByIdAsync(int id); // Modificado para lidar com a possibilidade de retorno nulo
}

public class LancamentoRepository : ILancamentoRepository
{
    private readonly LancamentosDbContext _context;

    public LancamentoRepository(LancamentosDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Lancamento lancamento)
    {
        await _context.Lancamentos.AddAsync(lancamento);
        await _context.SaveChangesAsync();
    }

    public async Task<Lancamento?> GetByIdAsync(int id) // Modificado para lidar com a possibilidade de retorno nulo
    {
        return await _context.Lancamentos.FindAsync(id);
    }

    
}
