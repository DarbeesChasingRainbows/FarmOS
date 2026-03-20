using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Aggregates;
using MediatR;

namespace FarmOS.IoT.Application.Commands.Handlers;

public class DeviceCommandHandlers(IIoTEventStore eventStore) :
    ICommandHandler<RegisterDeviceCommand, string>,
    ICommandHandler<UpdateDeviceCommand, Unit>,
    ICommandHandler<DecommissionDeviceCommand, Unit>,
    ICommandHandler<AssignDeviceToZoneCommand, Unit>,
    ICommandHandler<UnassignDeviceFromZoneCommand, Unit>,
    ICommandHandler<AssignDeviceToAssetCommand, Unit>,
    ICommandHandler<UnassignDeviceFromAssetCommand, Unit>
{
    private readonly IIoTEventStore _eventStore = eventStore;

    public async Task<Result<string, DomainError>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var id = IoTDeviceId.New();
        var zoneId = request.ZoneId.HasValue ? new ZoneId(request.ZoneId.Value) : null;
        
        var device = IoTDevice.Register(
            id,
            request.DeviceCode,
            request.Name,
            request.SensorType,
            zoneId,
            request.GridPos,
            request.GeoPos,
            request.Metadata);

        await _eventStore.SaveDeviceAsync(device, "system", cancellationToken);
        return Result<string, DomainError>.Success(id.Value.ToString());
    }

    public async Task<Result<Unit, DomainError>> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _eventStore.LoadDeviceAsync(request.DeviceId.ToString(), cancellationToken);
            device.Update(request.Name, request.Status, request.Metadata);
            await _eventStore.SaveDeviceAsync(device, "system", cancellationToken);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit, DomainError>.Failure(new DomainError("Error", ex.Message));
        }
    }

    public async Task<Result<Unit, DomainError>> Handle(DecommissionDeviceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _eventStore.LoadDeviceAsync(request.DeviceId.ToString(), cancellationToken);
            device.Decommission(request.Reason);
            await _eventStore.SaveDeviceAsync(device, "system", cancellationToken);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit, DomainError>.Failure(new DomainError("Error", ex.Message));
        }
    }

    public async Task<Result<Unit, DomainError>> Handle(AssignDeviceToZoneCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _eventStore.LoadDeviceAsync(request.DeviceId.ToString(), cancellationToken);
            // Verify zone exists
            var _ = await _eventStore.LoadZoneAsync(request.ZoneId.ToString(), cancellationToken);
            
            device.AssignToZone(new ZoneId(request.ZoneId), request.GridPos, request.GeoPos);
            await _eventStore.SaveDeviceAsync(device, "system", cancellationToken);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit, DomainError>.Failure(new DomainError("Error", ex.Message));
        }
    }

    public async Task<Result<Unit, DomainError>> Handle(UnassignDeviceFromZoneCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _eventStore.LoadDeviceAsync(request.DeviceId.ToString(), cancellationToken);
            device.UnassignFromZone();
            await _eventStore.SaveDeviceAsync(device, "system", cancellationToken);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit, DomainError>.Failure(new DomainError("Error", ex.Message));
        }
    }

    public async Task<Result<Unit, DomainError>> Handle(AssignDeviceToAssetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _eventStore.LoadDeviceAsync(request.DeviceId.ToString(), cancellationToken);
            device.AssignToAsset(request.Asset);
            await _eventStore.SaveDeviceAsync(device, "system", cancellationToken);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit, DomainError>.Failure(new DomainError("Error", ex.Message));
        }
    }

    public async Task<Result<Unit, DomainError>> Handle(UnassignDeviceFromAssetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _eventStore.LoadDeviceAsync(request.DeviceId.ToString(), cancellationToken);
            device.UnassignFromAsset(request.Context, request.AssetType, request.AssetId);
            await _eventStore.SaveDeviceAsync(device, "system", cancellationToken);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit, DomainError>.Failure(new DomainError("Error", ex.Message));
        }
    }
}
