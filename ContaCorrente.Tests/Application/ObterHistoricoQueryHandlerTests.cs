using ContaCorrente.Api.Application.Contas.Commands.Movimentar;
using ContaCorrente.Api.Application.Contas.Queries.ObterHistorico;
using ContaCorrente.Api.Domain;
using ContaCorrente.Api.Domain.Exceptions;
using ContaCorrente.Tests.Support;
using FluentAssertions;

namespace ContaCorrente.Tests.Application;

public class ObterHistoricoQueryHandlerTests : IDisposable
{
    private readonly AmbienteDeAplicacao _ambiente = new();

    private async Task<Guid> ContaComMovimentacoesAsync(int creditos)
    {
        var conta = new Conta("Empresa Exemplo LTDA", Guid.NewGuid().ToString("N")[..14]);
        _ambiente.Db.Contas.Add(conta);
        await _ambiente.Db.SaveChangesAsync();
        _ambiente.ReiniciarEscopo();

        for (var i = 1; i <= creditos; i++)
        {
            await _ambiente.Dispatcher.Send(
                new MovimentarCommand(conta.Id, TipoMovimentacao.Credito, i * 10m, $"Lancamento {i}"));
        }

        _ambiente.ReiniciarEscopo();
        return conta.Id;
    }

    [Fact]
    public async Task RetornaDoMaisRecenteParaOMaisAntigo()
    {
        var contaId = await ContaComMovimentacoesAsync(5);

        var pagina = await _ambiente.Dispatcher.Ask(new ObterHistoricoQuery(contaId));

        pagina.Itens.Should().HaveCount(5);
        pagina.Itens.Should().BeInDescendingOrder(m => m.OcorridaEm);
        pagina.Itens[0].Descricao.Should().Be("Lancamento 5");
    }

    [Fact]
    public async Task AplicaPaginacao()
    {
        var contaId = await ContaComMovimentacoesAsync(12);

        var primeira = await _ambiente.Dispatcher.Ask(new ObterHistoricoQuery(contaId, Pagina: 1, Tamanho: 5));
        var ultima = await _ambiente.Dispatcher.Ask(new ObterHistoricoQuery(contaId, Pagina: 3, Tamanho: 5));

        primeira.Itens.Should().HaveCount(5);
        primeira.TotalDeItens.Should().Be(12);
        primeira.TotalDePaginas.Should().Be(3);
        primeira.TemProximaPagina.Should().BeTrue();

        ultima.Itens.Should().HaveCount(2, "a última página fica incompleta");
        ultima.TemProximaPagina.Should().BeFalse();
    }

    [Fact]
    public async Task FiltraPorTipo()
    {
        var contaId = await ContaComMovimentacoesAsync(3);
        await _ambiente.Dispatcher.Send(
            new MovimentarCommand(contaId, TipoMovimentacao.Debito, 5m, "Saida"));
        _ambiente.ReiniciarEscopo();

        var debitos = await _ambiente.Dispatcher.Ask(
            new ObterHistoricoQuery(contaId, Tipo: TipoMovimentacao.Debito));

        debitos.TotalDeItens.Should().Be(1);
        debitos.Itens.Single().Descricao.Should().Be("Saida");
    }

    [Fact]
    public async Task FiltraPorPeriodo()
    {
        var contaId = await ContaComMovimentacoesAsync(3);

        var futuro = await _ambiente.Dispatcher.Ask(
            new ObterHistoricoQuery(contaId, De: DateTime.UtcNow.AddMinutes(5)));

        futuro.Itens.Should().BeEmpty();
        futuro.TotalDeItens.Should().Be(0);
    }

    [Fact]
    public async Task LimitaTamanhoDePaginaEIgnoraPaginaInvalida()
    {
        var contaId = await ContaComMovimentacoesAsync(3);

        var pagina = await _ambiente.Dispatcher.Ask(
            new ObterHistoricoQuery(contaId, Pagina: 0, Tamanho: 5_000));

        pagina.Pagina.Should().Be(1);
        pagina.Tamanho.Should().Be(ObterHistoricoQuery.TamanhoMaximoDePagina);
    }

    [Fact]
    public async Task ContaInexistente_LancaContaNaoEncontrada()
    {
        var acao = async () => await _ambiente.Dispatcher.Ask(new ObterHistoricoQuery(Guid.NewGuid()));

        await acao.Should().ThrowAsync<ContaNaoEncontradaException>();
    }

    public void Dispose() => _ambiente.Dispose();
}
