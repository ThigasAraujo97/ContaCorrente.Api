namespace ContaCorrente.Api.Domain.Exceptions;

/// <summary>
/// Lançada quando a conta informada não existe.
/// </summary>
public sealed class ContaNaoEncontradaException(Guid contaId)
    : DominioException($"Conta {contaId} não encontrada.")
{
    public Guid ContaId { get; } = contaId;
}
