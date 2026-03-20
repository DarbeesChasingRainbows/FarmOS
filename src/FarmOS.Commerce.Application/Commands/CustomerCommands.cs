using FarmOS.Commerce.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands;

public record CreateCustomerCommand(CustomerProfile Profile, AccountTier Tier) : ICommand<Guid>;
public record UpdateCustomerProfileCommand(Guid CustomerId, CustomerProfile Profile) : ICommand<Guid>;
public record AddCustomerNoteCommand(Guid CustomerId, string Content) : ICommand<Guid>;
public record MergeCustomersCommand(Guid SurvivingId, Guid AbsorbedId) : ICommand<Guid>;
public record DismissDuplicateCommand(Guid CustomerId, Guid DismissedMatchId) : ICommand<Guid>;
