using FarmOS.Commerce.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands;

public record OpenWholesaleAccountCommand(string BusinessName, string ContactName, string Email, string? Phone, OrderCycleFrequency OrderFrequency) : ICommand<Guid>;
public record SetStandingOrderCommand(Guid AccountId, StandingOrder Order) : ICommand<Guid>;
public record CancelStandingOrderCommand(Guid AccountId, string ProductName) : ICommand<Guid>;
public record AssignDeliveryRouteCommand(Guid AccountId, DeliveryRoute Route) : ICommand<Guid>;
public record CloseWholesaleAccountCommand(Guid AccountId, string? Reason) : ICommand<Guid>;
