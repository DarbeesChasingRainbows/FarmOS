using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using MediatR;

namespace FarmOS.IoT.Application.Queries.Handlers;

public class IoTQueryHandlers(IIoTProjection projection) :
    IQueryHandler<GetAllDevicesQuery, List<DeviceSummaryDto>>,
    IQueryHandler<GetDeviceDetailQuery, DeviceDetailDto>,
    IQueryHandler<GetDevicesByZoneQuery, List<DeviceSummaryDto>>,
    IQueryHandler<GetAllZonesQuery, List<ZoneSummaryDto>>,
    IQueryHandler<GetZoneDetailQuery, ZoneDetailDto>
{
    private readonly IIoTProjection _projection = projection;

    public async Task<List<DeviceSummaryDto>?> Handle(GetAllDevicesQuery request, CancellationToken cancellationToken)
    {
        return await _projection.GetAllDevicesAsync(cancellationToken);
    }

    public async Task<DeviceDetailDto?> Handle(GetDeviceDetailQuery request, CancellationToken cancellationToken)
    {
        return await _projection.GetDeviceDetailAsync(request.DeviceId, cancellationToken);
    }

    public async Task<List<DeviceSummaryDto>?> Handle(GetDevicesByZoneQuery request, CancellationToken cancellationToken)
    {
        return await _projection.GetDevicesByZoneAsync(request.ZoneId, cancellationToken);
    }

    public async Task<List<ZoneSummaryDto>?> Handle(GetAllZonesQuery request, CancellationToken cancellationToken)
    {
        return await _projection.GetAllZonesAsync(cancellationToken);
    }

    public async Task<ZoneDetailDto?> Handle(GetZoneDetailQuery request, CancellationToken cancellationToken)
    {
        return await _projection.GetZoneDetailAsync(request.ZoneId, cancellationToken);
    }
}
