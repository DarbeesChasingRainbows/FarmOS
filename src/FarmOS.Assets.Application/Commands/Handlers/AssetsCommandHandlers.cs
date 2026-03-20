using FarmOS.Assets.Domain;
using FarmOS.Assets.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Assets.Application.Commands.Handlers;

public sealed class EquipmentCommandHandlers(IAssetsEventStore store) :
    ICommandHandler<RegisterEquipmentCommand, Guid>,
    ICommandHandler<RecordEquipmentMaintenanceCommand, Guid>,
    ICommandHandler<MoveEquipmentCommand, Guid>,
    ICommandHandler<RetireEquipmentCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterEquipmentCommand cmd, CancellationToken ct)
    {
        var eq = Equipment.Register(cmd.Name, cmd.Make, cmd.Model, cmd.Year, cmd.Location);
        await store.SaveEquipmentAsync(eq, "steward", ct);
        return eq.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordEquipmentMaintenanceCommand cmd, CancellationToken ct)
    {
        var eq = await store.LoadEquipmentAsync(cmd.EquipmentId.ToString(), ct);
        eq.RecordMaintenance(cmd.Record);
        await store.SaveEquipmentAsync(eq, "steward", ct);
        return eq.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MoveEquipmentCommand cmd, CancellationToken ct)
    {
        var eq = await store.LoadEquipmentAsync(cmd.EquipmentId.ToString(), ct);
        eq.Move(cmd.NewLocation);
        await store.SaveEquipmentAsync(eq, "steward", ct);
        return eq.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RetireEquipmentCommand cmd, CancellationToken ct)
    {
        var eq = await store.LoadEquipmentAsync(cmd.EquipmentId.ToString(), ct);
        eq.Retire(cmd.Reason);
        await store.SaveEquipmentAsync(eq, "steward", ct);
        return eq.Id.Value;
    }
}

public sealed class StructureCommandHandlers(IAssetsEventStore store) :
    ICommandHandler<RegisterStructureCommand, Guid>,
    ICommandHandler<RecordStructureMaintenanceCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterStructureCommand cmd, CancellationToken ct)
    {
        var s = Structure.Register(cmd.Name, cmd.Type, cmd.Footprint);
        await store.SaveStructureAsync(s, "steward", ct);
        return s.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordStructureMaintenanceCommand cmd, CancellationToken ct)
    {
        var s = await store.LoadStructureAsync(cmd.StructureId.ToString(), ct);
        s.RecordMaintenance(cmd.Record);
        await store.SaveStructureAsync(s, "steward", ct);
        return s.Id.Value;
    }
}

public sealed class WaterSourceCommandHandlers(IAssetsEventStore store) :
    ICommandHandler<RegisterWaterSourceCommand, Guid>,
    ICommandHandler<RecordWaterTestCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterWaterSourceCommand cmd, CancellationToken ct)
    {
        var ws = WaterSource.Register(cmd.Name, cmd.Type, cmd.Position, cmd.FlowRate);
        await store.SaveWaterSourceAsync(ws, "steward", ct);
        return ws.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordWaterTestCommand cmd, CancellationToken ct)
    {
        var ws = await store.LoadWaterSourceAsync(cmd.WaterSourceId.ToString(), ct);
        ws.RecordWaterTest(cmd.pH, cmd.TDS, cmd.Date);
        await store.SaveWaterSourceAsync(ws, "steward", ct);
        return ws.Id.Value;
    }
}

public sealed class CompostCommandHandlers(IAssetsEventStore store) :
    ICommandHandler<StartCompostBatchCommand, Guid>,
    ICommandHandler<RecordCompostTempCommand, Guid>,
    ICommandHandler<TurnCompostCommand, Guid>,
    ICommandHandler<ChangeCompostPhaseCommand, Guid>,
    ICommandHandler<InoculateCompostCommand, Guid>,
    ICommandHandler<MeasureCompostPhCommand, Guid>,
    ICommandHandler<AddCompostNoteCommand, Guid>,
    ICommandHandler<CompleteCompostBatchCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(StartCompostBatchCommand cmd, CancellationToken ct)
    {
        var batch = CompostBatch.Start(cmd.BatchCode, cmd.Method, cmd.Location, cmd.Inputs, cmd.CarbonRatio, cmd.NitrogenRatio, cmd.Notes);
        await store.SaveCompostBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordCompostTempCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadCompostBatchAsync(cmd.BatchId.ToString(), ct);
        batch.RecordTemp(cmd.Reading);
        await store.SaveCompostBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(TurnCompostCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadCompostBatchAsync(cmd.BatchId.ToString(), ct);
        batch.Turn(cmd.Date, cmd.Notes);
        await store.SaveCompostBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(ChangeCompostPhaseCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadCompostBatchAsync(cmd.BatchId.ToString(), ct);
        batch.ChangePhase(cmd.NewPhase, cmd.Notes);
        await store.SaveCompostBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(InoculateCompostCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadCompostBatchAsync(cmd.BatchId.ToString(), ct);
        batch.Inoculate(cmd.Input);
        await store.SaveCompostBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MeasureCompostPhCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadCompostBatchAsync(cmd.BatchId.ToString(), ct);
        batch.MeasurePH(cmd.Measurement);
        await store.SaveCompostBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddCompostNoteCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadCompostBatchAsync(cmd.BatchId.ToString(), ct);
        batch.AddNote(cmd.Note);
        await store.SaveCompostBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CompleteCompostBatchCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadCompostBatchAsync(cmd.BatchId.ToString(), ct);
        batch.Complete(cmd.YieldCuYd, cmd.Notes);
        await store.SaveCompostBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }
}

public sealed class MaterialCommandHandlers(IAssetsEventStore store) :
    ICommandHandler<RegisterMaterialCommand, Guid>,
    ICommandHandler<UseMaterialCommand, Guid>,
    ICommandHandler<RestockMaterialCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterMaterialCommand cmd, CancellationToken ct)
    {
        var mat = Material.Register(cmd.Name, cmd.Category, cmd.OnHand, cmd.Supplier, cmd.IsOrganic);
        await store.SaveMaterialAsync(mat, "steward", ct);
        return mat.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UseMaterialCommand cmd, CancellationToken ct)
    {
        var mat = await store.LoadMaterialAsync(cmd.MaterialId.ToString(), ct);
        var result = mat.Use(cmd.Qty, cmd.Purpose);
        if (result.IsFailure) return result.Error;
        await store.SaveMaterialAsync(mat, "steward", ct);
        return mat.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RestockMaterialCommand cmd, CancellationToken ct)
    {
        var mat = await store.LoadMaterialAsync(cmd.MaterialId.ToString(), ct);
        mat.Restock(cmd.Qty, cmd.CostDollars);
        await store.SaveMaterialAsync(mat, "steward", ct);
        return mat.Id.Value;
    }
}
