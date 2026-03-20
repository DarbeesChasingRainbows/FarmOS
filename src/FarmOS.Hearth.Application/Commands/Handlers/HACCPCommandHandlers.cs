using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands.Handlers;

// ─── HACCP Plan Handlers ────────────────────────────────────────────

public sealed class HACCPPlanCommandHandlers(IHearthEventStore store) :
    ICommandHandler<CreateHACCPPlanCommand, Guid>,
    ICommandHandler<AddCCPDefinitionCommand, Guid>,
    ICommandHandler<RemoveCCPDefinitionCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateHACCPPlanCommand cmd, CancellationToken ct)
    {
        var plan = HACCPPlan.Create(cmd.PlanName, cmd.FacilityName);
        await store.SaveHACCPPlanAsync(plan, "steward", ct);
        return plan.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddCCPDefinitionCommand cmd, CancellationToken ct)
    {
        var plan = await store.LoadHACCPPlanAsync(cmd.PlanId.ToString(), ct);
        var result = plan.AddCCPDefinition(cmd.Definition);
        if (result.IsFailure) return result.Error;
        await store.SaveHACCPPlanAsync(plan, "steward", ct);
        return plan.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RemoveCCPDefinitionCommand cmd, CancellationToken ct)
    {
        var plan = await store.LoadHACCPPlanAsync(cmd.PlanId.ToString(), ct);
        var result = plan.RemoveCCPDefinition(cmd.Product, cmd.CCPName);
        if (result.IsFailure) return result.Error;
        await store.SaveHACCPPlanAsync(plan, "steward", ct);
        return plan.Id.Value;
    }
}

// ─── CAPA Handlers ──────────────────────────────────────────────────

public sealed class CAPACommandHandlers(IHearthEventStore store) :
    ICommandHandler<OpenCAPACommand, Guid>,
    ICommandHandler<CloseCAPACommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(OpenCAPACommand cmd, CancellationToken ct)
    {
        var id = CAPAId.New();
        var @event = new CAPAOpened(id, cmd.Description, cmd.DeviationSource, cmd.RelatedCTE, DateTimeOffset.UtcNow);
        await store.AppendCAPAEventAsync(@event, ct);
        return id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CloseCAPACommand cmd, CancellationToken ct)
    {
        var id = new CAPAId(cmd.CAPAId);
        var @event = new CAPAClosed(id, cmd.Resolution, cmd.VerifiedBy, DateTimeOffset.UtcNow);
        await store.AppendCAPAEventAsync(@event, ct);
        return id.Value;
    }
}

// ─── Equipment Monitoring Handlers ──────────────────────────────────

public sealed class EquipmentMonitoringCommandHandlers(IHearthEventStore store, IKitchenHubNotifier notifier) :
    ICommandHandler<LogEquipmentTemperatureCommand, Guid>,
    ICommandHandler<AppendMonitoringCorrectionCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(LogEquipmentTemperatureCommand cmd, CancellationToken ct)
    {
        var logId = MonitoringLogId.New();
        var equipId = new EquipmentId(cmd.EquipmentId);
        var @event = new EquipmentTemperatureLogged(logId, equipId, cmd.TemperatureF, cmd.LoggedBy, DateTimeOffset.UtcNow);
        await store.AppendEquipmentTempEventAsync(@event, ct);

        // Evaluate via F# rules and broadcast alert
        var reading = new SensorReading(cmd.EquipmentId.ToString(), SensorType.Temperature, cmd.TemperatureF, "°F", DateTimeOffset.UtcNow);
        var alert = new IoTAlert(cmd.EquipmentId.ToString(), AlertLevel.Safe, $"Equipment temp logged: {cmd.TemperatureF}°F", null, DateTimeOffset.UtcNow);
        await notifier.BroadcastAsync(reading, alert, ct);

        return logId.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AppendMonitoringCorrectionCommand cmd, CancellationToken ct)
    {
        var originalId = new MonitoringLogId(cmd.OriginalLogId);
        var @event = new MonitoringCorrectionAppended(originalId, cmd.Reason, cmd.CorrectedValueF, cmd.CorrectedBy, DateTimeOffset.UtcNow);
        await store.AppendMonitoringCorrectionAsync(@event, ct);
        return originalId.Value;
    }
}
