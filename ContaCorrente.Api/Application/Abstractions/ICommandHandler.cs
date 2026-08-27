namespace ContaCorrente.Api.Application.Abstractions;

/// <summary>
/// Executa um comando. O handler orquestra — a regra de negócio fica no domínio.
/// Não precisa chamar SaveChanges nem abrir transação: o
/// <see cref="IDispatcher"/> cuida disso.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}
