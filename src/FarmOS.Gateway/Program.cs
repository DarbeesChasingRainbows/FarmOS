using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.SharedKernel.Auth;
using FarmOS.SharedKernel.Infrastructure;

// ─── Auth-only API (YARP removed — routing handled by Caddy) ─────────────────

var builder = WebApplication.CreateBuilder(args);

var arangoUrl  = builder.Configuration.GetValue<string>("ArangoDB:Url")      ?? "http://localhost:8529";
var arangoUser = builder.Configuration.GetValue<string>("ArangoDB:User")      ?? "root";
var arangoPass = builder.Configuration.GetValue<string>("ArangoDB:Password")  ?? "farmos_dev";
var arangoDb   = builder.Configuration.GetValue<string>("ArangoDB:Database")  ?? "farmos";

builder.Services.AddSingleton<IArangoDBClient>(_ =>
{
    var transport = HttpApiTransport.UsingBasicAuth(
        new Uri(arangoUrl), arangoDb, arangoUser, arangoPass);
    return new ArangoDBClient(transport);
});

builder.Services.AddSingleton<IAuthService, ArangoAuthService>();

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();

// ─── Auth Endpoints ──────────────────────────────────────────────────────────

app.MapPost("/api/auth/login", async (LoginRequest req, IAuthService auth, CancellationToken ct) =>
{
    var user = await auth.AuthenticateByPinAsync(req.Pin, ct);
    if (user is null) return Results.Unauthorized();

    return Results.Ok(new
    {
        user.Id,
        user.Name,
        user.Role,
        Token = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                $"{user.Id}:{user.Role}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"))
    });
});

app.MapGet("/api/auth/whoami", (HttpRequest req) =>
{
    var token = req.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    try
    {
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var parts   = decoded.Split(':');
        return Results.Ok(new { Id = parts[0], Role = parts[1] });
    }
    catch
    {
        return Results.Unauthorized();
    }
});

app.Run();

record LoginRequest(string Pin);
