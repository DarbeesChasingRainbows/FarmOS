using FarmOS.Pasture.Domain;
using FarmOS.Pasture.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Pasture.Application.Commands.Handlers;

public sealed class HerdCommandHandlers(IPastureEventStore eventStore) :
    ICommandHandler<CreateHerdCommand, Guid>,
    ICommandHandler<MoveHerdCommand, Guid>,
    ICommandHandler<AddAnimalToHerdCommand, Guid>,
    ICommandHandler<RemoveAnimalFromHerdCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(
        CreateHerdCommand cmd, CancellationToken ct)
    {
        var herd = Herd.Create(cmd.Name, cmd.Type);
        await eventStore.SaveHerdAsync(herd, "system", ct);
        return herd.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        MoveHerdCommand cmd, CancellationToken ct)
    {
        var herd = await eventStore.LoadHerdAsync(cmd.HerdId.ToString(), ct);
        herd.MoveToPaddock(new PaddockId(cmd.PaddockId), cmd.Date);
        await eventStore.SaveHerdAsync(herd, "system", ct);
        return herd.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        AddAnimalToHerdCommand cmd, CancellationToken ct)
    {
        var herd = await eventStore.LoadHerdAsync(cmd.HerdId.ToString(), ct);
        var result = herd.AddAnimal(new AnimalId(cmd.AnimalId));
        if (result.IsFailure) return result.Error;
        await eventStore.SaveHerdAsync(herd, "system", ct);
        return herd.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        RemoveAnimalFromHerdCommand cmd, CancellationToken ct)
    {
        var herd = await eventStore.LoadHerdAsync(cmd.HerdId.ToString(), ct);
        var result = herd.RemoveAnimal(new AnimalId(cmd.AnimalId));
        if (result.IsFailure) return result.Error;
        await eventStore.SaveHerdAsync(herd, "system", ct);
        return herd.Id.Value;
    }
}
