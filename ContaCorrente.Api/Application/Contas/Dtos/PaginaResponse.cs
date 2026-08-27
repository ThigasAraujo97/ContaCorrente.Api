namespace ContaCorrente.Api.Application.Contas.Dtos;

/// <summary>
/// Envelope de paginação. O extrato de uma conta cresce indefinidamente, então a
/// consulta de histórico nunca devolve a coleção inteira.
/// </summary>
public sealed record PaginaResponse<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int Tamanho,
    int TotalDeItens)
{
    public int TotalDePaginas => Tamanho == 0 ? 0 : (int)Math.Ceiling(TotalDeItens / (double)Tamanho);

    public bool TemProximaPagina => Pagina < TotalDePaginas;
}
