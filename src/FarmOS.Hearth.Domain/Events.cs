using FarmOS.SharedKernel;

namespace FarmOS.Hearth.Domain.Events;

// Sourdough
public record SourdoughBatchStarted(BatchId Id, string BatchCode, LivingCultureId StarterId, IReadOnlyList<Ingredient> Ingredients, DateTimeOffset OccurredAt) : IDomainEvent;
public record CCPReadingRecorded(BatchId Id, HACCPReading Reading, DateTimeOffset OccurredAt) : IDomainEvent;
public record SourdoughPhaseAdvanced(BatchId Id, BatchPhase Previous, BatchPhase Next, DateTimeOffset OccurredAt) : IDomainEvent;
public record SourdoughBatchCompleted(BatchId Id, Quantity Yield, DateTimeOffset OccurredAt) : IDomainEvent;
public record SourdoughBatchDiscarded(BatchId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// Kombucha
public record KombuchaBatchStarted(BatchId Id, string BatchCode, KombuchaType Type, LivingCultureId SCOBYId, string TeaType, string Sweetener, Quantity Volume, decimal StartingPH, DateTimeOffset OccurredAt) : IDomainEvent;
public record KombuchaPHRecorded(BatchId Id, PHReading Reading, DateTimeOffset OccurredAt) : IDomainEvent;
public record KombuchaFlavoringAdded(BatchId Id, Flavoring Flavoring, DateTimeOffset OccurredAt) : IDomainEvent;
public record KombuchaPhaseAdvanced(BatchId Id, FermentationPhase Previous, FermentationPhase Next, DateTimeOffset OccurredAt) : IDomainEvent;
public record KombuchaBatchCompleted(BatchId Id, Quantity BottleCount, DateTimeOffset OccurredAt) : IDomainEvent;
public record KombuchaBatchDiscarded(BatchId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// Living Cultures
public record CultureCreated(LivingCultureId Id, string Name, CultureType Type, DateOnly BirthDate, LivingCultureId? ParentId, DateTimeOffset OccurredAt) : IDomainEvent;
public record CultureFed(LivingCultureId Id, FeedingRecord Feeding, DateTimeOffset OccurredAt) : IDomainEvent;
public record CultureSplit(LivingCultureId ParentId, LivingCultureId OffspringId, string NewName, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;

// IoT
public record SensorReadingIngested(string DeviceId, SensorReading Reading, IoTAlert Alert, DateTimeOffset OccurredAt) : IDomainEvent;

// Mushroom
public record MushroomBatchStarted(BatchId Id, string BatchCode, string Species, string SubstrateType, DateTimeOffset InoculatedAt, DateTimeOffset OccurredAt) : IDomainEvent;
public record MushroomTemperatureRecorded(BatchId Id, EnvironmentReading Reading, DateTimeOffset OccurredAt) : IDomainEvent;
public record MushroomHumidityRecorded(BatchId Id, EnvironmentReading Reading, DateTimeOffset OccurredAt) : IDomainEvent;
public record MushroomPhaseAdvanced(BatchId Id, string PreviousPhase, string NewPhase, DateTimeOffset OccurredAt) : IDomainEvent;
public record MushroomFlushRecorded(BatchId Id, Quantity Yield, int FlushNumber, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record MushroomBatchContaminated(BatchId Id, string Reason, DateTimeOffset Timestamp, DateTimeOffset OccurredAt) : IDomainEvent;
public record MushroomBatchCompleted(BatchId Id, Quantity TotalYield, int TotalFlushes, DateTimeOffset CompletedAt, DateTimeOffset OccurredAt) : IDomainEvent;
public record MushroomHarvestAvailable(string Species, Quantity Amount, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;

// Sanitation
public record SanitationRecordCreated(SanitationRecordId Id, SanitationSurfaceType SurfaceType, string Area, string CleaningMethod, SanitizerType Sanitizer, decimal? SanitizerPpm, string CleanedBy, DateTimeOffset OccurredAt) : IDomainEvent;

// Traceability & FSMA 204
public record TraceabilityEventLogged(TraceabilityRecordId Id, CriticalTrackingEvent EventType, ProductCategory Category, string ProductDescription, string LotId, Quantity Amount, string? SourceLocation, string? DestinationLocation, string? SourceLotId, DateTimeOffset OccurredAt) : IDomainEvent;

// HACCP Plan
public record HACCPPlanCreated(HACCPPlanId Id, string PlanName, string FacilityName, DateTimeOffset OccurredAt) : IDomainEvent;
public record CCPDefinitionAdded(HACCPPlanId PlanId, CCPDefinition Definition, DateTimeOffset OccurredAt) : IDomainEvent;
public record CCPDefinitionRemoved(HACCPPlanId PlanId, string Product, string CCPName, DateTimeOffset OccurredAt) : IDomainEvent;

// CAPA (Corrective and Preventive Action)
public record CAPAOpened(CAPAId Id, string Description, string DeviationSource, CriticalTrackingEvent? RelatedCTE, DateTimeOffset OccurredAt) : IDomainEvent;
public record CAPAClosed(CAPAId Id, string Resolution, string VerifiedBy, DateTimeOffset OccurredAt) : IDomainEvent;

// Equipment Monitoring (immutable log + correction pattern)
public record EquipmentTemperatureLogged(MonitoringLogId Id, EquipmentId EquipmentId, decimal TemperatureF, string LoggedBy, DateTimeOffset OccurredAt) : IDomainEvent;
public record MonitoringCorrectionAppended(MonitoringLogId OriginalLogId, string Reason, decimal? CorrectedValueF, string CorrectedBy, DateTimeOffset OccurredAt) : IDomainEvent;

// Freeze-Dryer
public record FreezeDryerBatchStarted(BatchId Id, string BatchCode, FreezeDryerId DryerId, string ProductDescription, decimal PreDryWeight, DateTimeOffset OccurredAt) : IDomainEvent;
public record FreezeDryerPhaseAdvanced(BatchId Id, FreezeDryerPhase Previous, FreezeDryerPhase Next, DateTimeOffset OccurredAt) : IDomainEvent;
public record FreezeDryerReadingRecorded(BatchId Id, FreezeDryerReading Reading, DateTimeOffset OccurredAt) : IDomainEvent;
public record FreezeDryerBatchCompleted(BatchId Id, decimal PostDryWeight, DateTimeOffset OccurredAt) : IDomainEvent;
public record FreezeDryerBatchAborted(BatchId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// Harvest Right Telemetry
public record HarvestRightTelemetryIngested(
    FreezeDryerId DryerId,
    int ScreenNumber,
    decimal TemperatureF,
    decimal VacuumMTorr,
    decimal ProgressPercent,
    IoTAlert? Alert,
    DateTimeOffset OccurredAt) : IDomainEvent;
