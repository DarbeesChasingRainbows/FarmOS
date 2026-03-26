using MediatR;
using FarmOS.Crew.Application.Commands;
using FarmOS.SharedKernel;

namespace FarmOS.Crew.API;

public static class CrewEndpoints
{
    public static void MapCrewEndpoints(this WebApplication app)
    {
        var workers = app.MapGroup("/api/crew/workers");
        var shifts = app.MapGroup("/api/crew/shifts");
        var programs = app.MapGroup("/api/crew/programs");

        // --- Workers -------------------------------------------------------

        workers.MapPost("/", async (RegisterWorkerCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/crew/workers/{id}", new { id }), err => Results.BadRequest(err));
        });

        workers.MapPut("/{id:guid}/profile", async (Guid id, UpdateWorkerProfileCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { WorkerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        workers.MapPost("/{id:guid}/deactivate", async (Guid id, DeactivateWorkerCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { WorkerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        workers.MapPost("/{id:guid}/certifications", async (Guid id, AddCertificationCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { WorkerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // --- Shifts --------------------------------------------------------

        shifts.MapPost("/", async (ScheduleShiftCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/crew/shifts/{id}", new { id }), err => Results.BadRequest(err));
        });

        shifts.MapPost("/{id:guid}/start", async (Guid id, StartShiftCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ShiftId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        shifts.MapPost("/{id:guid}/complete", async (Guid id, CompleteShiftCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ShiftId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        shifts.MapPost("/{id:guid}/cancel", async (Guid id, CancelShiftCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ShiftId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // --- Programs ----------------------------------------------------------

        programs.MapPost("/", async (CreateProgramCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/crew/programs/{id}", new { id }), err => Results.BadRequest(err));
        });

        programs.MapPost("/{id:guid}/enroll", async (Guid id, EnrollApprenticeCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ProgramId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        programs.MapPost("/{id:guid}/rotate", async (Guid id, RotateApprenticeCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ProgramId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        programs.MapPost("/{id:guid}/complete", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CompleteProgramCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        programs.MapPost("/{id:guid}/cancel", async (Guid id, CancelProgramCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ProgramId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
