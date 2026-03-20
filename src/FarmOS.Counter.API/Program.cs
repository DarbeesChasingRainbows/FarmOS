using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.Counter.API;
using FarmOS.Counter.Application;
using FarmOS.Counter.Application.Commands.Handlers;
using FarmOS.Counter.Infrastructure;
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

// --- Counter Services ------------------------------------------------------
builder.Services.AddScoped<ICounterEventStore, CounterEventStore>();

// --- MediatR ---------------------------------------------------------------
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<RegisterCommandHandlers>();
});

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<MessagePackMiddleware>();
app.MapCounterEndpoints();

app.Run();
