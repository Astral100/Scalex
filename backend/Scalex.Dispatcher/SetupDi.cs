using Microsoft.Extensions.DependencyInjection;

namespace Scalex.Dispatcher;

public static class SetupDi
{
    /// <summary>Registers everything Scalex.Dispatcher owns: the dispatch seam (ADR 0003) and its pool implementation.</summary>
    public static IServiceCollection SetupDispatcherDi(this IServiceCollection services)
    {
        return services;
    }
}
