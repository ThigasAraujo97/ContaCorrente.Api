namespace ContaCorrente.Api.Domain.Exceptions;

/// <summary>
/// Lançada quando uma movimentação recebe um valor que não é estritamente positivo.
/// </summary>
public sealed class ValorInvalidoException(decimal valor)
    : DominioException($"O valor da movimentação deve ser maior que zero. Recebido: {valor:F2}.")
{
    public decimal Valor { get; } = valor;
}
