using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ContaCorrente.Api.Application.Contas.Queries.ListarContas;

public sealed class ListarContasQueryHandler(ContaCorrenteDbContext db)
    : IQueryHandler<ListarContasQuery, IReadOnlyList<ContaResponse>>
{
    public async Task<IReadOnlyList<ContaResponse>> Handle(
        ListarContasQuery query,
        CancellationToken cancellationToken)
        => await db.Contas
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new ContaResponse(c.Id, c.Nome, c.Documento, c.Saldo, c.CriadaEm))
            .ToListAsync(cancellationToken);
}
