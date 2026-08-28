using ContaCorrente.Api.Domain;

namespace ContaCorrente.Api.Api.Requests;

/// <summary>
/// Corpo de POST /api/contas/{id}/movimentacoes.
/// O <c>ContaId</c> não aparece aqui: vem da rota.
/// </summary>
public sealed record MovimentarRequest(
    TipoMovimentacao Tipo,
    decimal Valor,
    string? Descricao,
    FormaPagamento? FormaPagamento = null);
