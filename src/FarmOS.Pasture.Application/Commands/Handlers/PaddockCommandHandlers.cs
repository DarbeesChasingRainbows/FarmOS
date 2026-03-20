using FarmOS.Pasture.Domain;
using FarmOS.Pasture.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.SharedKernel.EventStore;

namespace FarmOS.Pasture.Application.Commands.Handlers;

/// <summary>
/// Handles all Paddock-related commands by loading the aggregate from the event store,
/// executing the domain logic, and appending new events.
/// </summary>
public sealed class PaddockCommandHandlers(IPastureEventStore eventStore) :
    ICommandHandler<CreatePaddockCommand, Guid>,
    ICommandHandler<UpdatePaddockBoundaryCommand, Guid>,
    ICommandHandler<BeginGrazingCommand, Guid>,
    ICommandHandler<EndGrazingCommand, Guid>,
    ICommandHandler<UpdateBiomassCommand, Guid>,
    ICommandHandler<RecordSoilTestCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(
        CreatePaddockCommand cmd, CancellationToken ct)
    {
        var paddock = Paddock.Create(cmd.Name, new Acreage(cmd.Acreage), cmd.LandType);
        await eventStore.SavePaddockAsync(paddock, "system", ct);
        return paddock.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        UpdatePaddockBoundaryCommand cmd, CancellationToken ct)
    {
        var paddock = await eventStore.LoadPaddockAsync(cmd.PaddockId.ToString(), ct);
        paddock.UpdateBoundary(cmd.Boundary);
        await eventStore.SavePaddockAsync(paddock, "system", ct);
        return paddock.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        BeginGrazingCommand cmd, CancellationToken ct)
    {
        var paddock = await eventStore.LoadPaddockAsync(cmd.PaddockId.ToString(), ct);
        var result = paddock.BeginGrazing(new HerdId(cmd.HerdId), cmd.Date);

        if (result.IsFailure)
            return result.Error;

        await eventStore.SavePaddockAsync(paddock, "system", ct);
        return paddock.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        EndGrazingCommand cmd, CancellationToken ct)
    {
        var paddock = await eventStore.LoadPaddockAsync(cmd.PaddockId.ToString(), ct);
        paddock.EndGrazing(cmd.Date);
        await eventStore.SavePaddockAsync(paddock, "system", ct);
        return paddock.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        UpdateBiomassCommand cmd, CancellationToken ct)
    {
        var paddock = await eventStore.LoadPaddockAsync(cmd.PaddockId.ToString(), ct);
        paddock.UpdateBiomass(new BiomassEstimate(cmd.TonsPerAcre, cmd.MeasuredOn, cmd.Method));
        await eventStore.SavePaddockAsync(paddock, "system", ct);
        return paddock.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        RecordSoilTestCommand cmd, CancellationToken ct)
    {
        var paddock = await eventStore.LoadPaddockAsync(cmd.PaddockId.ToString(), ct);
        paddock.RecordSoilTest(new SoilProfile(cmd.pH, cmd.OrganicMatterPct, cmd.CarbonPct, cmd.TestedOn, cmd.Lab));
        await eventStore.SavePaddockAsync(paddock, "system", ct);
        return paddock.Id.Value;
    }
}
