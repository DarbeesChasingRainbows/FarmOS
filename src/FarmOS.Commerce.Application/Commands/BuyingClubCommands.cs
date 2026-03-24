using FarmOS.Commerce.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands;

public record CreateBuyingClubCommand(string Name, string? Description, OrderCycleFrequency Frequency) : ICommand<Guid>;
public record AddDropSiteCommand(Guid ClubId, DropSite Site) : ICommand<Guid>;
public record RemoveDropSiteCommand(Guid ClubId, string SiteName) : ICommand<Guid>;
public record OpenOrderCycleCommand(Guid ClubId, DateOnly CycleDate) : ICommand<Guid>;
public record CloseOrderCycleCommand(Guid ClubId, DateOnly CycleDate) : ICommand<Guid>;
public record PauseBuyingClubCommand(Guid ClubId, string? Reason) : ICommand<Guid>;
public record CloseBuyingClubCommand(Guid ClubId, string? Reason) : ICommand<Guid>;
