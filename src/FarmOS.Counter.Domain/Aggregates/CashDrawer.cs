using FarmOS.Counter.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Counter.Domain.Aggregates;

public sealed class CashDrawer : AggregateRoot<CashDrawerId>
{
    public RegisterId RegisterId { get; private set; } = new(Guid.Empty);
    public decimal StartingCash { get; private set; }
    public bool IsReconciled { get; private set; }
    public DrawerCount? LastCount { get; private set; }
    public decimal Discrepancy { get; private set; }

    public static CashDrawer Open(RegisterId registerId, decimal startingCash)
    {
        var drawer = new CashDrawer();
        drawer.RaiseEvent(new CashDrawerOpened(CashDrawerId.New(), registerId, startingCash, DateTimeOffset.UtcNow));
        return drawer;
    }

    public void Count(DrawerCount count) =>
        RaiseEvent(new CashDrawerCounted(Id, count, DateTimeOffset.UtcNow));

    public Result<CashDrawerId, DomainError> Reconcile()
    {
        if (LastCount is null)
            return DomainError.Validation("Cannot reconcile without a count.");
        var discrepancy = LastCount.Actual - LastCount.Expected;
        RaiseEvent(new CashDrawerReconciled(Id, discrepancy, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CashDrawerOpened e: Id = e.Id; RegisterId = e.RegisterId; StartingCash = e.StartingCash; break;
            case CashDrawerCounted e: LastCount = e.Count; break;
            case CashDrawerReconciled e: IsReconciled = true; Discrepancy = e.Discrepancy; break;
        }
    }
}
