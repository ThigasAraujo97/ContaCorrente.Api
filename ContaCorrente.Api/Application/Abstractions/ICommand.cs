namespace ContaCorrente.Api.Application.Abstractions;

/// <summary>
/// Intenção de alterar o estado do sistema. Passa pelo pipeline de escrita do
/// <see cref="IDispatcher"/>: validação, transação e handler.
/// </summary>
/// <typeparam name="TResult">O que a operação devolve ao chamador.</typeparam>
public interface ICommand<out TResult>;
