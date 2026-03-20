using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.IoT.API;
using FarmOS.IoT.API.Workers;
using FarmOS.IoT.Application;
using FarmOS.IoT.Application.Commands.Handlers;
using FarmOS.IoT.Application.Queries;
using FarmOS.IoT.Application.Queries.Handlers;
using FarmOS.IoT.Infrastructure;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;
using FarmOS.IoT.API.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

// ─── SignalR ────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── IoT Services ────────────────────────────────────────────────────
builder.Services.AddScoped<IIoTEventStore, IoTEventStore>();
builder.Services.AddScoped<IIoTProjection, IoTProjection>();

// ─── Telemetry Services ──────────────────────────────────────────────
builder.Services.AddScoped<IIoTProjectionLookup, IoTProjectionLookup>();
builder.Services.AddScoped<ITelemetryProjection, TelemetryProjection>();
builder.Services.AddSingleton<IThresholdRuleProvider, DefaultThresholdRuleProvider>();
builder.Services.AddSingleton<IAlertNotifier>(sp =>
    new LoggingAlertNotifier(
        sp.GetRequiredService<ILogger<LoggingAlertNotifier>>(),
        sp.GetRequiredService<IEventBus>()));

// ─── Notification Channel ────────────────────────────────────────
var twilioSid = builder.Configuration.GetValue<string>("TWILIO_SID") ?? "";
var twilioToken = builder.Configuration.GetValue<string>("TWILIO_AUTH_TOKEN") ?? "";
var twilioFrom = builder.Configuration.GetValue<string>("TWILIO_FROM") ?? "";
var alertPhone = builder.Configuration.GetValue<string>("ALERT_PHONE") ?? "";

if (!string.IsNullOrEmpty(twilioSid) && !string.IsNullOrEmpty(twilioToken))
{
    builder.Services.AddSingleton<INotificationChannel>(sp =>
        new TwilioSmsNotifier(twilioSid, twilioToken, twilioFrom, alertPhone,
            sp.GetRequiredService<ILogger<TwilioSmsNotifier>>()));
}
else
{
    builder.Services.AddSingleton<INotificationChannel, ConsoleNotifier>();
}

// ─── Background Workers ──────────────────────────────────────────
builder.Services.AddHostedService<HASensorPollingWorker>();
builder.Services.AddHostedService<IoTProjectorWorker>();
builder.Services.AddHostedService<NotificationWorker>();

// ─── MediatR ─────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<DeviceCommandHandlers>();
    cfg.RegisterServicesFromAssemblyContaining<TelemetryCommandHandler>();
});

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<MessagePackMiddleware>();
app.MapIoT();
app.MapTelemetry();
app.MapHub<SensorHub>("/hubs/sensors");

app.Run();
