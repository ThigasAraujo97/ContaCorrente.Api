using ContaCorrente.Api.Domain.Exceptions;

namespace ContaCorrente.Api.Domain;

/// <summary>
/// Conta empresarial. É a raiz de agregação: o saldo só muda através de
/// <see cref="Creditar"/> e <see cref="Debitar"/>, que aplicam as regras de negócio e
/// registram o lançamento correspondente na mesma operação.
/// </summary>
public class Conta
{
    private readonly List<Movimentacao> _movimentacoes = [];

    // Construtor sem parâmetros exigido pelo EF Core.
    private Conta()
    {
        Nome = string.Empty;
        Documento = string.Empty;
    }

    public Conta(string nome, string documento)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("Nome da conta é obrigatório.", nameof(nome));
        }

        if (string.IsNullOrWhiteSpace(documento))
        {
            throw new ArgumentException("Documento da conta é obrigatório.", nameof(documento));
        }

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Documento = documento.Trim();
        Saldo = 0m;
        CriadaEm = DateTime.UtcNow;
        AtualizadaEm = CriadaEm;
        Versao = Guid.NewGuid();
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public string Documento { get; private set; }

    /// <summary>
    /// Saldo materializado. Mantido em coluna própria (em vez de somado a cada leitura)
    /// para consulta O(1); protegido contra escrita concorrente por <see cref="Versao"/>.
    /// </summary>
    public decimal Saldo { get; private set; }

    public DateTime CriadaEm { get; private set; }

    public DateTime AtualizadaEm { get; private set; }

    /// <summary>
    /// Token de concorrência otimista. Trocado a cada movimentação; o EF Core compara o
    /// valor original no UPDATE e lança DbUpdateConcurrencyException se outra transação
    /// tiver alterado a conta no intervalo.
    /// </summary>
    public Guid Versao { get; private set; }

    public IReadOnlyCollection<Movimentacao> Movimentacoes => _movimentacoes.AsReadOnly();

    /// <summary>
    /// Registra uma entrada de valor.
    /// </summary>
    /// <exception cref="ValorInvalidoException">Se o valor não for maior que zero.</exception>
    public Movimentacao Creditar(
        decimal valor,
        string? descricao = null,
        FormaPagamento? formaPagamento = null)
    {
        GarantirValorPositivo(valor);

        Saldo += valor;
        return Registrar(TipoMovimentacao.Credito, valor, descricao, formaPagamento);
    }

    /// <summary>
    /// Registra uma saída de valor.
    /// </summary>
    /// <exception cref="ValorInvalidoException">Se o valor não for maior que zero.</exception>
    /// <exception cref="SaldoInsuficienteException">Se o valor exceder o saldo disponível.</exception>
    public Movimentacao Debitar(
        decimal valor,
        string? descricao = null,
        FormaPagamento? formaPagamento = null)
    {
        GarantirValorPositivo(valor);

        // Esta é a regra central do desafio: o saldo nunca fica negativo.
        if (valor > Saldo)
        {
            throw new SaldoInsuficienteException(Saldo, valor);
        }

        Saldo -= valor;
        return Registrar(TipoMovimentacao.Debito, valor, descricao, formaPagamento);
    }

    private Movimentacao Registrar(
        TipoMovimentacao tipo,
        decimal valor,
        string? descricao,
        FormaPagamento? formaPagamento)
    {
        var movimentacao = Movimentacao.Criar(Id, tipo, valor, Saldo, descricao, formaPagamento);
        _movimentacoes.Add(movimentacao);

        AtualizadaEm = movimentacao.OcorridaEm;
        Versao = Guid.NewGuid();

        return movimentacao;
    }

    private static void GarantirValorPositivo(decimal valor)
    {
        if (valor <= 0m)
        {
            throw new ValorInvalidoException(valor);
        }
    }
}
