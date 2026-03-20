using FarmOS.Pasture.Domain;
using FarmOS.Pasture.Domain.Aggregates;
using FarmOS.SharedKernel;

namespace FarmOS.Pasture.Application;

/// <summary>
/// Context-specific event store abstraction for Pasture.
/// Wraps the generic IEventStore with Pasture-specific collection names, serialization, and typed loaders.
/// This keeps Application layer handlers clean and focused on domain orchestration.
/// </summary>
public interface IPastureEventStore
{
    // ─── Paddock ─────────────────────────────────────────────────
    Task<Paddock> LoadPaddockAsync(string paddockId, CancellationToken ct);
    Task SavePaddockAsync(Paddock paddock, string userId, CancellationToken ct);

    // ─── Animal ──────────────────────────────────────────────────
    Task<Animal> LoadAnimalAsync(string animalId, CancellationToken ct);
    Task SaveAnimalAsync(Animal animal, string userId, CancellationToken ct);

    // ─── Herd ────────────────────────────────────────────────────
    Task<Herd> LoadHerdAsync(string herdId, CancellationToken ct);
    Task SaveHerdAsync(Herd herd, string userId, CancellationToken ct);
}
