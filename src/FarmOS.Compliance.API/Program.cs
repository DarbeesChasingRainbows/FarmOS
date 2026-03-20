using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.Compliance.API;
using FarmOS.Compliance.Application;
using FarmOS.Compliance.Application.Commands.Handlers;
using FarmOS.Compliance.Infrastructure;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- ArangoDB --------------------------------------------------------------
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

// --- Compliance Services ---------------------------------------------------
builder.Services.AddScoped<IComplianceEventStore, ComplianceEventStore>();

// --- MediatR ---------------------------------------------------------------
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<PermitCommandHandlers>();
});

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<MessagePackMiddleware>();
app.MapComplianceEndpoints();

app.Run();
