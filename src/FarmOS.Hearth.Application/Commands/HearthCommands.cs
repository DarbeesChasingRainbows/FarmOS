using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands;

// ─── Sourdough Commands ──────────────────────────────────────────────

public record StartSourdoughCommand(string BatchCode, Guid StarterId, IReadOnlyList<Ingredient> Ingredients) : ICommand<Guid>;
public record RecordSourdoughCCPCommand(Guid BatchId, HACCPReading Reading) : ICommand<Guid>;
public record AdvanceSourdoughPhaseCommand(Guid BatchId, BatchPhase NextPhase) : ICommand<Guid>;
public record CompleteSourdoughCommand(Guid BatchId, Quantity Yield) : ICommand<Guid>;

// ─── Kombucha Commands ───────────────────────────────────────────────

public record StartKombuchaCommand(string BatchCode, KombuchaType Type, Guid SCOBYId, string TeaType, string Sweetener, Quantity Volume, decimal StartingPH) : ICommand<Guid>;
public record RecordKombuchaPHCommand(Guid BatchId, decimal pH, string? Notes) : ICommand<Guid>;
public record AddKombuchaFlavoringCommand(Guid BatchId, Flavoring Flavoring) : ICommand<Guid>;
public record AdvanceKombuchaPhaseCommand(Guid BatchId, FermentationPhase NextPhase) : ICommand<Guid>;
public record CompleteKombuchaCommand(Guid BatchId, Quantity BottleCount) : ICommand<Guid>;

// ─── Culture Commands ────────────────────────────────────────────────

public record CreateCultureCommand(string Name, CultureType Type, DateOnly BirthDate, Guid? ParentId) : ICommand<Guid>;
public record FeedCultureCommand(Guid CultureId, FeedingRecord Feeding) : ICommand<Guid>;
public record SplitCultureCommand(Guid CultureId, string NewName, DateOnly Date) : ICommand<Guid>;

// ─── Sanitation Commands ─────────────────────────────────────────────

public record RecordSanitationCommand(SanitationSurfaceType SurfaceType, string Area, string CleaningMethod, SanitizerType Sanitizer, decimal? SanitizerPpm, string CleanedBy) : ICommand<Guid>;
