namespace ContaCorrente.Api.Domain;

/// <summary>
/// Meio pelo qual o dinheiro entrou ou saiu da conta.
/// <para>
/// Note que <see cref="CartaoCredito"/> e <see cref="CartaoDebito"/> não têm relação com
/// <see cref="TipoMovimentacao.Credito"/> e <see cref="TipoMovimentacao.Debito"/>: lá o
/// sentido é contábil (entrada ou saída de valor), aqui é o instrumento de pagamento.
/// Os nomes são explícitos justamente para que as duas dimensões não se confundam.
/// </para>
/// </summary>
public enum FormaPagamento
{
    Boleto = 1,
    CartaoCredito = 2,
    CartaoDebito = 3,
    Pix = 4,
    TransferenciaBancaria = 5,
    Dinheiro = 6
}
