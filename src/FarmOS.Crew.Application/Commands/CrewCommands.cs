using FarmOS.Crew.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Crew.Application.Commands;

// --- Worker ----------------------------------------------------------------

public record RegisterWorkerCommand(WorkerProfile Profile) : ICommand<Guid>;
public record UpdateWorkerProfileCommand(Guid WorkerId, WorkerProfile Profile) : ICommand<Guid>;
public record DeactivateWorkerCommand(Guid WorkerId, WorkerStatus NewStatus, string? Reason) : ICommand<Guid>;
public record AddCertificationCommand(Guid WorkerId, Certification Cert) : ICommand<Guid>;

// --- Shift -----------------------------------------------------------------

public record ScheduleShiftCommand(ShiftEntry Entry) : ICommand<Guid>;
public record StartShiftCommand(Guid ShiftId) : ICommand<Guid>;
public record CompleteShiftCommand(Guid ShiftId, string? Notes) : ICommand<Guid>;
public record CancelShiftCommand(Guid ShiftId, string Reason) : ICommand<Guid>;

// --- ApprenticeProgram -------------------------------------------------------

public record CreateProgramCommand(string Name, int Year, DateOnly StartDate, DateOnly EndDate) : ICommand<Guid>;
public record EnrollApprenticeCommand(Guid ProgramId, Guid WorkerId) : ICommand<Guid>;
public record RotateApprenticeCommand(Guid ProgramId, Guid WorkerId, RotationAssignment Rotation) : ICommand<Guid>;
public record CompleteProgramCommand(Guid ProgramId) : ICommand<Guid>;
public record CancelProgramCommand(Guid ProgramId, string? Reason) : ICommand<Guid>;
