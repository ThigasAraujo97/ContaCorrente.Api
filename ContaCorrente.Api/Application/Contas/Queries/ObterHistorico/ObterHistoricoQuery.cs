using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Domain;

namespace ContaCorrente.Api.Application.Contas.Queries.ObterHistorico;

public sealed record ObterHistoricoQuery(
    Guid ContaId,
    int Pagina = 1,
    int Tamanho = 20,
    DateTime? De = null,
    DateTime? Ate = null,
    TipoMovimentacao? Tipo = null,
    FormaPagamento? FormaPagamento = null) : IQuery<PaginaResponse<MovimentacaoResponse>>
{
    public const int TamanhoMaximoDePagina = 100;

    /// <summary>
    /// Normaliza a paginação vinda da query string, evitando página zero/negativa e
    /// impedindo que um cliente peça a tabela inteira numa requisição só.
    /// </summary>
    public ObterHistoricoQuery Normalizada() => this with
    {
        Pagina = Pagina < 1 ? 1 : Pagina,
        Tamanho = Tamanho switch
        {
            < 1 => 20,
            > TamanhoMaximoDePagina => TamanhoMaximoDePagina,
            _ => Tamanho
        }
    };
}
