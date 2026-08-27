using ContaCorrente.Api.Domain;

namespace ContaCorrente.Api.Application.Contas.Dtos;

public sealed record ContaResponse(
    Guid Id,
    string Nome,
    string Documento,
    decimal Saldo,
    DateTime CriadaEm)
{
    public static ContaResponse De(Conta conta)
        => new(conta.Id, conta.Nome, conta.Documento, conta.Saldo, conta.CriadaEm);
}
