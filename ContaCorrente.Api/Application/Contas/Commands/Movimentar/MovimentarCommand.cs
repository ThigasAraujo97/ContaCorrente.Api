using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Domain;

namespace ContaCorrente.Api.Application.Contas.Commands.Movimentar;

/// <summary>
/// Registra uma entrada ou saída de valor. Entrada e saída compartilham o mesmo comando
/// porque compartilham validação, transação e formato de resposta — o que muda é apenas
/// qual método do domínio será chamado.
/// </summary>
public sealed record MovimentarCommand(
    Guid ContaId,
    TipoMovimentacao Tipo,
    decimal Valor,
    string? Descricao) : ICommand<MovimentacaoResponse>;
