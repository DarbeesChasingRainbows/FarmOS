using FarmOS.Crew.Domain;
using FarmOS.Crew.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Crew.Application.Commands.Handlers;

public sealed class WorkerCommandHandlers(ICrewEventStore store) :
    ICommandHandler<RegisterWorkerCommand, Guid>,
    ICommandHandler<UpdateWorkerProfileCommand, Guid>,
    ICommandHandler<DeactivateWorkerCommand, Guid>,
    ICommandHandler<AddCertificationCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterWorkerCommand cmd, CancellationToken ct)
    {
        var worker = Worker.Register(cmd.Profile);
        await store.SaveWorkerAsync(worker, "steward", ct);
        return worker.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UpdateWorkerProfileCommand cmd, CancellationToken ct)
    {
        var worker = await store.LoadWorkerAsync(cmd.WorkerId.ToString(), ct);
        worker.UpdateProfile(cmd.Profile);
        await store.SaveWorkerAsync(worker, "steward", ct);
        return worker.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(DeactivateWorkerCommand cmd, CancellationToken ct)
    {
        var worker = await store.LoadWorkerAsync(cmd.WorkerId.ToString(), ct);
        var result = worker.Deactivate(cmd.NewStatus, cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveWorkerAsync(worker, "steward", ct);
        return worker.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddCertificationCommand cmd, CancellationToken ct)
    {
        var worker = await store.LoadWorkerAsync(cmd.WorkerId.ToString(), ct);
        var result = worker.AddCertification(cmd.Cert);
        if (result.IsFailure) return result.Error;
        await store.SaveWorkerAsync(worker, "steward", ct);
        return worker.Id.Value;
    }
}

public sealed class ShiftCommandHandlers(ICrewEventStore store) :
    ICommandHandler<ScheduleShiftCommand, Guid>,
    ICommandHandler<StartShiftCommand, Guid>,
    ICommandHandler<CompleteShiftCommand, Guid>,
    ICommandHandler<CancelShiftCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(ScheduleShiftCommand cmd, CancellationToken ct)
    {
        var shift = Shift.Schedule(cmd.Entry);
        await store.SaveShiftAsync(shift, "steward", ct);
        return shift.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(StartShiftCommand cmd, CancellationToken ct)
    {
        var shift = await store.LoadShiftAsync(cmd.ShiftId.ToString(), ct);
        var result = shift.Start();
        if (result.IsFailure) return result.Error;
        await store.SaveShiftAsync(shift, "steward", ct);
        return shift.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CompleteShiftCommand cmd, CancellationToken ct)
    {
        var shift = await store.LoadShiftAsync(cmd.ShiftId.ToString(), ct);
        var result = shift.Complete(cmd.Notes);
        if (result.IsFailure) return result.Error;
        await store.SaveShiftAsync(shift, "steward", ct);
        return shift.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CancelShiftCommand cmd, CancellationToken ct)
    {
        var shift = await store.LoadShiftAsync(cmd.ShiftId.ToString(), ct);
        var result = shift.Cancel(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveShiftAsync(shift, "steward", ct);
        return shift.Id.Value;
    }
}

public sealed class ApprenticeProgramCommandHandlers(ICrewEventStore store) :
    ICommandHandler<CreateProgramCommand, Guid>,
    ICommandHandler<EnrollApprenticeCommand, Guid>,
    ICommandHandler<RotateApprenticeCommand, Guid>,
    ICommandHandler<CompleteProgramCommand, Guid>,
    ICommandHandler<CancelProgramCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateProgramCommand cmd, CancellationToken ct)
    {
        var program = ApprenticeProgram.Create(cmd.Name, cmd.Year, cmd.StartDate, cmd.EndDate);
        await store.SaveApprenticeProgramAsync(program, "steward", ct);
        return program.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(EnrollApprenticeCommand cmd, CancellationToken ct)
    {
        var program = await store.LoadApprenticeProgramAsync(cmd.ProgramId.ToString(), ct);
        var result = program.Enroll(new WorkerId(cmd.WorkerId));
        if (result.IsFailure) return result.Error;
        await store.SaveApprenticeProgramAsync(program, "steward", ct);
        return program.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RotateApprenticeCommand cmd, CancellationToken ct)
    {
        var program = await store.LoadApprenticeProgramAsync(cmd.ProgramId.ToString(), ct);
        var result = program.Rotate(new WorkerId(cmd.WorkerId), cmd.Rotation);
        if (result.IsFailure) return result.Error;
        await store.SaveApprenticeProgramAsync(program, "steward", ct);
        return program.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CompleteProgramCommand cmd, CancellationToken ct)
    {
        var program = await store.LoadApprenticeProgramAsync(cmd.ProgramId.ToString(), ct);
        var result = program.Complete();
        if (result.IsFailure) return result.Error;
        await store.SaveApprenticeProgramAsync(program, "steward", ct);
        return program.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CancelProgramCommand cmd, CancellationToken ct)
    {
        var program = await store.LoadApprenticeProgramAsync(cmd.ProgramId.ToString(), ct);
        var result = program.Cancel(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveApprenticeProgramAsync(program, "steward", ct);
        return program.Id.Value;
    }
}
