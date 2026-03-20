using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands.Handlers;

public sealed class RecordSanitationHandler : ICommandHandler<RecordSanitationCommand, Guid>
{
    private readonly IHearthEventStore _eventStore;

    public RecordSanitationHandler(IHearthEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordSanitationCommand request, CancellationToken cancellationToken)
    {
        var id = SanitationRecordId.New();
        var record = SanitationRecord.Create(
            id,
            request.SurfaceType,
            request.Area,
            request.CleaningMethod,
            request.Sanitizer,
            request.SanitizerPpm,
            request.CleanedBy,
            DateTimeOffset.UtcNow
        );

        await _eventStore.SaveSanitationAsync(record, "steward", cancellationToken);

        // Events would be published to EventBus here by the infrastructure's outbox or event store implementation

        return id.Value;
    }
}
