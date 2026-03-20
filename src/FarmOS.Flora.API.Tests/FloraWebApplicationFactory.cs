using ArangoDBNetStandard;
using FarmOS.Flora.Application;
using FarmOS.Flora.Application.Commands.Handlers;
using FarmOS.Flora.Application.Queries;
using FarmOS.SharedKernel.EventStore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FarmOS.Flora.API.Tests;

public class FloraWebApplicationFactory : WebApplicationFactory<Program>
{
    public IFloraEventStore MockFloraEventStore { get; } = Substitute.For<IFloraEventStore>();
    public IFloraProjection MockFloraProjection { get; } = Substitute.For<IFloraProjection>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real ArangoDB registrations
            RemoveService<IArangoDBClient>(services);
            RemoveService<IEventStore>(services);
            RemoveService<IFloraEventStore>(services);
            RemoveService<IFloraProjection>(services);

            // Replace with mocks
            services.AddSingleton(Substitute.For<IArangoDBClient>());
            services.AddSingleton(Substitute.For<IEventStore>());
            services.AddScoped(_ => MockFloraEventStore);
            services.AddScoped(_ => MockFloraProjection);

            // Add CORS (required by UseCors middleware)
            services.AddCors();

            // Add MediatR (required by endpoint handlers)
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GuildCommandHandlers>());
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);
    }
}
