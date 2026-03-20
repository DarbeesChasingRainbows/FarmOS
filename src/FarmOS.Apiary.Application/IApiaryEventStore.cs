using FarmOS.Apiary.Domain.Aggregates;
using ApiaryAggregate = FarmOS.Apiary.Domain.Aggregates.Apiary;

namespace FarmOS.Apiary.Application;

public interface IApiaryEventStore
{
    Task<Hive> LoadHiveAsync(string hiveId, CancellationToken ct);
    Task SaveHiveAsync(Hive hive, string userId, CancellationToken ct);
    Task<ApiaryAggregate> LoadApiaryAsync(string apiaryId, CancellationToken ct);
    Task SaveApiaryAsync(ApiaryAggregate apiary, string userId, CancellationToken ct);
}
