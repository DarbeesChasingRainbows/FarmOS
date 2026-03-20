using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Aggregates;
using MediatR;

namespace FarmOS.IoT.Application.Commands.Handlers;

public class ZoneCommandHandlers(IIoTEventStore eventStore) :
    ICommandHandler<CreateZoneCommand, string>,
    ICommandHandler<UpdateZoneCommand, Unit>,
    ICommandHandler<ArchiveZoneCommand, Unit>
{
    private readonly IIoTEventStore _eventStore = eventStore;

    public async Task<Result<string, DomainError>> Handle(CreateZoneCommand request, CancellationToken cancellationToken)
    {
        var id = ZoneId.New();
        var parentZoneId = request.ParentZoneId.HasValue ? new ZoneId(request.ParentZoneId.Value) : null;
        
        var zone = Zone.Create(
            id,
            request.Name,
            request.ZoneType,
            request.Description,
            request.GridPos,
            request.GeoPos,
            parentZoneId);

        await _eventStore.SaveZoneAsync(zone, "system", cancellationToken);
        return Result<string, DomainError>.Success(id.Value.ToString());
    }

    public async Task<Result<Unit, DomainError>> Handle(UpdateZoneCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var zone = await _eventStore.LoadZoneAsync(request.ZoneId.ToString(), cancellationToken);
            zone.Update(request.Name, request.Description);
            await _eventStore.SaveZoneAsync(zone, "system", cancellationToken);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit, DomainError>.Failure(new DomainError("Error", ex.Message));
        }
    }

    public async Task<Result<Unit, DomainError>> Handle(ArchiveZoneCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var zone = await _eventStore.LoadZoneAsync(request.ZoneId.ToString(), cancellationToken);
            zone.Archive(request.Reason);
            await _eventStore.SaveZoneAsync(zone, "system", cancellationToken);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit, DomainError>.Failure(new DomainError("Error", ex.Message));
        }
    }
}
