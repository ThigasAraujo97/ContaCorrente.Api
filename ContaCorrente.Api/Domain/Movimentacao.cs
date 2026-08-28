namespace ContaCorrente.Api.Domain;

/// <summary>
/// Lançamento imutável no extrato da conta. Uma vez registrada, uma movimentação
/// nunca é alterada nem removida — o extrato é um log append-only.
/// </summary>
public class Movimentacao
{
    // Construtor sem parâmetros exigido pelo EF Core para materializar a entidade.
    private Movimentacao()
    {
        Descricao = null;
    }

    private Movimentacao(
        Guid contaId,
        TipoMovimentacao tipo,
        decimal valor,
        decimal saldoResultante,
        string? descricao,
        FormaPagamento? formaPagamento)
    {
        Id = Guid.NewGuid();
        ContaId = contaId;
        Tipo = tipo;
        Valor = valor;
        SaldoResultante = saldoResultante;
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        FormaPagamento = formaPagamento;
        OcorridaEm = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ContaId { get; private set; }

    public TipoMovimentacao Tipo { get; private set; }

    /// <summary>Valor movimentado, sempre positivo. O sinal é dado por <see cref="Tipo"/>.</summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Saldo da conta imediatamente após este lançamento. Guardar o snapshot torna o
    /// extrato auditável e dispensa recalcular o saldo histórico a cada consulta.
    /// </summary>
    public decimal SaldoResultante { get; private set; }

    public string? Descricao { get; private set; }

    /// <summary>
    /// Meio de pagamento usado. Opcional: lançamentos anteriores à introdução do campo
    /// não têm essa informação, e não faria sentido inventar um valor para eles num
    /// extrato que é registro histórico.
    /// </summary>
    public FormaPagamento? FormaPagamento { get; private set; }

    public DateTime OcorridaEm { get; private set; }

    /// <summary>
    /// Fábrica interna: só a <see cref="Conta"/> cria movimentações, e sempre depois de
    /// aplicar a regra de negócio. Isso impede que um lançamento exista sem que o saldo
    /// correspondente tenha sido atualizado.
    /// </summary>
    internal static Movimentacao Criar(
        Guid contaId,
        TipoMovimentacao tipo,
        decimal valor,
        decimal saldoResultante,
        string? descricao,
        FormaPagamento? formaPagamento)
        => new(contaId, tipo, valor, saldoResultante, descricao, formaPagamento);
}
