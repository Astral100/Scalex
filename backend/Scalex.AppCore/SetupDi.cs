using Microsoft.Extensions.DependencyInjection;

namespace Scalex.AppCore;

public static class SetupDi
{
    /// <summary>Registers everything Scalex.AppCore owns: entities, state machine, policies, shared operations.</summary>
    public static IServiceCollection SetupAppCoreDi(this IServiceCollection services)
    {
        return services;
    }
}
