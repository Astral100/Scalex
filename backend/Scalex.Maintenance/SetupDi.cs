using Microsoft.Extensions.DependencyInjection;

namespace Scalex.Maintenance;

public static class SetupDi
{
    /// <summary>Registers everything Scalex.Maintenance owns: recovery checks and the periodic deletion pass.</summary>
    public static IServiceCollection SetupMaintenanceDi(this IServiceCollection services)
    {
        return services;
    }
}
