using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;

namespace ContaCorrente.Api.Application.Contas.Queries.ListarContas;

public sealed record ListarContasQuery : IQuery<IReadOnlyList<ContaResponse>>;
