using FarmOS.Commerce.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Commerce.Domain.Aggregates;

public sealed class CSASeason : AggregateRoot<CSASeasonId>
{
    public int Year { get; private set; }
    public string Name { get; private set; } = "";
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    private readonly List<ShareDefinition> _shares = [];
    public IReadOnlyList<ShareDefinition> Shares => _shares;
    private readonly List<CSAPickup> _pickups = [];
    public IReadOnlyList<CSAPickup> Pickups => _pickups;
    public bool IsClosed { get; private set; }

    public static CSASeason Create(int year, string name, DateOnly start, DateOnly end, IReadOnlyList<ShareDefinition> shares)
    {
        var season = new CSASeason();
        season.RaiseEvent(new CSASeasonCreated(CSASeasonId.New(), year, name, start, end, shares, DateTimeOffset.UtcNow));
        return season;
    }

    public void SchedulePickup(CSAPickup pickup) =>
        RaiseEvent(new CSAPickupScheduled(Id, pickup, DateTimeOffset.UtcNow));

    public void Close() => RaiseEvent(new CSASeasonClosed(Id, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CSASeasonCreated e: Id = e.Id; Year = e.Year; Name = e.Name; StartDate = e.StartDate; EndDate = e.EndDate; _shares.AddRange(e.Shares); break;
            case CSAPickupScheduled e: _pickups.Add(e.Pickup); break;
            case CSASeasonClosed: IsClosed = true; break;
        }
    }
}

public sealed class CSAMember : AggregateRoot<CSAMemberId>
{
    public CSASeasonId SeasonId { get; private set; } = new(Guid.Empty);
    public ContactInfo Contact { get; private set; } = new("", "", null, null);
    public ShareSize ShareType { get; private set; }
    public DeliveryMethod Method { get; private set; }
    public decimal TotalPaid { get; private set; }
    public int PickupsCompleted { get; private set; }

    public static CSAMember Register(CSASeasonId seasonId, ContactInfo contact, ShareSize shareType, DeliveryMethod method)
    {
        var member = new CSAMember();
        member.RaiseEvent(new CSAMemberRegistered(CSAMemberId.New(), seasonId, contact, shareType, method, DateTimeOffset.UtcNow));
        return member;
    }

    public void RecordPayment(decimal amount, string method, string? reference) =>
        RaiseEvent(new CSAPaymentRecorded(Id, amount, method, reference, DateTimeOffset.UtcNow));

    public void RecordPickup(DateOnly date) =>
        RaiseEvent(new CSASharePickedUp(Id, date, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CSAMemberRegistered e: Id = e.Id; SeasonId = e.SeasonId; Contact = e.Contact; ShareType = e.ShareType; Method = e.Method; break;
            case CSAPaymentRecorded e: TotalPaid += e.Amount; break;
            case CSASharePickedUp: PickupsCompleted++; break;
        }
    }
}

public sealed class Order : AggregateRoot<OrderId>
{
    public string CustomerName { get; private set; } = "";
    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items;
    public decimal Total => _items.Sum(i => i.Qty.Value * i.UnitPrice);
    public OrderStatus Status { get; private set; }
    public DeliveryMethod Method { get; private set; }

    public static Order Create(string customerName, IReadOnlyList<OrderItem> items, DeliveryMethod method)
    {
        var order = new Order();
        order.RaiseEvent(new OrderCreated(OrderId.New(), customerName, items, method, DateTimeOffset.UtcNow));
        return order;
    }

    public Result<OrderId, DomainError> Pack()
    {
        if (Status != OrderStatus.Confirmed) return DomainError.Conflict("Only confirmed orders can be packed.");
        RaiseEvent(new OrderPacked(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<OrderId, DomainError> Fulfill()
    {
        if (Status != OrderStatus.Packed) return DomainError.Conflict("Only packed orders can be fulfilled.");
        RaiseEvent(new OrderFulfilled(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public void Cancel(string reason) => RaiseEvent(new OrderCancelled(Id, reason, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreated e: Id = e.Id; CustomerName = e.CustomerName; _items.AddRange(e.Items); Method = e.Method; Status = OrderStatus.Confirmed; break;
            case OrderPacked: Status = OrderStatus.Packed; break;
            case OrderFulfilled: Status = Method == DeliveryMethod.Pickup ? OrderStatus.PickedUp : OrderStatus.Delivered; break;
            case OrderCancelled: Status = OrderStatus.Cancelled; break;
        }
    }
}
