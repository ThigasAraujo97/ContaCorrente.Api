namespace ContaCorrente.Api.Application.Exceptions;

/// <summary>
/// Falha de validação de um comando, agrupada por campo. O handler de exceções da API
/// converte em 400 com o dicionário de erros no corpo do ProblemDetails.
/// </summary>
public sealed class ValidacaoException(IReadOnlyDictionary<string, string[]> erros)
    : Exception("Um ou mais campos são inválidos.")
{
    public IReadOnlyDictionary<string, string[]> Erros { get; } = erros;
}
