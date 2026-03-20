using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.Hearth.API;
using FarmOS.Hearth.API.Hubs;
using FarmOS.Hearth.API.Workers;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Application.Commands.Handlers;
using FarmOS.Hearth.Infrastructure;
using FarmOS.SharedKernel.EventStore;
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

// ─── RabbitMQ Event Bus ─────────────────────────────────────────────
var rabbitHost = builder.Configuration.GetValue<string>("RABBITMQ_HOST") ?? "localhost";
var rabbitPort = builder.Configuration.GetValue<int?>("RABBITMQ_PORT") ?? 5672;
builder.Services.AddSingleton<IEventBus>(new RabbitMqEventBus(rabbitHost, rabbitPort));

// ─── Hearth Services ─────────────────────────────────────────────────
builder.Services.AddScoped<IHearthEventStore, HearthEventStore>();
builder.Services.AddScoped<ITraceabilityGraphService, ArangoTraceabilityGraphService>();

// ─── MediatR ────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<SourdoughCommandHandlers>());

// ─── Background Workers ──────────────────────────────────────────
builder.Services.AddHostedService<ComplianceEventSubscriber>();

// ─── Harvest Right IoT ───────────────────────────────────────────
builder.Services.Configure<FarmOS.Hearth.Infrastructure.HarvestRight.HarvestRightOptions>(
    builder.Configuration.GetSection(FarmOS.Hearth.Infrastructure.HarvestRight.HarvestRightOptions.SectionName));
builder.Services.AddHttpClient<IHarvestRightAuthClient, FarmOS.Hearth.Infrastructure.HarvestRight.HarvestRightAuthClient>();
builder.Services.AddSingleton<FarmOS.Hearth.Infrastructure.HarvestRight.HarvestRightMqttWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FarmOS.Hearth.Infrastructure.HarvestRight.HarvestRightMqttWorker>());

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<MessagePackMiddleware>();
app.MapHearthEndpoints();

app.Run();

public partial class Program { }
