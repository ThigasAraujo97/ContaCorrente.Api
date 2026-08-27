using System.Text.Json;
using System.Text.Json.Serialization;
using ContaCorrente.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ContaCorrente.Tests.Support;

/// <summary>
/// Sobe a API inteira em memória (pipeline HTTP, DI, serialização, tratamento de erros)
/// apontando para um arquivo SQLite temporário e exclusivo por classe de teste.
/// <para>
/// Arquivo em vez de <c>:memory:</c> de propósito: o teste de concorrência dispara
/// requisições em paralelo, e o banco in-memory depende de uma única conexão
/// compartilhada, que não suporta acesso simultâneo.
/// </para>
/// </summary>
public sealed class ApiEmMemoria : WebApplicationFactory<Program>
{
    private readonly string _caminhoDoBanco =
        Path.Combine(Path.GetTempPath(), $"contacorrente-testes-{Guid.NewGuid():N}.db");

    /// <summary>Espelha a configuração de JSON da API (enum como texto).</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureServices(services =>
        {
            // Troca a connection string do appsettings pelo banco descartável do teste.
            services.RemoveAll<DbContextOptions<ContaCorrenteDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ContaCorrenteDbContext>();

            services.AddDbContext<ContaCorrenteDbContext>(options =>
                options.UseSqlite($"Data Source={_caminhoDoBanco};Default Timeout=30"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // Sem liberar o pool, o arquivo continua aberto e não pode ser removido.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        foreach (var arquivo in new[] { _caminhoDoBanco, $"{_caminhoDoBanco}-wal", $"{_caminhoDoBanco}-shm" })
        {
            try
            {
                if (File.Exists(arquivo))
                {
                    File.Delete(arquivo);
                }
            }
            catch (IOException)
            {
                // Arquivo temporário preso pelo SO: não é motivo para falhar o teste.
            }
        }
    }
}
