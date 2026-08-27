namespace ContaCorrente.Api.Domain.Exceptions;

/// <summary>
/// Lançada quando um débito excede o saldo disponível da conta.
/// É a exceção que garante o requisito de nunca permitir saldo negativo.
/// </summary>
public sealed class SaldoInsuficienteException(decimal saldoDisponivel, decimal valorSolicitado)
    : DominioException($"Saldo insuficiente. Disponível: {saldoDisponivel:F2}, solicitado: {valorSolicitado:F2}.")
{
    public decimal SaldoDisponivel { get; } = saldoDisponivel;

    public decimal ValorSolicitado { get; } = valorSolicitado;
}
