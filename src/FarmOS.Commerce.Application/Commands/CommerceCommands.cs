using FarmOS.Commerce.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands;

// ─── CSA Season ──────────────────────────────────────────────────────

public record CreateSeasonCommand(int Year, string Name, DateOnly StartDate, DateOnly EndDate, IReadOnlyList<ShareDefinition> Shares) : ICommand<Guid>;
public record SchedulePickupCommand(Guid SeasonId, CSAPickup Pickup) : ICommand<Guid>;
public record CloseSeasonCommand(Guid SeasonId) : ICommand<Guid>;

// ─── CSA Member ──────────────────────────────────────────────────────

public record RegisterMemberCommand(Guid SeasonId, ContactInfo Contact, ShareSize ShareType, DeliveryMethod Method) : ICommand<Guid>;
public record RecordPaymentCommand(Guid MemberId, decimal Amount, string PaymentMethod, string? Reference) : ICommand<Guid>;
public record RecordPickupCommand(Guid MemberId, DateOnly PickupDate) : ICommand<Guid>;

// ─── Direct Orders ───────────────────────────────────────────────────

public record CreateOrderCommand(string CustomerName, IReadOnlyList<OrderItem> Items, DeliveryMethod Method) : ICommand<Guid>;
public record PackOrderCommand(Guid OrderId) : ICommand<Guid>;
public record FulfillOrderCommand(Guid OrderId) : ICommand<Guid>;
public record CancelOrderCommand(Guid OrderId, string Reason) : ICommand<Guid>;
