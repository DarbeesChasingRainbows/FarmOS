using FarmOS.Pasture.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Pasture.Domain.Aggregates;

/// <summary>
/// Herd aggregate root. A logical grouping of animals that move together.
/// Broiler tractors and eggmobiles are herd types.
/// </summary>
public sealed class Herd : AggregateRoot<HerdId>
{
    public string Name { get; private set; } = string.Empty;
    public HerdType Type { get; private set; }
    public PaddockId? CurrentPaddockId { get; private set; }
    private readonly List<AnimalId> _members = [];
    public IReadOnlyList<AnimalId> Members => _members;

    // ─── Commands ────────────────────────────────────────────────

    public static Herd Create(string name, HerdType type)
    {
        var herd = new Herd();
        herd.RaiseEvent(new HerdCreated(HerdId.New(), name, type, DateTimeOffset.UtcNow));
        return herd;
    }

    public void MoveToPaddock(PaddockId paddockId, DateOnly date)
    {
        RaiseEvent(new HerdMoved(Id, CurrentPaddockId, paddockId, date, DateTimeOffset.UtcNow));
    }

    public Result<HerdId, DomainError> AddAnimal(AnimalId animalId)
    {
        if (_members.Contains(animalId))
            return DomainError.Conflict("Animal is already a member of this herd.");

        RaiseEvent(new AnimalAddedToHerd(Id, animalId, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<HerdId, DomainError> RemoveAnimal(AnimalId animalId)
    {
        if (!_members.Contains(animalId))
            return DomainError.NotFound("Animal", animalId.ToString());

        RaiseEvent(new AnimalRemovedFromHerd(Id, animalId, DateTimeOffset.UtcNow));
        return Id;
    }

    // ─── Event Application ───────────────────────────────────────

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case HerdCreated e:
                Id = e.HerdId;
                Name = e.Name;
                Type = e.Type;
                break;

            case HerdMoved e:
                CurrentPaddockId = e.ToPaddockId;
                break;

            case AnimalAddedToHerd e:
                _members.Add(e.AnimalId);
                break;

            case AnimalRemovedFromHerd e:
                _members.Remove(e.AnimalId);
                break;
        }
    }
}
