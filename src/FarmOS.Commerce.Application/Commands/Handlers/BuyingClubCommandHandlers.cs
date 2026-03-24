using FarmOS.Commerce.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands.Handlers;

public sealed class BuyingClubCommandHandlers(ICommerceEventStore store) :
    ICommandHandler<CreateBuyingClubCommand, Guid>,
    ICommandHandler<AddDropSiteCommand, Guid>,
    ICommandHandler<RemoveDropSiteCommand, Guid>,
    ICommandHandler<OpenOrderCycleCommand, Guid>,
    ICommandHandler<CloseOrderCycleCommand, Guid>,
    ICommandHandler<PauseBuyingClubCommand, Guid>,
    ICommandHandler<CloseBuyingClubCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateBuyingClubCommand cmd, CancellationToken ct)
    {
        var club = BuyingClub.Create(cmd.Name, cmd.Description, cmd.Frequency);
        await store.SaveBuyingClubAsync(club, "steward", ct);
        return club.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddDropSiteCommand cmd, CancellationToken ct)
    {
        var club = await store.LoadBuyingClubAsync(cmd.ClubId.ToString(), ct);
        var result = club.AddDropSite(cmd.Site);
        if (result.IsFailure) return result.Error;
        await store.SaveBuyingClubAsync(club, "steward", ct);
        return club.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RemoveDropSiteCommand cmd, CancellationToken ct)
    {
        var club = await store.LoadBuyingClubAsync(cmd.ClubId.ToString(), ct);
        var result = club.RemoveDropSite(cmd.SiteName);
        if (result.IsFailure) return result.Error;
        await store.SaveBuyingClubAsync(club, "steward", ct);
        return club.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(OpenOrderCycleCommand cmd, CancellationToken ct)
    {
        var club = await store.LoadBuyingClubAsync(cmd.ClubId.ToString(), ct);
        var result = club.OpenCycle(cmd.CycleDate);
        if (result.IsFailure) return result.Error;
        await store.SaveBuyingClubAsync(club, "steward", ct);
        return club.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CloseOrderCycleCommand cmd, CancellationToken ct)
    {
        var club = await store.LoadBuyingClubAsync(cmd.ClubId.ToString(), ct);
        var result = club.CloseCycle(cmd.CycleDate);
        if (result.IsFailure) return result.Error;
        await store.SaveBuyingClubAsync(club, "steward", ct);
        return club.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(PauseBuyingClubCommand cmd, CancellationToken ct)
    {
        var club = await store.LoadBuyingClubAsync(cmd.ClubId.ToString(), ct);
        club.Pause(cmd.Reason);
        await store.SaveBuyingClubAsync(club, "steward", ct);
        return club.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CloseBuyingClubCommand cmd, CancellationToken ct)
    {
        var club = await store.LoadBuyingClubAsync(cmd.ClubId.ToString(), ct);
        var result = club.Close(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveBuyingClubAsync(club, "steward", ct);
        return club.Id.Value;
    }
}
