using ConsolidadoDiario.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public virtual DbSet<Lancamento> Lancamentos { get; set; } // Torne virtual para permitir mock

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
