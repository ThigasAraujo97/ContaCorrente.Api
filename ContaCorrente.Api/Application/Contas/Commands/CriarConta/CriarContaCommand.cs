using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;

namespace ContaCorrente.Api.Application.Contas.Commands.CriarConta;

public sealed record CriarContaCommand(string Nome, string Documento)
    : ICommand<ContaResponse>;
