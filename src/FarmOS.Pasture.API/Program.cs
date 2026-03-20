using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.Pasture.API;
using FarmOS.Pasture.Application;
using FarmOS.Pasture.Application.Commands.Handlers;
using FarmOS.Pasture.Infrastructure;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── ArangoDB ────────────────────────────────────────────────────────
var arangoUrl = builder.Configuration.GetValue<string>("ArangoDB:Url") ?? "http://localhost:8529";
var arangoUser = builder.Configuration.GetValue<string>("ArangoDB:User") ?? "root";
var arangoPass = builder.Configuration.GetValue<string>("ArangoDB:Password") ?? "";
var arangoDb = builder.Configuration.GetValue<string>("ArangoDB:Database") ?? "farmos";

builder.Services.AddSingleton<IArangoDBClient>(_ =>
{
    var transport = HttpApiTransport.UsingBasicAuth(
        new Uri(arangoUrl), arangoDb, arangoUser, arangoPass);
    return new ArangoDBClient(transport);
});

builder.Services.AddSingleton<IEventStore>(sp =>
    new ArangoEventStore(sp.GetRequiredService<IArangoDBClient>(), arangoDb));

// ─── Pasture Services ────────────────────────────────────────────────
builder.Services.AddScoped<IPastureEventStore, PastureEventStore>();
builder.Services.AddHostedService<FarmOS.Pasture.Infrastructure.Projectors.PastureProjectorWorker>();

// ─── MediatR ─────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<PaddockCommandHandlers>();
    cfg.RegisterServicesFromAssembly(typeof(FarmOS.Pasture.Infrastructure.QueryHandlers.PaddockQueryHandlers).Assembly);
});

// ─── CORS (for Deno frontends on different ports) ────────────────────
builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<MessagePackMiddleware>();
app.MapPastureEndpoints();

app.Run();
