using ContaCorrente.Api.Application.Contas.Commands.Movimentar;
using ContaCorrente.Api.Application.Exceptions;
using ContaCorrente.Api.Domain;
using ContaCorrente.Api.Domain.Exceptions;
using ContaCorrente.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ContaCorrente.Tests.Application;

public class MovimentarCommandHandlerTests : IDisposable
{
    private readonly AmbienteDeAplicacao _ambiente = new();

    private async Task<Guid> CriarContaAsync(decimal saldoInicial = 0m)
    {
        var conta = new Conta("Empresa Exemplo LTDA", Guid.NewGuid().ToString("N")[..14]);
        if (saldoInicial > 0)
        {
            conta.Creditar(saldoInicial, "Saldo inicial");
        }

        _ambiente.Db.Contas.Add(conta);
        await _ambiente.Db.SaveChangesAsync();
        _ambiente.ReiniciarEscopo();

        return conta.Id;
    }

    [Fact]
    public async Task Credito_PersisteMovimentacaoEAtualizaSaldo()
    {
        var contaId = await CriarContaAsync();

        var resposta = await _ambiente.Dispatcher.Send(
            new MovimentarCommand(contaId, TipoMovimentacao.Credito, 1500.25m, "Recebimento"));

        resposta.Valor.Should().Be(1500.25m);
        resposta.SaldoResultante.Should().Be(1500.25m);

        _ambiente.ReiniciarEscopo();
        var conta = await _ambiente.Db.Contas.AsNoTracking().SingleAsync(c => c.Id == contaId);

        conta.Saldo.Should().Be(1500.25m, "o saldo precisa ter sido comitado no banco");
        (await _ambiente.Db.Movimentacoes.CountAsync(m => m.ContaId == contaId)).Should().Be(1);
    }

    [Fact]
    public async Task Debito_SemSaldo_NaoPersisteNadaEPropagaExcecao()
    {
        var contaId = await CriarContaAsync(saldoInicial: 100m);

        var acao = async () => await _ambiente.Dispatcher.Send(
            new MovimentarCommand(contaId, TipoMovimentacao.Debito, 500m, "Tentativa"));

        await acao.Should().ThrowAsync<SaldoInsuficienteException>();

        _ambiente.ReiniciarEscopo();
        var conta = await _ambiente.Db.Contas.AsNoTracking().SingleAsync(c => c.Id == contaId);

        // A transação do dispatcher deve ter sido revertida por inteiro.
        conta.Saldo.Should().Be(100m);
        (await _ambiente.Db.Movimentacoes.CountAsync(m => m.ContaId == contaId))
            .Should().Be(1, "só o crédito inicial pode existir");
    }

    [Fact]
    public async Task ContaInexistente_LancaContaNaoEncontrada()
    {
        var acao = async () => await _ambiente.Dispatcher.Send(
            new MovimentarCommand(Guid.NewGuid(), TipoMovimentacao.Credito, 10m, null));

        await acao.Should().ThrowAsync<ContaNaoEncontradaException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ValorNaoPositivo_ParaNaValidacaoAntesDoHandler(decimal valor)
    {
        var contaId = await CriarContaAsync(saldoInicial: 100m);

        var acao = async () => await _ambiente.Dispatcher.Send(
            new MovimentarCommand(contaId, TipoMovimentacao.Credito, valor, null));

        var excecao = await acao.Should().ThrowAsync<ValidacaoException>();
        excecao.Which.Erros.Should().ContainKey(nameof(MovimentarCommand.Valor));
    }

    [Fact]
    public async Task Contas_SaoIsoladas_MovimentacaoDeUmaNaoAfetaOutra()
    {
        var contaA = await CriarContaAsync(saldoInicial: 1000m);
        var contaB = await CriarContaAsync(saldoInicial: 1000m);

        await _ambiente.Dispatcher.Send(
            new MovimentarCommand(contaA, TipoMovimentacao.Debito, 400m, null));

        _ambiente.ReiniciarEscopo();

        (await _ambiente.Db.Contas.AsNoTracking().SingleAsync(c => c.Id == contaA)).Saldo.Should().Be(600m);
        (await _ambiente.Db.Contas.AsNoTracking().SingleAsync(c => c.Id == contaB)).Saldo.Should().Be(1000m);
    }

    [Fact]
    public async Task SequenciaDeMovimentacoes_MantemSaldoEExtratoConsistentes()
    {
        var contaId = await CriarContaAsync();

        await _ambiente.Dispatcher.Send(new MovimentarCommand(contaId, TipoMovimentacao.Credito, 1000m, null));
        await _ambiente.Dispatcher.Send(new MovimentarCommand(contaId, TipoMovimentacao.Debito, 300m, null));
        await _ambiente.Dispatcher.Send(new MovimentarCommand(contaId, TipoMovimentacao.Credito, 50.50m, null));
        await _ambiente.Dispatcher.Send(new MovimentarCommand(contaId, TipoMovimentacao.Debito, 0.50m, null));

        _ambiente.ReiniciarEscopo();

        var conta = await _ambiente.Db.Contas.AsNoTracking().SingleAsync(c => c.Id == contaId);
        var movimentacoes = await _ambiente.Db.Movimentacoes
            .AsNoTracking()
            .Where(m => m.ContaId == contaId)
            .ToListAsync();

        conta.Saldo.Should().Be(750m);

        // Reconciliação: somar o extrato tem de reproduzir exatamente o saldo materializado.
        var somaDoExtrato = movimentacoes.Sum(m =>
            m.Tipo == TipoMovimentacao.Credito ? m.Valor : -m.Valor);

        somaDoExtrato.Should().Be(conta.Saldo);
    }

    public void Dispose() => _ambiente.Dispose();
}
