using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.Flora.API;
using FarmOS.Flora.Application;
using FarmOS.Flora.Application.Commands.Handlers;
using FarmOS.Flora.Application.Queries;
using FarmOS.Flora.Infrastructure;
using FarmOS.Flora.API.Workers;
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

// ─── RabbitMQ ────────────────────────────────────────────────────────
var rabbitHost = builder.Configuration.GetValue<string>("RABBITMQ_HOST") ?? "localhost";
var rabbitPort = builder.Configuration.GetValue<int?>("RABBITMQ_PORT") ?? 5672;
var rabbitUser = builder.Configuration.GetValue<string>("RABBITMQ_USER") ?? "guest";
var rabbitPass = builder.Configuration.GetValue<string>("RABBITMQ_PASS") ?? "guest";
builder.Services.AddSingleton<IEventBus>(new RabbitMqEventBus(rabbitHost, rabbitPort, userName: rabbitUser, password: rabbitPass));


// ─── Flora Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IFloraEventStore, FloraEventStore>();
builder.Services.AddScoped<IFloraProjection, FloraProjection>();
builder.Services.AddScoped<IFloraIntegrationPublisher, FloraIntegrationPublisher>();

// ─── Background Workers ─────────────────────────────────────────────
builder.Services.AddHostedService<FloraEventPublisher>();

// ─── MediatR ─────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<GuildCommandHandlers>();
});

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.MapFloraEndpoints();

app.Run();
