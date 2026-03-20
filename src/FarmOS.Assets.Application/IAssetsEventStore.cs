using FarmOS.Assets.Domain.Aggregates;

namespace FarmOS.Assets.Application;

public interface IAssetsEventStore
{
    Task<Equipment> LoadEquipmentAsync(string id, CancellationToken ct);
    Task SaveEquipmentAsync(Equipment eq, string userId, CancellationToken ct);

    Task<Structure> LoadStructureAsync(string id, CancellationToken ct);
    Task SaveStructureAsync(Structure s, string userId, CancellationToken ct);

    Task<WaterSource> LoadWaterSourceAsync(string id, CancellationToken ct);
    Task SaveWaterSourceAsync(WaterSource ws, string userId, CancellationToken ct);

    Task<CompostBatch> LoadCompostBatchAsync(string id, CancellationToken ct);
    Task SaveCompostBatchAsync(CompostBatch cb, string userId, CancellationToken ct);


    Task<Material> LoadMaterialAsync(string id, CancellationToken ct);
    Task SaveMaterialAsync(Material mat, string userId, CancellationToken ct);
}
