using ContaCorrente.Api.Domain;

namespace ContaCorrente.Api.Api.Requests;

/// <summary>
/// Parâmetros de consulta de GET /api/contas/{id}/movimentacoes.
/// <para>
/// Agrupar os filtros num objeto, em vez de listar um <c>[FromQuery]</c> por parâmetro,
/// mantém a assinatura da action legível e faz com que adicionar um filtro novo seja uma
/// propriedade aqui — sem tocar no controller. O binder do ASP.NET Core preenche as
/// propriedades pelo nome do parâmetro na query string.
/// </para>
/// </summary>
public sealed class ObterHistoricoRequest
{
    /// <summary>Página desejada, começando em 1.</summary>
    public int Pagina { get; init; } = 1;

    /// <summary>Itens por página. Limitado no servidor para não devolver a tabela inteira.</summary>
    public int Tamanho { get; init; } = 20;

    /// <summary>Início do período (inclusive), em UTC.</summary>
    public DateTime? De { get; init; }

    /// <summary>Fim do período (inclusive), em UTC.</summary>
    public DateTime? Ate { get; init; }

    /// <summary>Filtra por entrada (<c>Credito</c>) ou saída (<c>Debito</c>).</summary>
    public TipoMovimentacao? Tipo { get; init; }

    /// <summary>Filtra pelo meio de pagamento: Boleto, CartaoCredito, CartaoDebito, Pix...</summary>
    public FormaPagamento? FormaPagamento { get; init; }
}
