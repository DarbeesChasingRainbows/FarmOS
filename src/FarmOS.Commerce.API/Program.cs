using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.Commerce.API;
using FarmOS.Commerce.Application;
using FarmOS.Commerce.Application.Commands.Handlers;
using FarmOS.Commerce.Infrastructure;
using FarmOS.SharedKernel.EventStore;
using FarmOS.Commerce.Infrastructure.Projectors;
using FarmOS.SharedKernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── ArangoDB ────────────────────────────────────────────────────────
var arangoUrl = builder.Configuration.GetValue<string>("ArangoDB:Url") ?? "http://localhost:8529";
var arangoUser = builder.Configuration.GetValue<string>("ArangoDB:User") ?? "root";
var arangoPass = builder.Configuration.GetValue<string>("ArangoDB:Password") ?? "farmos_dev";
var arangoDb = builder.Configuration.GetValue<string>("ArangoDB:Database") ?? "farmos";

builder.Services.AddSingleton<IArangoDBClient>(_ =>
{
    var transport = HttpApiTransport.UsingBasicAuth(
        new Uri(arangoUrl), arangoDb, arangoUser, arangoPass);
    return new ArangoDBClient(transport);
});

builder.Services.AddSingleton<IEventStore>(sp =>
    new ArangoEventStore(sp.GetRequiredService<IArangoDBClient>(), arangoDb));

// ─── Commerce Services ───────────────────────────────────────────────
builder.Services.AddScoped<ICommerceEventStore, CommerceEventStore>();
builder.Services.AddSingleton<InventoryQueryService>();
builder.Services.AddHostedService<InventoryProjectorWorker>();

// ─── MediatR ─────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<OrderCommandHandlers>();
});

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<MessagePackMiddleware>();
app.MapCommerceEndpoints();
app.MapInventoryEndpoints();

app.Run();
