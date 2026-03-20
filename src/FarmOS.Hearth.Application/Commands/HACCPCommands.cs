using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands;

// ─── HACCP Plan Commands ────────────────────────────────────────────

public record CreateHACCPPlanCommand(string PlanName, string FacilityName) : ICommand<Guid>;
public record AddCCPDefinitionCommand(Guid PlanId, CCPDefinition Definition) : ICommand<Guid>;
public record RemoveCCPDefinitionCommand(Guid PlanId, string Product, string CCPName) : ICommand<Guid>;

// ─── CAPA Commands ──────────────────────────────────────────────────

public record OpenCAPACommand(string Description, string DeviationSource, CriticalTrackingEvent? RelatedCTE) : ICommand<Guid>;
public record CloseCAPACommand(Guid CAPAId, string Resolution, string VerifiedBy) : ICommand<Guid>;

// ─── Equipment Monitoring Commands ──────────────────────────────────

public record LogEquipmentTemperatureCommand(Guid EquipmentId, decimal TemperatureF, string LoggedBy) : ICommand<Guid>;
public record AppendMonitoringCorrectionCommand(Guid OriginalLogId, string Reason, decimal? CorrectedValueF, string CorrectedBy) : ICommand<Guid>;
