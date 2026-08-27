using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Commands.CriarConta;
using ContaCorrente.Api.Application.Contas.Queries.ListarContas;
using ContaCorrente.Api.Application.Exceptions;
using ContaCorrente.Tests.Support;
using FluentAssertions;

namespace ContaCorrente.Tests.Application;

public class DispatcherTests : IDisposable
{
    private readonly AmbienteDeAplicacao _ambiente = new();

    /// <summary>
    /// Comando declarado só no projeto de testes, portanto sem handler no assembly da API.
    /// Protege contra o modo silencioso de falhar da varredura de assembly: adicionar um
    /// comando e esquecer o handler tem de dar erro claro, não NullReferenceException.
    /// </summary>
    private sealed record ComandoSemHandler : ICommand<string>;

    [Fact]
    public async Task Send_SemHandlerRegistrado_LancaExcecaoDescritiva()
    {
        var acao = async () => await _ambiente.Dispatcher.Send(new ComandoSemHandler());

        (await acao.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*ComandoSemHandler*");
    }

    [Fact]
    public async Task Send_ResolveHandlerPeloTipoConcretoDoComando()
    {
        var conta = await _ambiente.Dispatcher.Send(
            new CriarContaCommand("Empresa Exemplo LTDA", "12345678000199"));

        conta.Id.Should().NotBeEmpty();
        conta.Saldo.Should().Be(0m);
    }

    [Fact]
    public async Task Send_ComandoInvalido_LancaValidacaoComErrosPorCampo()
    {
        var acao = async () => await _ambiente.Dispatcher.Send(new CriarContaCommand("", ""));

        var excecao = await acao.Should().ThrowAsync<ValidacaoException>();

        excecao.Which.Erros.Should().ContainKeys(
            nameof(CriarContaCommand.Nome),
            nameof(CriarContaCommand.Documento));
    }

    [Fact]
    public async Task Send_ComandoInvalido_NaoChegaAPersistirNada()
    {
        var acao = async () => await _ambiente.Dispatcher.Send(new CriarContaCommand("", ""));
        await acao.Should().ThrowAsync<ValidacaoException>();

        _ambiente.ReiniciarEscopo();
        var contas = await _ambiente.Dispatcher.Ask(new ListarContasQuery());

        contas.Should().BeEmpty("a validação roda antes de abrir a transação");
    }

    [Fact]
    public async Task Ask_ResolveQueryHandler()
    {
        await _ambiente.Dispatcher.Send(new CriarContaCommand("Empresa A", "111"));
        await _ambiente.Dispatcher.Send(new CriarContaCommand("Empresa B", "222"));
        _ambiente.ReiniciarEscopo();

        var contas = await _ambiente.Dispatcher.Ask(new ListarContasQuery());

        contas.Should().HaveCount(2);
        contas.Select(c => c.Nome).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Send_ComNulo_Rejeita()
    {
        var acao = async () => await _ambiente.Dispatcher.Send<string>(null!);

        await acao.Should().ThrowAsync<ArgumentNullException>();
    }

    public void Dispose() => _ambiente.Dispose();
}
