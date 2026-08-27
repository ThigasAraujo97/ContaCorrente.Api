namespace ContaCorrente.Api.Api.Requests;

/// <summary>
/// Corpo de POST /api/contas. Vive na borda HTTP e é convertido em comando pelo
/// controller, para que o contrato REST possa evoluir sem alterar a camada de aplicação.
/// </summary>
public sealed record CriarContaRequest(string Nome, string Documento);
