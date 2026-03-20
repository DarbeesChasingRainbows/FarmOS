using ArangoDBNetStandard;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Application.Commands.Handlers;
using FarmOS.SharedKernel.EventStore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FarmOS.Hearth.API.Tests;

public class HearthWebApplicationFactory : WebApplicationFactory<Program>
{
    public IHearthEventStore MockHearthEventStore { get; } = Substitute.For<IHearthEventStore>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real ArangoDB registrations
            RemoveService<IArangoDBClient>(services);
            RemoveService<IEventStore>(services);
            RemoveService<IHearthEventStore>(services);

            // Replace with mocks
            services.AddSingleton(Substitute.For<IArangoDBClient>());
            services.AddSingleton(Substitute.For<IEventStore>());
            services.AddScoped(_ => MockHearthEventStore);

            // Mock IKitchenHubNotifier (required by IoTCommandHandlers)
            services.AddSingleton(Substitute.For<IKitchenHubNotifier>());

            // Add CORS (required by UseCors middleware)
            services.AddCors();

            // Add SignalR (required by MapHub<KitchenHub>)
            services.AddSignalR();

            // Add MediatR (required by endpoint handlers)
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<LogReceivingEventHandler>());
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);
    }
}
