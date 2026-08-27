namespace ContaCorrente.Api.Application.Contas.Dtos;

public sealed record SaldoResponse(
    Guid ContaId,
    decimal Saldo,
    DateTime AtualizadoEm);
