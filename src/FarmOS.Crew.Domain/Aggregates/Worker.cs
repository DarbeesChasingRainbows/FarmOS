using FarmOS.Crew.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain.Aggregates;

public sealed class Worker : AggregateRoot<WorkerId>
{
    public WorkerProfile Profile { get; private set; } = new("", "", null, WorkerRole.Employee, null, null, DateOnly.MinValue);
    public WorkerStatus Status { get; private set; }
    private readonly List<Certification> _certifications = [];
    public IReadOnlyList<Certification> Certifications => _certifications;

    public static Worker Register(WorkerProfile profile)
    {
        var worker = new Worker();
        worker.RaiseEvent(new WorkerRegistered(WorkerId.New(), profile, DateTimeOffset.UtcNow));
        return worker;
    }

    public void UpdateProfile(WorkerProfile profile) =>
        RaiseEvent(new WorkerProfileUpdated(Id, profile, DateTimeOffset.UtcNow));

    public Result<WorkerId, DomainError> Deactivate(WorkerStatus newStatus, string? reason)
    {
        if (Status != WorkerStatus.Active)
            return DomainError.Conflict("Only active workers can be deactivated.");
        if (newStatus == WorkerStatus.Active)
            return DomainError.Validation("Deactivation status cannot be Active.");
        RaiseEvent(new WorkerDeactivated(Id, newStatus, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<WorkerId, DomainError> AddCertification(Certification cert)
    {
        if (Status != WorkerStatus.Active)
            return DomainError.Conflict("Cannot add certifications to inactive workers.");
        RaiseEvent(new CertificationAdded(Id, cert, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case WorkerRegistered e: Id = e.Id; Profile = e.Profile; Status = WorkerStatus.Active; break;
            case WorkerProfileUpdated e: Profile = e.Profile; break;
            case WorkerDeactivated e: Status = e.NewStatus; break;
            case CertificationAdded e: _certifications.Add(e.Cert); break;
        }
    }
}
