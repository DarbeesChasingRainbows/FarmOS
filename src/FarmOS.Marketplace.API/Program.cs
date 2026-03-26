using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.Commerce.Application;
using FarmOS.Commerce.Application.Commands.Handlers;
using FarmOS.Commerce.Infrastructure;
using FarmOS.Commerce.Infrastructure.Projectors;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;
using FarmOS.Marketplace.API.Seo;
using FarmOS.Marketplace.API.Ucp;
using ModelContextProtocol.Server;

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

// ─── Commerce Services ──────────────────────────────────────────────
builder.Services.AddScoped<ICommerceEventStore, CommerceEventStore>();
builder.Services.AddSingleton<InventoryQueryService>();

// ─── MediatR (for order placement via MCP tools) ────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<OrderCommandHandlers>();
});

// ─── MCP Server ─────────────────────────────────────────────────────
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "FarmOS Marketplace",
        Version = "1.0.0"
    };
})
.WithHttpTransport()
.WithToolsFromAssembly()
.WithResourcesFromAssembly();

// ─── CORS ────────────────────────────────────────────────────────────
builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();

// ─── MCP endpoint (Streamable HTTP + SSE fallback) ──────────────────
app.MapMcp("/mcp");

// ─── UCP endpoints ──────────────────────────────────────────────────
app.MapGet("/.well-known/ucp", () => Results.Ok(FarmOS.Marketplace.API.Ucp.UcpDiscovery.GetProfile(app)));
app.MapUcpCatalogEndpoints();
app.MapUcpCheckoutEndpoints();

// ─── SEO / Structured Data ───────────────────────────────────────────
app.MapStructuredDataEndpoints();

app.Run();
