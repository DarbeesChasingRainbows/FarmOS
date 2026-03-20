using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain.Events;

// ─── Worker ─────────────────────────────────────────────────────────
public record WorkerRegistered(WorkerId Id, WorkerProfile Profile, DateTimeOffset OccurredAt) : IDomainEvent;
public record WorkerProfileUpdated(WorkerId Id, WorkerProfile Profile, DateTimeOffset OccurredAt) : IDomainEvent;
public record WorkerDeactivated(WorkerId Id, WorkerStatus NewStatus, string? Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record CertificationAdded(WorkerId Id, Certification Cert, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Shift ──────────────────────────────────────────────────────────
public record ShiftScheduled(ShiftId Id, ShiftEntry Entry, DateTimeOffset OccurredAt) : IDomainEvent;
public record ShiftStarted(ShiftId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record ShiftCompleted(ShiftId Id, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;
public record ShiftCancelled(ShiftId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── ApprenticeProgram ─────────────────────────────────────────────
public record ProgramCreated(ApprenticeProgramId Id, string Name, int Year, DateOnly StartDate, DateOnly EndDate, DateTimeOffset OccurredAt) : IDomainEvent;
public record ApprenticeEnrolled(ApprenticeProgramId Id, WorkerId WorkerId, DateTimeOffset OccurredAt) : IDomainEvent;
public record ApprenticeRotated(ApprenticeProgramId Id, WorkerId WorkerId, RotationAssignment Rotation, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProgramCompleted(ApprenticeProgramId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProgramCancelled(ApprenticeProgramId Id, string? Reason, DateTimeOffset OccurredAt) : IDomainEvent;
