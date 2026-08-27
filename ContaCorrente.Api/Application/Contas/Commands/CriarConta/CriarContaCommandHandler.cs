using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Domain;
using ContaCorrente.Api.Infrastructure;

namespace ContaCorrente.Api.Application.Contas.Commands.CriarConta;

public sealed class CriarContaCommandHandler(ContaCorrenteDbContext db)
    : ICommandHandler<CriarContaCommand, ContaResponse>
{
    public async Task<ContaResponse> Handle(
        CriarContaCommand command,
        CancellationToken cancellationToken)
    {
        var conta = new Conta(command.Nome, command.Documento);

        await db.Contas.AddAsync(conta, cancellationToken);

        // SaveChanges e commit ficam por conta do Dispatcher.
        return ContaResponse.De(conta);
    }
}
