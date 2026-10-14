using Scalex.AppCore;
using Scalex.Dispatcher;
using Scalex.Maintenance;
using Scalex.Provisioner;

namespace Scalex.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.SetupAppCoreDi();
        builder.Services.SetupDispatcherDi();
        builder.Services.SetupProvisionerDi();
        builder.Services.SetupMaintenanceDi();

        var app = builder.Build();

        app.MapGet(Constants.HealthRoute, () => Results.Ok());

        app.Run();
    }
}
