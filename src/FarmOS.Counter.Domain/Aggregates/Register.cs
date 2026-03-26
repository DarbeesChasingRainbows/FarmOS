using FarmOS.Counter.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Counter.Domain.Aggregates;

public sealed class Register : AggregateRoot<RegisterId>
{
    public RegisterLocation Location { get; private set; }
    public string OperatorName { get; private set; } = string.Empty;
    public RegisterStatus Status { get; private set; }

    public static Register Open(RegisterLocation location, string operatorName)
    {
        var register = new Register();
        register.RaiseEvent(new RegisterOpened(RegisterId.New(), location, operatorName, DateTimeOffset.UtcNow));
        return register;
    }

    public Result<RegisterId, DomainError> Close()
    {
        if (Status == RegisterStatus.Closed)
            return DomainError.Conflict("Register is already closed.");
        RaiseEvent(new RegisterClosed(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case RegisterOpened e: Id = e.Id; Location = e.Location; OperatorName = e.OperatorName; Status = RegisterStatus.Open; break;
            case RegisterClosed: Status = RegisterStatus.Closed; break;
        }
    }
}
