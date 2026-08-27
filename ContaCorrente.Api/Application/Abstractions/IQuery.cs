namespace ContaCorrente.Api.Application.Abstractions;

/// <summary>
/// Consulta de leitura. Não altera estado e, por isso, não abre transação nem passa
/// pela validação de escrita.
/// </summary>
/// <typeparam name="TResult">O que a consulta devolve.</typeparam>
public interface IQuery<out TResult>;
