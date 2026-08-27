using ContaCorrente.Api.Application.Exceptions;
using ContaCorrente.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaCorrente.Api.Api;

/// <summary>
/// Traduz exceções em respostas ProblemDetails (RFC 7807). Concentrar o mapeamento aqui
/// mantém os controllers livres de try/catch e garante um formato de erro único para o
/// front-end consumir.
/// </summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = Mapear(exception);
        if (problem is null)
        {
            // Não é um erro esperado: deixa o pipeline padrão devolver 500.
            logger.LogError(exception, "Erro não tratado ao processar {Path}.", httpContext.Request.Path);
            return false;
        }

        logger.LogInformation(
            "Requisição {Path} rejeitada com {Status}: {Titulo}",
            httpContext.Request.Path,
            problem.Status,
            problem.Title);

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private static ProblemDetails? Mapear(Exception exception) => exception switch
    {
        // Regra de negócio violada: a requisição é bem formada, mas não pode ser aceita.
        SaldoInsuficienteException e => new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Saldo insuficiente",
            Detail = e.Message,
            Extensions =
            {
                ["saldoDisponivel"] = e.SaldoDisponivel,
                ["valorSolicitado"] = e.ValorSolicitado
            }
        },

        ContaNaoEncontradaException e => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Conta não encontrada",
            Detail = e.Message
        },

        ValorInvalidoException e => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Valor inválido",
            Detail = e.Message
        },

        ValidacaoException e => new ValidationProblemDetails(
            e.Erros.ToDictionary(par => par.Key, par => par.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Falha de validação",
            Detail = e.Message
        },

        // O retry do Dispatcher já se esgotou: a conta está sob disputa intensa.
        // 409 sinaliza ao cliente que repetir a requisição tende a funcionar.
        DbUpdateConcurrencyException => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflito de concorrência",
            Detail = "A conta foi alterada por outra operação. Tente novamente."
        },

        _ => null
    };
}
