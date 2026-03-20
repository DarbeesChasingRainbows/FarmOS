using MediatR;
using FarmOS.Codex.Application.Commands;
using FarmOS.SharedKernel;

namespace FarmOS.Codex.API;

public static class CodexEndpoints
{
    public static void MapCodexEndpoints(this WebApplication app)
    {
        var procedures = app.MapGroup("/api/codex/procedures");
        var playbooks = app.MapGroup("/api/codex/playbooks");

        // --- Procedures -------------------------------------------------------

        procedures.MapPost("/", async (CreateProcedureCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/codex/procedures/{id}", new { id }), err => Results.BadRequest(err));
        });

        procedures.MapPost("/{id:guid}/steps", async (Guid id, AddProcedureStepCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ProcedureId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        procedures.MapPost("/{id:guid}/publish", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new PublishProcedureCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        procedures.MapPost("/{id:guid}/revise", async (Guid id, ReviseProcedureCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ProcedureId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        procedures.MapPost("/{id:guid}/archive", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new ArchiveProcedureCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // --- Playbooks --------------------------------------------------------

        playbooks.MapPost("/", async (CreatePlaybookCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/codex/playbooks/{id}", new { id }), err => Results.BadRequest(err));
        });

        playbooks.MapPost("/{id:guid}/tasks", async (Guid id, AddPlaybookTaskCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PlaybookId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        playbooks.MapPost("/{id:guid}/tasks/remove", async (Guid id, RemovePlaybookTaskCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PlaybookId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
