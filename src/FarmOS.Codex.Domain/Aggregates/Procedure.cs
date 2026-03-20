using FarmOS.Codex.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain.Aggregates;

public sealed class Procedure : AggregateRoot<ProcedureId>
{
    public string Title { get; private set; } = string.Empty;
    public ProcedureCategory Category { get; private set; }
    public AudienceRole Audience { get; private set; }
    public string? Description { get; private set; }
    public ProcedureStatus Status { get; private set; }
    public int Revision { get; private set; }
    private readonly List<ProcedureStep> _steps = [];
    public IReadOnlyList<ProcedureStep> Steps => _steps;

    public static Procedure Create(string title, ProcedureCategory category, AudienceRole audience, string? description)
    {
        var procedure = new Procedure();
        procedure.RaiseEvent(new ProcedureCreated(ProcedureId.New(), title, category, audience, description, DateTimeOffset.UtcNow));
        return procedure;
    }

    public void AddStep(ProcedureStep step) =>
        RaiseEvent(new ProcedureStepAdded(Id, step, DateTimeOffset.UtcNow));

    public Result<ProcedureId, DomainError> Publish()
    {
        if (Status == ProcedureStatus.Archived)
            return DomainError.Conflict("Cannot publish an archived procedure.");
        if (_steps.Count == 0)
            return DomainError.Validation("Cannot publish a procedure with no steps.");
        RaiseEvent(new ProcedurePublished(Id, Revision + 1, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ProcedureId, DomainError> Revise(string? changeNotes)
    {
        if (Status != ProcedureStatus.Published)
            return DomainError.Conflict("Only published procedures can be revised.");
        RaiseEvent(new ProcedureRevised(Id, changeNotes, DateTimeOffset.UtcNow));
        return Id;
    }

    public void Archive() =>
        RaiseEvent(new ProcedureArchived(Id, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProcedureCreated e: Id = e.Id; Title = e.Title; Category = e.Category; Audience = e.Audience; Description = e.Description; Status = ProcedureStatus.Draft; break;
            case ProcedureStepAdded e: _steps.Add(e.Step); break;
            case ProcedurePublished e: Status = ProcedureStatus.Published; Revision = e.Revision; break;
            case ProcedureRevised: Status = ProcedureStatus.Draft; break;
            case ProcedureArchived: Status = ProcedureStatus.Archived; break;
        }
    }
}
