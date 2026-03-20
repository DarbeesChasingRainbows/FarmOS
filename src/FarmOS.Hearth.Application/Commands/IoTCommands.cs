using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands;

// ─── IoT Commands ────────────────────────────────────────────────────────────

public record IngestSensorReadingCommand(
    string DeviceId,
    SensorType SensorType,
    decimal Value,
    string Unit
) : ICommand<IoTAlert>;
