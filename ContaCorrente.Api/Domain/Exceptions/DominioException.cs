namespace ContaCorrente.Api.Domain.Exceptions;

/// <summary>
/// Base para as violações de regra de negócio do domínio.
/// Serve de ponto único para o handler de exceções da API mapear em respostas HTTP.
/// </summary>
public abstract class DominioException(string mensagem) : Exception(mensagem);
