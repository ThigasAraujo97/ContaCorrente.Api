using System.Net;
using System.Net.Http.Json;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Domain;
using ContaCorrente.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace ContaCorrente.Tests.Api;

/// <summary>
/// Percorre a API pelo HTTP real: roteamento, binding, pipeline do dispatcher,
/// persistência e tradução de exceções em ProblemDetails.
/// </summary>
public class ContasEndpointsTests : IClassFixture<ApiEmMemoria>
{
    private readonly HttpClient _cliente;

    public ContasEndpointsTests(ApiEmMemoria api) => _cliente = api.CreateClient();

    private async Task<ContaResponse> CriarContaAsync()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/contas", new
        {
            nome = "Empresa Exemplo LTDA",
            documento = Guid.NewGuid().ToString("N")[..14]
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<ContaResponse>(ApiEmMemoria.Json))!;
    }

    private Task<HttpResponseMessage> MovimentarAsync(
        Guid contaId, string tipo, decimal valor, string? descricao = null)
        => _cliente.PostAsJsonAsync(
            $"/api/contas/{contaId}/movimentacoes",
            new { tipo, valor, descricao });

    [Fact]
    public async Task PostConta_CriaComSaldoZero()
    {
        var conta = await CriarContaAsync();

        conta.Id.Should().NotBeEmpty();
        conta.Saldo.Should().Be(0m);
    }

    [Fact]
    public async Task PostMovimentacao_Credito_RetornaCriadoEAtualizaSaldo()
    {
        var conta = await CriarContaAsync();

        var resposta = await MovimentarAsync(conta.Id, "Credito", 1000m, "Aporte");
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);

        var movimentacao = (await resposta.Content.ReadFromJsonAsync<MovimentacaoResponse>(ApiEmMemoria.Json))!;
        movimentacao.Tipo.Should().Be(TipoMovimentacao.Credito);
        movimentacao.SaldoResultante.Should().Be(1000m);

        var saldo = await _cliente.GetFromJsonAsync<SaldoResponse>(
            $"/api/contas/{conta.Id}/saldo", ApiEmMemoria.Json);

        saldo!.Saldo.Should().Be(1000m);
    }

    [Fact]
    public async Task PostMovimentacao_Debito_ReduzSaldo()
    {
        var conta = await CriarContaAsync();
        await MovimentarAsync(conta.Id, "Credito", 1000m);

        await MovimentarAsync(conta.Id, "Debito", 300m);

        var saldo = await _cliente.GetFromJsonAsync<SaldoResponse>(
            $"/api/contas/{conta.Id}/saldo", ApiEmMemoria.Json);

        saldo!.Saldo.Should().Be(700m);
    }

    [Fact]
    public async Task PostMovimentacao_DebitoSemSaldo_Retorna422ComProblemDetails()
    {
        var conta = await CriarContaAsync();
        await MovimentarAsync(conta.Id, "Credito", 100m);

        var resposta = await MovimentarAsync(conta.Id, "Debito", 5000m);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>(ApiEmMemoria.Json);
        problema!.Title.Should().Be("Saldo insuficiente");
        problema.Extensions.Should().ContainKey("saldoDisponivel");

        // E o saldo tem de continuar intacto.
        var saldo = await _cliente.GetFromJsonAsync<SaldoResponse>(
            $"/api/contas/{conta.Id}/saldo", ApiEmMemoria.Json);
        saldo!.Saldo.Should().Be(100m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task PostMovimentacao_ValorNaoPositivo_Retorna400ComErrosPorCampo(decimal valor)
    {
        var conta = await CriarContaAsync();

        var resposta = await MovimentarAsync(conta.Id, "Credito", valor);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiEmMemoria.Json);
        problema!.Errors.Should().ContainKey("Valor");
    }

    [Fact]
    public async Task PostMovimentacao_ContaInexistente_Retorna404()
    {
        var resposta = await MovimentarAsync(Guid.NewGuid(), "Credito", 10m);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSaldo_ContaInexistente_Retorna404()
    {
        var resposta = await _cliente.GetAsync($"/api/contas/{Guid.NewGuid()}/saldo");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistorico_RetornaOrdenadoDoMaisRecenteEPaginado()
    {
        var conta = await CriarContaAsync();
        for (var i = 1; i <= 7; i++)
        {
            await MovimentarAsync(conta.Id, "Credito", i * 10m, $"Lancamento {i}");
        }

        var pagina = await _cliente.GetFromJsonAsync<PaginaResponse<MovimentacaoResponse>>(
            $"/api/contas/{conta.Id}/movimentacoes?pagina=1&tamanho=3", ApiEmMemoria.Json);

        pagina!.Itens.Should().HaveCount(3);
        pagina.TotalDeItens.Should().Be(7);
        pagina.Itens[0].Descricao.Should().Be("Lancamento 7");
        pagina.Itens.Should().BeInDescendingOrder(m => m.OcorridaEm);
    }

    [Fact]
    public async Task Contas_SaoIsoladas_MovimentacaoDeUmaNaoAfetaOutra()
    {
        var contaA = await CriarContaAsync();
        var contaB = await CriarContaAsync();

        await MovimentarAsync(contaA.Id, "Credito", 500m);

        var saldoB = await _cliente.GetFromJsonAsync<SaldoResponse>(
            $"/api/contas/{contaB.Id}/saldo", ApiEmMemoria.Json);

        saldoB!.Saldo.Should().Be(0m);
    }

    [Fact]
    public async Task Datas_SaoSerializadasEmUtcComSufixoZ()
    {
        var conta = await CriarContaAsync();
        await MovimentarAsync(conta.Id, "Credito", 10m);

        // Sem o sufixo Z, o JavaScript interpretaria a data como horário local e o
        // extrato apareceria deslocado pelo fuso do navegador.
        var saldoJson = await _cliente.GetStringAsync($"/api/contas/{conta.Id}/saldo");
        var historicoJson = await _cliente.GetStringAsync($"/api/contas/{conta.Id}/movimentacoes");

        saldoJson.Should().MatchRegex(@"""atualizadoEm"":""[^""]+Z""");
        historicoJson.Should().MatchRegex(@"""ocorridaEm"":""[^""]+Z""");
    }

    [Fact]
    public async Task Health_RespondeOk()
    {
        var resposta = await _cliente.GetAsync("/health");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
