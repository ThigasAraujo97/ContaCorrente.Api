using System.Collections.Concurrent;
using ContaCorrente.Api.Application.Abstractions;
using ContaCorrente.Api.Application.Exceptions;
using ContaCorrente.Api.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContaCorrente.Api.Application.Dispatch;

/// <summary>
/// Resolve e executa handlers a partir do tipo concreto do comando/consulta.
/// <para>
/// O desafio: em <c>Send&lt;TResult&gt;(ICommand&lt;TResult&gt; command)</c> só
/// <c>TResult</c> é conhecido em compilação — o tipo do comando só aparece em runtime.
/// A solução é fechar o genérico <c>Invoker&lt;TCommand, TResult&gt;</c> por reflection
/// <b>uma única vez por tipo</b> e guardar a instância em cache; das chamadas seguintes
/// em diante é despacho virtual comum, sem custo de reflection.
/// </para>
/// </summary>
public sealed class Dispatcher(IServiceProvider serviceProvider, ContaCorrenteDbContext db)
    : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> InvokersDeComando = new();
    private static readonly ConcurrentDictionary<Type, object> InvokersDeConsulta = new();

    /// <summary>Tentativa original + 1 retry em caso de conflito de concorrência.</summary>
    private const int MaximoDeTentativas = 2;

    public async Task<TResult> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoker = (InvokerDeComando<TResult>)InvokersDeComando.GetOrAdd(
            command.GetType(),
            tipo => CriarInvoker(typeof(InvokerDeComando<,>), tipo, typeof(TResult)));

        // Passo 1 - validação. Antes da transação: comando inválido não abre transação.
        invoker.Validar(serviceProvider, command);

        // Passo 2 - transação (e passo 3, o handler, por dentro dela).
        return await ExecutarEmTransacao(
            () => invoker.Invocar(serviceProvider, command, cancellationToken),
            cancellationToken);
    }

    public Task<TResult> Ask<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoker = (InvokerDeConsulta<TResult>)InvokersDeConsulta.GetOrAdd(
            query.GetType(),
            tipo => CriarInvoker(typeof(InvokerDeConsulta<,>), tipo, typeof(TResult)));

        // Leitura não valida nem abre transação.
        return invoker.Invocar(serviceProvider, query, cancellationToken);
    }

    private static object CriarInvoker(Type invokerAberto, Type tipoDaMensagem, Type tipoDoResultado)
    {
        var fechado = invokerAberto.MakeGenericType(tipoDaMensagem, tipoDoResultado);
        return Activator.CreateInstance(fechado)!;
    }

    private async Task<TResult> ExecutarEmTransacao<TResult>(
        Func<Task<TResult>> acao,
        CancellationToken cancellationToken)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var resultado = await acao();
                await db.SaveChangesAsync(cancellationToken);
                await transacao.CommitAsync(cancellationToken);
                return resultado;
            }
            catch (DbUpdateConcurrencyException) when (tentativa < MaximoDeTentativas)
            {
                // Outra requisição movimentou a mesma conta entre a leitura e o UPDATE.
                // Descarta o estado rastreado para a próxima tentativa reler do banco.
                await transacao.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
        }
    }

    private abstract class InvokerDeComando<TResult>
    {
        public abstract void Validar(IServiceProvider serviceProvider, object command);

        public abstract Task<TResult> Invocar(
            IServiceProvider serviceProvider,
            object command,
            CancellationToken cancellationToken);
    }

    private sealed class InvokerDeComando<TCommand, TResult> : InvokerDeComando<TResult>
        where TCommand : ICommand<TResult>
    {
        public override void Validar(IServiceProvider serviceProvider, object command)
        {
            // Validator é opcional: comando sem regras de formato simplesmente não tem um.
            var validator = serviceProvider.GetService<IValidator<TCommand>>();
            if (validator is null)
            {
                return;
            }

            var resultado = validator.Validate((TCommand)command);
            if (resultado.IsValid)
            {
                return;
            }

            var erros = resultado.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidacaoException(erros);
        }

        public override Task<TResult> Invocar(
            IServiceProvider serviceProvider,
            object command,
            CancellationToken cancellationToken)
        {
            var handler = serviceProvider.GetService<ICommandHandler<TCommand, TResult>>()
                ?? throw new InvalidOperationException(
                    $"Nenhum handler registrado para o comando {typeof(TCommand).Name}. " +
                    $"Implemente ICommandHandler<{typeof(TCommand).Name}, {typeof(TResult).Name}>.");

            return handler.Handle((TCommand)command, cancellationToken);
        }
    }

    private abstract class InvokerDeConsulta<TResult>
    {
        public abstract Task<TResult> Invocar(
            IServiceProvider serviceProvider,
            object query,
            CancellationToken cancellationToken);
    }

    private sealed class InvokerDeConsulta<TQuery, TResult> : InvokerDeConsulta<TResult>
        where TQuery : IQuery<TResult>
    {
        public override Task<TResult> Invocar(
            IServiceProvider serviceProvider,
            object query,
            CancellationToken cancellationToken)
        {
            var handler = serviceProvider.GetService<IQueryHandler<TQuery, TResult>>()
                ?? throw new InvalidOperationException(
                    $"Nenhum handler registrado para a consulta {typeof(TQuery).Name}. " +
                    $"Implemente IQueryHandler<{typeof(TQuery).Name}, {typeof(TResult).Name}>.");

            return handler.Handle((TQuery)query, cancellationToken);
        }
    }
}
