using ContaCorrente.Api.Domain;
using ContaCorrente.Api.Infrastructure.Converters;
using Microsoft.EntityFrameworkCore;

namespace ContaCorrente.Api.Infrastructure;

/// <summary>
/// Contexto de persistência. Também cumpre o papel de Unit of Work — por isso o projeto
/// não define um IRepository/IUnitOfWork por cima: seria indireção sem ganho.
/// </summary>
public class ContaCorrenteDbContext(DbContextOptions<ContaCorrenteDbContext> options)
    : DbContext(options)
{
    public DbSet<Conta> Contas => Set<Conta>();

    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContaCorrenteDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Aplica a todas as datas do modelo de uma vez, em vez de repetir em cada
        // propriedade — e vale automaticamente para qualquer data futura.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

        base.ConfigureConventions(configurationBuilder);
    }
}
