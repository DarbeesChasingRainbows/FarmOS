using FarmOS.Crew.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain.Aggregates;

public sealed class ApprenticeProgram : AggregateRoot<ApprenticeProgramId>
{
    public string Name { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public ProgramStatus Status { get; private set; }
    private readonly List<WorkerId> _enrolledWorkers = [];
    public IReadOnlyList<WorkerId> EnrolledWorkers => _enrolledWorkers;
    private readonly Dictionary<WorkerId, List<RotationAssignment>> _rotations = [];
    public IReadOnlyDictionary<WorkerId, List<RotationAssignment>> Rotations => _rotations;

    public static ApprenticeProgram Create(string name, int year, DateOnly startDate, DateOnly endDate)
    {
        var program = new ApprenticeProgram();
        program.RaiseEvent(new ProgramCreated(ApprenticeProgramId.New(), name, year, startDate, endDate, DateTimeOffset.UtcNow));
        return program;
    }

    public Result<ApprenticeProgramId, DomainError> Enroll(WorkerId workerId)
    {
        if (Status != ProgramStatus.Active)
            return DomainError.Conflict("Cannot enroll in a program that is not active.");
        if (_enrolledWorkers.Any(w => w.Value == workerId.Value))
            return DomainError.Conflict("Worker is already enrolled in this program.");
        RaiseEvent(new ApprenticeEnrolled(Id, workerId, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ApprenticeProgramId, DomainError> Rotate(WorkerId workerId, RotationAssignment rotation)
    {
        if (!_enrolledWorkers.Any(w => w.Value == workerId.Value))
            return DomainError.Validation("Worker is not enrolled in this program.");

        var durationDays = rotation.EndDate.DayNumber - rotation.StartDate.DayNumber;
        if (durationDays < 14)
            return DomainError.Validation("Rotation must be at least 2 weeks (14 days).");
        if (durationDays > 84)
            return DomainError.Validation("Rotation must be at most 12 weeks (84 days).");

        if (_rotations.TryGetValue(workerId, out var existing) && existing.Count > 0)
        {
            var lastRotation = existing[^1];
            if (lastRotation.Enterprise == rotation.Enterprise)
                return DomainError.Conflict("Cannot assign the same enterprise as the last rotation.");
        }

        RaiseEvent(new ApprenticeRotated(Id, workerId, rotation, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ApprenticeProgramId, DomainError> Complete()
    {
        if (Status != ProgramStatus.Active)
            return DomainError.Conflict("Only active programs can be completed.");
        RaiseEvent(new ProgramCompleted(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ApprenticeProgramId, DomainError> Cancel(string? reason)
    {
        if (Status is ProgramStatus.Completed or ProgramStatus.Cancelled)
            return DomainError.Conflict("Program is already completed or cancelled.");
        RaiseEvent(new ProgramCancelled(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProgramCreated e: Id = e.Id; Name = e.Name; Year = e.Year; StartDate = e.StartDate; EndDate = e.EndDate; Status = ProgramStatus.Active; break;
            case ApprenticeEnrolled e: _enrolledWorkers.Add(e.WorkerId); _rotations[e.WorkerId] = []; break;
            case ApprenticeRotated e:
                if (!_rotations.ContainsKey(e.WorkerId)) _rotations[e.WorkerId] = [];
                _rotations[e.WorkerId].Add(e.Rotation);
                break;
            case ProgramCompleted: Status = ProgramStatus.Completed; break;
            case ProgramCancelled: Status = ProgramStatus.Cancelled; break;
        }
    }
}
