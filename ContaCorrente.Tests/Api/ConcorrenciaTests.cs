using System.Net;
using System.Net.Http.Json;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Domain;
using ContaCorrente.Tests.Support;
using FluentAssertions;

namespace ContaCorrente.Tests.Api;

/// <summary>
/// Prova o requisito "garantir a consistência dos registros, sem permitir saldo negativo"
/// sob acesso simultâneo — a situação em que uma verificação ingênua de saldo falha.
/// </summary>
public class ConcorrenciaTests : IClassFixture<ApiEmMemoria>
{
    private readonly ApiEmMemoria _api;

    public ConcorrenciaTests(ApiEmMemoria api) => _api = api;

    private async Task<Guid> CriarContaComSaldoAsync(HttpClient cliente, decimal saldo)
    {
        var criacao = await cliente.PostAsJsonAsync("/api/contas", new
        {
            nome = "Empresa Concorrencia LTDA",
            documento = Guid.NewGuid().ToString("N")[..14]
        });

        var conta = (await criacao.Content.ReadFromJsonAsync<ContaResponse>(ApiEmMemoria.Json))!;

        await cliente.PostAsJsonAsync($"/api/contas/{conta.Id}/movimentacoes", new
        {
            tipo = "Credito",
            valor = saldo
        });

        return conta.Id;
    }

    [Fact]
    public async Task DebitosConcorrentesAlemDoSaldo_NaoGeramSaldoNegativo()
    {
        const decimal SaldoInicial = 500m;
        const decimal ValorDeCadaDebito = 100m;
        const int TentativasSimultaneas = 20;
        const int DebitosQueCabemNoSaldo = 5; // 500 / 100

        var cliente = _api.CreateClient();
        var contaId = await CriarContaComSaldoAsync(cliente, SaldoInicial);

        // Dispara todos os débitos de uma vez, sem escalonar.
        var requisicoes = Enumerable.Range(0, TentativasSimultaneas)
            .Select(i => cliente.PostAsJsonAsync(
                $"/api/contas/{contaId}/movimentacoes",
                new { tipo = "Debito", valor = ValorDeCadaDebito, descricao = $"Saque {i}" }))
            .ToArray();

        var respostas = await Task.WhenAll(requisicoes);

        var aceitos = respostas.Count(r => r.StatusCode == HttpStatusCode.Created);
        var recusados = respostas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);

        aceitos.Should().Be(
            DebitosQueCabemNoSaldo,
            "o saldo comporta exatamente {0} débitos de {1}", DebitosQueCabemNoSaldo, ValorDeCadaDebito);

        // Todas as demais devem ter sido recusadas por saldo insuficiente — nenhuma
        // pode ter falhado por erro interno.
        (aceitos + recusados).Should().Be(
            TentativasSimultaneas,
            "toda requisição deve terminar em 201 ou 422, nunca em 500");

        var saldo = await cliente.GetFromJsonAsync<SaldoResponse>(
            $"/api/contas/{contaId}/saldo", ApiEmMemoria.Json);

        saldo!.Saldo.Should().Be(0m);
        saldo.Saldo.Should().BeGreaterThanOrEqualTo(0m, "o saldo nunca pode ficar negativo");
    }

    [Fact]
    public async Task MovimentacoesConcorrentesMistas_MantemExtratoReconciliavelComSaldo()
    {
        const decimal SaldoInicial = 1000m;
        const int Pares = 15;

        var cliente = _api.CreateClient();
        var contaId = await CriarContaComSaldoAsync(cliente, SaldoInicial);

        // Créditos e débitos entrelaçados, todos disparados juntos.
        var requisicoes = Enumerable.Range(0, Pares)
            .SelectMany(i => new[]
            {
                cliente.PostAsJsonAsync($"/api/contas/{contaId}/movimentacoes",
                    new { tipo = "Credito", valor = 20m, descricao = $"Entrada {i}" }),
                cliente.PostAsJsonAsync($"/api/contas/{contaId}/movimentacoes",
                    new { tipo = "Debito", valor = 20m, descricao = $"Saida {i}" })
            })
            .ToArray();

        var respostas = await Task.WhenAll(requisicoes);

        respostas.Should().OnlyContain(
            r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.UnprocessableEntity,
            "nenhuma requisição pode resultar em erro interno");

        var saldo = await cliente.GetFromJsonAsync<SaldoResponse>(
            $"/api/contas/{contaId}/saldo", ApiEmMemoria.Json);

        var extrato = await cliente.GetFromJsonAsync<PaginaResponse<MovimentacaoResponse>>(
            $"/api/contas/{contaId}/movimentacoes?tamanho=100", ApiEmMemoria.Json);

        var somaDoExtrato = extrato!.Itens.Sum(m =>
            m.Tipo == TipoMovimentacao.Credito ? m.Valor : -m.Valor);

        // O invariante que importa: saldo materializado == soma do extrato.
        somaDoExtrato.Should().Be(saldo!.Saldo);
        saldo.Saldo.Should().BeGreaterThanOrEqualTo(0m);
    }
}
