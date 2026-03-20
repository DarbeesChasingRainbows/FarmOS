using FarmOS.Commerce.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Commerce.Domain.Aggregates;

public sealed class Customer : AggregateRoot<CustomerId>
{
    public CustomerProfile Profile { get; private set; } = new("", "", null, null, [], null, null);
    public AccountTier Tier { get; private set; }
    private readonly List<CustomerNote> _notes = [];
    public IReadOnlyList<CustomerNote> Notes => _notes;
    private readonly List<CustomerId> _mergedFrom = [];
    public IReadOnlyList<CustomerId> MergedFrom => _mergedFrom;

    public static Customer Create(CustomerProfile profile, AccountTier tier)
    {
        var customer = new Customer();
        customer.RaiseEvent(new CustomerCreated(CustomerId.New(), profile, tier, DateTimeOffset.UtcNow));
        return customer;
    }

    public void UpdateProfile(CustomerProfile profile) =>
        RaiseEvent(new CustomerProfileUpdated(Id, profile, DateTimeOffset.UtcNow));

    public void AddNote(string content) =>
        RaiseEvent(new CustomerNoteAdded(Id, new CustomerNote(content, DateTimeOffset.UtcNow), DateTimeOffset.UtcNow));

    public void FlagDuplicate(MatchCandidate candidate) =>
        RaiseEvent(new DuplicateSuspected(Id, candidate, DateTimeOffset.UtcNow));

    public Result<CustomerId, DomainError> AbsorbCustomer(CustomerId absorbedId)
    {
        if (absorbedId == Id)
            return DomainError.Validation("Cannot merge customer with self.");
        RaiseEvent(new CustomersMerged(Id, absorbedId, DateTimeOffset.UtcNow));
        return Id;
    }

    public void DismissDuplicate(CustomerId dismissedId) =>
        RaiseEvent(new DuplicateDismissed(Id, dismissedId, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CustomerCreated e: Id = e.Id; Profile = e.Profile; Tier = e.Tier; break;
            case CustomerProfileUpdated e: Profile = e.Profile; break;
            case CustomerNoteAdded e: _notes.Add(e.Note); break;
            case CustomersMerged e: _mergedFrom.Add(e.AbsorbedId); break;
            default: break; // DuplicateSuspected/DuplicateDismissed are informational
        }
    }
}
