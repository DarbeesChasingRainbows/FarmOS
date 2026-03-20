using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands.Handlers;

public sealed class CustomerCommandHandlers(ICommerceEventStore store) :
    ICommandHandler<CreateCustomerCommand, Guid>,
    ICommandHandler<UpdateCustomerProfileCommand, Guid>,
    ICommandHandler<AddCustomerNoteCommand, Guid>,
    ICommandHandler<MergeCustomersCommand, Guid>,
    ICommandHandler<DismissDuplicateCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = Customer.Create(cmd.Profile, cmd.Tier);
        await store.SaveCustomerAsync(customer, "steward", ct);
        return customer.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UpdateCustomerProfileCommand cmd, CancellationToken ct)
    {
        var customer = await store.LoadCustomerAsync(cmd.CustomerId.ToString(), ct);
        customer.UpdateProfile(cmd.Profile);
        await store.SaveCustomerAsync(customer, "steward", ct);
        return customer.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddCustomerNoteCommand cmd, CancellationToken ct)
    {
        var customer = await store.LoadCustomerAsync(cmd.CustomerId.ToString(), ct);
        customer.AddNote(cmd.Content);
        await store.SaveCustomerAsync(customer, "steward", ct);
        return customer.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MergeCustomersCommand cmd, CancellationToken ct)
    {
        var surviving = await store.LoadCustomerAsync(cmd.SurvivingId.ToString(), ct);
        var result = surviving.AbsorbCustomer(new CustomerId(cmd.AbsorbedId));
        if (result.IsFailure) return result.Error;
        await store.SaveCustomerAsync(surviving, "steward", ct);
        return surviving.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(DismissDuplicateCommand cmd, CancellationToken ct)
    {
        var customer = await store.LoadCustomerAsync(cmd.CustomerId.ToString(), ct);
        customer.DismissDuplicate(new CustomerId(cmd.DismissedMatchId));
        await store.SaveCustomerAsync(customer, "steward", ct);
        return customer.Id.Value;
    }
}
