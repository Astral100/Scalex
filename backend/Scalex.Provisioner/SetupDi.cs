using Microsoft.Extensions.DependencyInjection;

namespace Scalex.Provisioner;

public static class SetupDi
{
    /// <summary>Registers everything Scalex.Provisioner owns: worker-pool lifecycle (ADR 0016), the only Vast API client.</summary>
    public static IServiceCollection SetupProvisionerDi(this IServiceCollection services)
    {
        return services;
    }
}
