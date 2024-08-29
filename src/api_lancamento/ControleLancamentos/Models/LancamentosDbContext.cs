using Microsoft.EntityFrameworkCore;

public class LancamentosDbContext : DbContext
{
    public LancamentosDbContext(DbContextOptions<LancamentosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Lancamento> Lancamentos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurações adicionais de mapeamento podem ser feitas aqui
        modelBuilder.Entity<Lancamento>().ToTable("lancamentos");
    }
}
