using ContaCorrente.Api.Api.Requests;
using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Commands.CriarConta;
using ContaCorrente.Api.Application.Contas.Commands.Movimentar;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Application.Contas.Queries.ListarContas;
using ContaCorrente.Api.Application.Contas.Queries.ObterHistorico;
using ContaCorrente.Api.Application.Contas.Queries.ObterSaldo;
using ContaCorrente.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ContaCorrente.Api.Api.Controllers;

/// <summary>
/// Borda HTTP. Cada action traduz a requisição em comando ou consulta, delega ao
/// dispatcher e devolve o resultado — sem regra de negócio e sem acesso a dados.
/// </summary>
[ApiController]
[Route("api/contas")]
[Produces("application/json")]
public sealed class ContasController(IDispatcher dispatcher) : ControllerBase
{
    /// <summary>Cria uma conta empresarial com saldo inicial zero.</summary>
    [HttpPost]
    [ProducesResponseType<ContaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContaResponse>> Criar(
        [FromBody] CriarContaRequest request,
        CancellationToken cancellationToken)
    {
        var conta = await dispatcher.Send(
            new CriarContaCommand(request.Nome, request.Documento),
            cancellationToken);

        return CreatedAtAction(nameof(ObterSaldo), new { contaId = conta.Id }, conta);
    }

    /// <summary>Lista as contas cadastradas.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ContaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContaResponse>>> Listar(
        CancellationToken cancellationToken)
        => Ok(await dispatcher.Ask(new ListarContasQuery(), cancellationToken));

    /// <summary>Consulta o saldo disponível de uma conta.</summary>
    [HttpGet("{contaId:guid}/saldo")]
    [ProducesResponseType<SaldoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaldoResponse>> ObterSaldo(
        Guid contaId,
        CancellationToken cancellationToken)
        => Ok(await dispatcher.Ask(new ObterSaldoQuery(contaId), cancellationToken));

    /// <summary>
    /// Registra uma entrada (Credito) ou saída (Debito) de valor.
    /// Uma saída maior que o saldo disponível é recusada com 422.
    /// </summary>
    [HttpPost("{contaId:guid}/movimentacoes")]
    [ProducesResponseType<MovimentacaoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MovimentacaoResponse>> Movimentar(
        Guid contaId,
        [FromBody] MovimentarRequest request,
        CancellationToken cancellationToken)
    {
        var movimentacao = await dispatcher.Send(
            new MovimentarCommand(
                contaId,
                request.Tipo,
                request.Valor,
                request.Descricao,
                request.FormaPagamento),
            cancellationToken);

        return CreatedAtAction(nameof(ObterHistorico), new { contaId }, movimentacao);
    }

    /// <summary>
    /// Consulta o histórico de movimentações, da mais recente para a mais antiga.
    /// Aceita filtros de período, tipo (entrada/saída) e forma de pagamento.
    /// </summary>
    [HttpGet("{contaId:guid}/movimentacoes")]
    [ProducesResponseType<PaginaResponse<MovimentacaoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginaResponse<MovimentacaoResponse>>> ObterHistorico(
        Guid contaId,
        [FromQuery] ObterHistoricoRequest filtro,
        CancellationToken cancellationToken)
        => Ok(await dispatcher.Ask(
            new ObterHistoricoQuery(
                contaId,
                filtro.Pagina,
                filtro.Tamanho,
                filtro.De,
                filtro.Ate,
                filtro.Tipo,
                filtro.FormaPagamento),
            cancellationToken));
}
