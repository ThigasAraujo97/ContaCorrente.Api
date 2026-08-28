using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Contas.Dtos;
using ContaCorrente.Api.Domain;
using ContaCorrente.Api.Domain.Exceptions;
using ContaCorrente.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ContaCorrente.Api.Application.Contas.Commands.Movimentar;

public sealed class MovimentarCommandHandler(ContaCorrenteDbContext db)
    : ICommandHandler<MovimentarCommand, MovimentacaoResponse>
{
    public async Task<MovimentacaoResponse> Handle(
        MovimentarCommand command,
        CancellationToken cancellationToken)
    {
        var conta = await db.Contas
            .FirstOrDefaultAsync(c => c.Id == command.ContaId, cancellationToken)
            ?? throw new ContaNaoEncontradaException(command.ContaId);

        // Quem aceita ou recusa a movimentação é a entidade. Se o saldo for insuficiente,
        // Debitar lança SaldoInsuficienteException e a transação inteira é revertida.
        var movimentacao = command.Tipo switch
        {
            TipoMovimentacao.Credito => conta.Creditar(
                command.Valor, command.Descricao, command.FormaPagamento),
            TipoMovimentacao.Debito => conta.Debitar(
                command.Valor, command.Descricao, command.FormaPagamento),
            _ => throw new ArgumentOutOfRangeException(
                nameof(command),
                command.Tipo,
                "Tipo de movimentação não suportado.")
        };

        // A movimentação é persistida por alcance: o EF a descobre pela coleção do
        // agregado. Ver MovimentacaoConfiguration para o motivo de ValueGeneratedNever.
        return MovimentacaoResponse.De(movimentacao);
    }
}
