using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.Hearth.Domain.Aggregates;
using MediatR;

namespace FarmOS.Hearth.Application.Commands.Handlers;

public class LogReceivingEventHandler(IHearthEventStore eventStore) : ICommandHandler<LogReceivingEventCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(LogReceivingEventCommand request, CancellationToken cancellationToken)
    {
        var record = TraceabilityRecord.LogReceiving(request.Category, request.Description, request.LotId, request.Amount, request.SourceSupplier, request.Timestamp);
        await eventStore.SaveTraceabilityAsync(record, "system", cancellationToken);
        return record.Id.Value;
    }
}

public class LogTransformationEventHandler(IHearthEventStore eventStore) : ICommandHandler<LogTransformationEventCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(LogTransformationEventCommand request, CancellationToken cancellationToken)
    {
        var record = TraceabilityRecord.LogTransformation(request.Category, request.Description, request.NewLotId, request.Amount, request.SourceLotId, request.Timestamp);
        await eventStore.SaveTraceabilityAsync(record, "system", cancellationToken);
        return record.Id.Value;
    }
}

public class LogShippingEventHandler(IHearthEventStore eventStore) : ICommandHandler<LogShippingEventCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(LogShippingEventCommand request, CancellationToken cancellationToken)
    {
        var record = TraceabilityRecord.LogShipping(request.Category, request.Description, request.LotId, request.Amount, request.Destination, request.Timestamp);
        await eventStore.SaveTraceabilityAsync(record, "system", cancellationToken);
        return record.Id.Value;
    }
}
