using FarmOS.IoT.Domain;
using FarmOS.SharedKernel.CQRS;
using MediatR;

namespace FarmOS.IoT.Application.Commands;

public record RecordTelemetryReadingCommand(
    string DeviceCode,
    SensorType SensorType,
    decimal Value,
    string Unit) : ICommand<Unit>;
