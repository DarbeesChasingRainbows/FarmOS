using MediatR;
using FarmOS.Compliance.Application.Commands;
using FarmOS.SharedKernel;

namespace FarmOS.Compliance.API;

public static class ComplianceEndpoints
{
    public static void MapComplianceEndpoints(this WebApplication app)
    {
        var permits = app.MapGroup("/api/compliance/permits");
        var policies = app.MapGroup("/api/compliance/policies");

        // --- Permits -------------------------------------------------------

        permits.MapPost("/", async (RegisterPermitCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/compliance/permits/{id}", new { id }), err => Results.BadRequest(err));
        });

        permits.MapPost("/{id:guid}/renew", async (Guid id, RenewPermitCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PermitId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        permits.MapPost("/{id:guid}/revoke", async (Guid id, RevokePermitCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PermitId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        permits.MapPost("/{id:guid}/expire", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new MarkPermitExpiredCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // --- Policies ------------------------------------------------------

        policies.MapPost("/", async (RegisterPolicyCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/compliance/policies/{id}", new { id }), err => Results.BadRequest(err));
        });

        policies.MapPost("/{id:guid}/renew", async (Guid id, RenewPolicyCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PolicyId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        policies.MapPost("/{id:guid}/expire", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new MarkPolicyExpiredCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        policies.MapPut("/{id:guid}/coverages", async (Guid id, UpdateCoveragesCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PolicyId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
