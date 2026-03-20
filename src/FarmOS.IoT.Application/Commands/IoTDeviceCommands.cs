using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.IoT.Domain;
using MediatR;

namespace FarmOS.IoT.Application.Commands;

public record RegisterDeviceCommand(
    string DeviceCode,
    string Name,
    SensorType SensorType,
    Guid? ZoneId = null,
    GridPosition? GridPos = null,
    GeoPosition? GeoPos = null,
    Dictionary<string, string>? Metadata = null) : ICommand<string>;

public record UpdateDeviceCommand(
    Guid DeviceId,
    string Name,
    DeviceStatus Status,
    Dictionary<string, string>? Metadata = null) : ICommand<Unit>;

public record DecommissionDeviceCommand(
    Guid DeviceId,
    string Reason) : ICommand<Unit>;

public record AssignDeviceToZoneCommand(
    Guid DeviceId,
    Guid ZoneId,
    GridPosition? GridPos = null,
    GeoPosition? GeoPos = null) : ICommand<Unit>;

public record UnassignDeviceFromZoneCommand(
    Guid DeviceId) : ICommand<Unit>;

public record AssignDeviceToAssetCommand(
    Guid DeviceId,
    AssetRef Asset) : ICommand<Unit>;

public record UnassignDeviceFromAssetCommand(
    Guid DeviceId,
    string Context,
    string AssetType,
    Guid AssetId) : ICommand<Unit>;
