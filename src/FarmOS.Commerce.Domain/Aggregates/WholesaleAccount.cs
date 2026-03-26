using FarmOS.Commerce.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Commerce.Domain.Aggregates;

public sealed class WholesaleAccount : AggregateRoot<WholesaleAccountId>
{
    public string BusinessName { get; private set; } = "";
    public string ContactName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? Phone { get; private set; }
    public OrderCycleFrequency OrderFrequency { get; private set; }
    public bool IsClosed { get; private set; }
    private readonly List<StandingOrder> _standingOrders = [];
    public IReadOnlyList<StandingOrder> StandingOrders => _standingOrders;
    public DeliveryRoute? Route { get; private set; }

    public static WholesaleAccount Open(string businessName, string contactName, string email, string? phone, OrderCycleFrequency orderFrequency)
    {
        var account = new WholesaleAccount();
        account.RaiseEvent(new WholesaleAccountOpened(WholesaleAccountId.New(), businessName, contactName, email, phone, orderFrequency, DateTimeOffset.UtcNow));
        return account;
    }

    public Result<WholesaleAccountId, DomainError> SetStandingOrder(StandingOrder order)
    {
        if (IsClosed)
            return DomainError.Conflict("Cannot set standing order on a closed account.");
        RaiseEvent(new StandingOrderSet(Id, order, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<WholesaleAccountId, DomainError> CancelStandingOrder(string productName)
    {
        if (!_standingOrders.Any(o => o.ProductName == productName))
            return DomainError.NotFound("StandingOrder", productName);
        RaiseEvent(new StandingOrderCancelled(Id, productName, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<WholesaleAccountId, DomainError> AssignDeliveryRoute(DeliveryRoute route)
    {
        if (IsClosed)
            return DomainError.Conflict("Cannot assign route to a closed account.");
        RaiseEvent(new DeliveryRouteAssigned(Id, route, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<WholesaleAccountId, DomainError> Close(string? reason)
    {
        if (IsClosed)
            return DomainError.Conflict("Wholesale account is already closed.");
        RaiseEvent(new WholesaleAccountClosed(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case WholesaleAccountOpened e: Id = e.Id; BusinessName = e.BusinessName; ContactName = e.ContactName; Email = e.Email; Phone = e.Phone; OrderFrequency = e.OrderFrequency; break;
            case StandingOrderSet e: _standingOrders.RemoveAll(o => o.ProductName == e.Order.ProductName); _standingOrders.Add(e.Order); break;
            case StandingOrderCancelled e: _standingOrders.RemoveAll(o => o.ProductName == e.ProductName); break;
            case DeliveryRouteAssigned e: Route = e.Route; break;
            case WholesaleAccountClosed: IsClosed = true; break;
        }
    }
}
