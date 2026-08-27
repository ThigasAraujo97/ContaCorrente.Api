using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Domain.Exceptions;
using ContaCorrente.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ContaCorrente.Api.Application.Contas.Queries.ObterHistorico;

public sealed class ObterHistoricoQueryHandler(ContaCorrenteDbContext db)
    : IQueryHandler<ObterHistoricoQuery, PaginaResponse<MovimentacaoResponse>>
{
    public async Task<PaginaResponse<MovimentacaoResponse>> Handle(
        ObterHistoricoQuery query,
        CancellationToken cancellationToken)
    {
        var filtro = query.Normalizada();

        var contaExiste = await db.Contas
            .AsNoTracking()
            .AnyAsync(c => c.Id == filtro.ContaId, cancellationToken);

        if (!contaExiste)
        {
            throw new ContaNaoEncontradaException(filtro.ContaId);
        }

        var consulta = db.Movimentacoes
            .AsNoTracking()
            .Where(m => m.ContaId == filtro.ContaId);

        if (filtro.De is { } de)
        {
            consulta = consulta.Where(m => m.OcorridaEm >= de);
        }

        if (filtro.Ate is { } ate)
        {
            consulta = consulta.Where(m => m.OcorridaEm <= ate);
        }

        if (filtro.Tipo is { } tipo)
        {
            consulta = consulta.Where(m => m.Tipo == tipo);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var itens = await consulta
            .OrderByDescending(m => m.OcorridaEm)
            .ThenByDescending(m => m.Id)
            .Skip((filtro.Pagina - 1) * filtro.Tamanho)
            .Take(filtro.Tamanho)
            .Select(m => new MovimentacaoResponse(
                m.Id,
                m.ContaId,
                m.Tipo,
                m.Valor,
                m.SaldoResultante,
                m.Descricao,
                m.OcorridaEm))
            .ToListAsync(cancellationToken);

        return new PaginaResponse<MovimentacaoResponse>(itens, filtro.Pagina, filtro.Tamanho, total);
    }
}
