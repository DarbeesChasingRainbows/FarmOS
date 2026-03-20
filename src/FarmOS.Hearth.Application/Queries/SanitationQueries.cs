using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Queries;

public record SanitationRecordDto(
    Guid Id,
    SanitationSurfaceType SurfaceType,
    string Area,
    string CleaningMethod,
    SanitizerType Sanitizer,
    decimal? SanitizerPpm,
    string CleanedBy,
    DateTimeOffset Timestamp
);

public record GetRecentSanitationRecordsQuery(int Limit = 50) : IQuery<IReadOnlyList<SanitationRecordDto>>;

public sealed class GetRecentSanitationRecordsHandler : IQueryHandler<GetRecentSanitationRecordsQuery, IReadOnlyList<SanitationRecordDto>>
{
    public Task<IReadOnlyList<SanitationRecordDto>?> Handle(GetRecentSanitationRecordsQuery request, CancellationToken cancellationToken)
    {
        // For now, returning an empty list as we haven't implemented the ArangoDB projector 
        // and read view for Sanitation compliance logs yet.
        IReadOnlyList<SanitationRecordDto> emptyList = Array.Empty<SanitationRecordDto>();
        return Task.FromResult<IReadOnlyList<SanitationRecordDto>?>(emptyList);
    }
}
