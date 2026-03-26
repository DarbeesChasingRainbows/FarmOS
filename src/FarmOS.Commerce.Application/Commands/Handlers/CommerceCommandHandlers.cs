using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands.Handlers;

public sealed class SeasonCommandHandlers(ICommerceEventStore store) :
    ICommandHandler<CreateSeasonCommand, Guid>,
    ICommandHandler<SchedulePickupCommand, Guid>,
    ICommandHandler<CloseSeasonCommand, Guid>,
    ICommandHandler<SetSelectionModeCommand, Guid>,
    ICommandHandler<OpenSelectionWindowCommand, Guid>,
    ICommandHandler<CloseSelectionWindowCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateSeasonCommand cmd, CancellationToken ct)
    {
        var season = CSASeason.Create(cmd.Year, cmd.Name, cmd.StartDate, cmd.EndDate, cmd.Shares);
        await store.SaveSeasonAsync(season, "steward", ct);
        return season.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(SchedulePickupCommand cmd, CancellationToken ct)
    {
        var season = await store.LoadSeasonAsync(cmd.SeasonId.ToString(), ct);
        if (season.IsClosed) return DomainError.Conflict("Cannot schedule pickups for a closed season.");
        season.SchedulePickup(cmd.Pickup);
        await store.SaveSeasonAsync(season, "steward", ct);
        return season.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CloseSeasonCommand cmd, CancellationToken ct)
    {
        var season = await store.LoadSeasonAsync(cmd.SeasonId.ToString(), ct);
        season.Close();
        await store.SaveSeasonAsync(season, "steward", ct);
        return season.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(SetSelectionModeCommand cmd, CancellationToken ct)
    {
        var season = await store.LoadSeasonAsync(cmd.SeasonId.ToString(), ct);
        season.SetSelectionMode(cmd.Mode, cmd.FullShareValue, cmd.HalfShareValue);
        await store.SaveSeasonAsync(season, "steward", ct);
        return season.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(OpenSelectionWindowCommand cmd, CancellationToken ct)
    {
        var season = await store.LoadSeasonAsync(cmd.SeasonId.ToString(), ct);
        var result = season.OpenSelectionWindow(cmd.PickupDate, cmd.Deadline);
        if (result.IsFailure) return result.Error;
        await store.SaveSeasonAsync(season, "steward", ct);
        return season.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CloseSelectionWindowCommand cmd, CancellationToken ct)
    {
        var season = await store.LoadSeasonAsync(cmd.SeasonId.ToString(), ct);
        season.CloseSelectionWindow(cmd.PickupDate);
        await store.SaveSeasonAsync(season, "steward", ct);
        return season.Id.Value;
    }
}

public sealed class MemberCommandHandlers(ICommerceEventStore store) :
    ICommandHandler<RegisterMemberCommand, Guid>,
    ICommandHandler<RecordPaymentCommand, Guid>,
    ICommandHandler<RecordPickupCommand, Guid>,
    ICommandHandler<SelectItemsCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterMemberCommand cmd, CancellationToken ct)
    {
        var member = CSAMember.Register(new CSASeasonId(cmd.SeasonId), cmd.Contact, cmd.ShareType, cmd.Method);
        await store.SaveMemberAsync(member, "steward", ct);
        return member.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordPaymentCommand cmd, CancellationToken ct)
    {
        var member = await store.LoadMemberAsync(cmd.MemberId.ToString(), ct);
        member.RecordPayment(cmd.Amount, cmd.PaymentMethod, cmd.Reference);
        await store.SaveMemberAsync(member, "steward", ct);
        return member.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordPickupCommand cmd, CancellationToken ct)
    {
        var member = await store.LoadMemberAsync(cmd.MemberId.ToString(), ct);
        member.RecordPickup(cmd.PickupDate);
        await store.SaveMemberAsync(member, "steward", ct);
        return member.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(SelectItemsCommand cmd, CancellationToken ct)
    {
        var member = await store.LoadMemberAsync(cmd.MemberId.ToString(), ct);
        var result = member.SelectItems(cmd.PickupDate, cmd.Items, cmd.ShareAllowance);
        if (result.IsFailure) return result.Error;
        await store.SaveMemberAsync(member, "steward", ct);
        return member.Id.Value;
    }
}

public sealed class OrderCommandHandlers(ICommerceEventStore store) :
    ICommandHandler<CreateOrderCommand, Guid>,
    ICommandHandler<PackOrderCommand, Guid>,
    ICommandHandler<FulfillOrderCommand, Guid>,
    ICommandHandler<CancelOrderCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.CustomerName, cmd.Items, cmd.Method);
        await store.SaveOrderAsync(order, "edge-portal", ct);
        return order.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(PackOrderCommand cmd, CancellationToken ct)
    {
        var order = await store.LoadOrderAsync(cmd.OrderId.ToString(), ct);
        var result = order.Pack();
        if (result.IsFailure) return result.Error;
        await store.SaveOrderAsync(order, "steward", ct);
        return order.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(FulfillOrderCommand cmd, CancellationToken ct)
    {
        var order = await store.LoadOrderAsync(cmd.OrderId.ToString(), ct);
        var result = order.Fulfill();
        if (result.IsFailure) return result.Error;
        await store.SaveOrderAsync(order, "steward", ct);
        return order.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await store.LoadOrderAsync(cmd.OrderId.ToString(), ct);
        order.Cancel(cmd.Reason);
        await store.SaveOrderAsync(order, "steward", ct);
        return order.Id.Value;
    }
}
