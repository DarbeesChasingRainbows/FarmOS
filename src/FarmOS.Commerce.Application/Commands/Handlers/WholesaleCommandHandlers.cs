using FarmOS.Commerce.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands.Handlers;

public sealed class WholesaleCommandHandlers(ICommerceEventStore store) :
    ICommandHandler<OpenWholesaleAccountCommand, Guid>,
    ICommandHandler<SetStandingOrderCommand, Guid>,
    ICommandHandler<CancelStandingOrderCommand, Guid>,
    ICommandHandler<AssignDeliveryRouteCommand, Guid>,
    ICommandHandler<CloseWholesaleAccountCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(OpenWholesaleAccountCommand cmd, CancellationToken ct)
    {
        var account = WholesaleAccount.Open(cmd.BusinessName, cmd.ContactName, cmd.Email, cmd.Phone, cmd.OrderFrequency);
        await store.SaveWholesaleAccountAsync(account, "steward", ct);
        return account.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(SetStandingOrderCommand cmd, CancellationToken ct)
    {
        var account = await store.LoadWholesaleAccountAsync(cmd.AccountId.ToString(), ct);
        var result = account.SetStandingOrder(cmd.Order);
        if (result.IsFailure) return result.Error;
        await store.SaveWholesaleAccountAsync(account, "steward", ct);
        return account.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CancelStandingOrderCommand cmd, CancellationToken ct)
    {
        var account = await store.LoadWholesaleAccountAsync(cmd.AccountId.ToString(), ct);
        var result = account.CancelStandingOrder(cmd.ProductName);
        if (result.IsFailure) return result.Error;
        await store.SaveWholesaleAccountAsync(account, "steward", ct);
        return account.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AssignDeliveryRouteCommand cmd, CancellationToken ct)
    {
        var account = await store.LoadWholesaleAccountAsync(cmd.AccountId.ToString(), ct);
        var result = account.AssignDeliveryRoute(cmd.Route);
        if (result.IsFailure) return result.Error;
        await store.SaveWholesaleAccountAsync(account, "steward", ct);
        return account.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CloseWholesaleAccountCommand cmd, CancellationToken ct)
    {
        var account = await store.LoadWholesaleAccountAsync(cmd.AccountId.ToString(), ct);
        var result = account.Close(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveWholesaleAccountAsync(account, "steward", ct);
        return account.Id.Value;
    }
}
