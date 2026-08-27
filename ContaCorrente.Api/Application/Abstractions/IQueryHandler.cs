namespace ContaCorrente.Api.Application.Abstractions;

/// <summary>
/// Executa uma consulta. Por convenção, lê com AsNoTracking e projeta direto no DTO.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}
