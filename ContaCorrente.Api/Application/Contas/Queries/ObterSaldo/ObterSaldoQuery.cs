using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;

namespace ContaCorrente.Api.Application.Contas.Queries.ObterSaldo;

public sealed record ObterSaldoQuery(Guid ContaId) : IQuery<SaldoResponse>;
