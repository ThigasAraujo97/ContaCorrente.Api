namespace ContaCorrente.Api.Application.Abstractions;

/// <summary>
/// Ponto único de entrada da camada de aplicação. Os controllers dependem apenas
/// desta interface — não conhecem handlers, EF Core nem transações.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Executa um comando: valida, abre transação, chama o handler, salva e commita.
    /// </summary>
    Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa uma consulta, sem validação e sem transação.
    /// </summary>
    Task<TResult> Ask<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
