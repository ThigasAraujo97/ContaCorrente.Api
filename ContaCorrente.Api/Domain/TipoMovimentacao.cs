namespace ContaCorrente.Api.Domain;

/// <summary>
/// Natureza de uma movimentação na conta.
/// </summary>
public enum TipoMovimentacao
{
    /// <summary>Entrada de valor (aumenta o saldo).</summary>
    Credito = 1,

    /// <summary>Saída de valor (reduz o saldo).</summary>
    Debito = 2
}
