using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Domain.Exceptions;
using ContaCorrente.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ContaCorrente.Api.Application.Contas.Queries.ObterSaldo;

public sealed class ObterSaldoQueryHandler(ContaCorrenteDbContext db)
    : IQueryHandler<ObterSaldoQuery, SaldoResponse>
{
    public async Task<SaldoResponse> Handle(
        ObterSaldoQuery query,
        CancellationToken cancellationToken)
    {
        // Projeção direta: o SELECT traz três colunas, sem materializar a entidade Conta
        // nem tocar na tabela de movimentações.
        var saldo = await db.Contas
            .AsNoTracking()
            .Where(c => c.Id == query.ContaId)
            .Select(c => new SaldoResponse(c.Id, c.Saldo, c.AtualizadaEm))
            .FirstOrDefaultAsync(cancellationToken);

        return saldo ?? throw new ContaNaoEncontradaException(query.ContaId);
    }
}
