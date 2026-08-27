using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Dispatch;
using ContaCorrente.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContaCorrente.Tests.Support;

/// <summary>
/// Monta a camada de aplicação completa (dispatcher, handlers e validators reais) sobre
/// um SQLite in-memory. Usa banco de verdade em vez de mocks de repositório: os handlers
/// dependem do DbContext, e transação e concorrência só se comportam como em produção
/// contra um provider real.
/// </summary>
public sealed class AmbienteDeAplicacao : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly ServiceProvider _servicos;

    public AmbienteDeAplicacao()
    {
        // A conexão precisa ficar aberta: o banco in-memory do SQLite existe apenas
        // enquanto houver uma conexão viva.
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ContaCorrenteDbContext>(options => options.UseSqlite(_conexao));
        services.AddApplication();

        _servicos = services.BuildServiceProvider();

        Db.Database.EnsureCreated();
    }

    private IServiceScope? _escopo;

    private IServiceScope Escopo => _escopo ??= _servicos.CreateScope();

    public ContaCorrenteDbContext Db =>
        Escopo.ServiceProvider.GetRequiredService<ContaCorrenteDbContext>();

    public IDispatcher Dispatcher =>
        Escopo.ServiceProvider.GetRequiredService<IDispatcher>();

    /// <summary>
    /// Descarta o escopo atual para que a próxima leitura venha do banco, e não do
    /// change tracker — importante ao verificar o que de fato foi persistido.
    /// </summary>
    public void ReiniciarEscopo()
    {
        _escopo?.Dispose();
        _escopo = null;
    }

    public void Dispose()
    {
        _escopo?.Dispose();
        _servicos.Dispose();
        _conexao.Dispose();
    }
}
