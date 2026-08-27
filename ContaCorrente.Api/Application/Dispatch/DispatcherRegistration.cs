using System.Reflection;
using ContaCorrente.Api.Application.Abstractions;
using FluentValidation;

namespace ContaCorrente.Api.Application.Dispatch;

public static class DispatcherRegistration
{
    /// <summary>
    /// Registra o dispatcher, todos os handlers de comando/consulta e os validators
    /// encontrados no assembly. Handler novo passa a funcionar só por existir — sem
    /// precisar editar o Program.cs.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DispatcherRegistration).Assembly;

        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        RegistrarHandlers(services, assembly, typeof(ICommandHandler<,>));
        RegistrarHandlers(services, assembly, typeof(IQueryHandler<,>));

        return services;
    }

    private static void RegistrarHandlers(
        IServiceCollection services,
        Assembly assembly,
        Type interfaceAberta)
    {
        var implementacoes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var implementacao in implementacoes)
        {
            var interfacesFechadas = implementacao.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceAberta);

            foreach (var interfaceFechada in interfacesFechadas)
            {
                services.AddScoped(interfaceFechada, implementacao);
            }
        }
    }
}
