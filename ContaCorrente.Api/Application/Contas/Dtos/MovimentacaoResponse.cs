using ContaCorrente.Api.Domain;

namespace ContaCorrente.Api.Application.Contas.Dtos;

public sealed record MovimentacaoResponse(
    Guid Id,
    Guid ContaId,
    TipoMovimentacao Tipo,
    decimal Valor,
    decimal SaldoResultante,
    string? Descricao,
    DateTime OcorridaEm)
{
    public static MovimentacaoResponse De(Movimentacao movimentacao)
        => new(
            movimentacao.Id,
            movimentacao.ContaId,
            movimentacao.Tipo,
            movimentacao.Valor,
            movimentacao.SaldoResultante,
            movimentacao.Descricao,
            movimentacao.OcorridaEm);
}
