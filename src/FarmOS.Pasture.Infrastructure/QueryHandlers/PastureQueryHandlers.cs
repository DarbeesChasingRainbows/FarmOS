using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using FarmOS.Pasture.Application.Queries;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Pasture.Infrastructure.QueryHandlers;

/// <summary>
/// Reads Pasture projections directly from ArangoDB read-model collections.
/// For the initial implementation, we replay from the event store to build read models on the fly.
/// As volume grows, these will read from pre-built projection collections.
/// </summary>
public sealed class PaddockQueryHandlers(IArangoDBClient arango) :
    IQueryHandler<GetPaddocksQuery, IReadOnlyList<PaddockSummaryDto>>,
    IQueryHandler<GetPaddockByIdQuery, PaddockDetailDto>
{
    private const string ViewCollection = "pasture_paddock_view";

    public async Task<IReadOnlyList<PaddockSummaryDto>?> Handle(
        GetPaddocksQuery query, CancellationToken ct)
    {
        var aql = @"
            FOR p IN @@collection
                SORT p.Name ASC
                RETURN p
        ";

        var cursor = await arango.Cursor.PostCursorAsync<PaddockViewDoc>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = ViewCollection
                }
            });

        return cursor.Result.Select(p => new PaddockSummaryDto(
            Guid.Parse(p._key),
            p.Name,
            p.Acreage,
            p.LandType,
            p.Status,
            p.RestDaysElapsed,
            p.CurrentHerdId != null ? Guid.Parse(p.CurrentHerdId) : null
        )).ToList();
    }

    public async Task<PaddockDetailDto?> Handle(
        GetPaddockByIdQuery query, CancellationToken ct)
    {
        var aql = @"
            FOR p IN @@collection
                FILTER p._key == @id
                LIMIT 1
                RETURN p
        ";

        var cursor = await arango.Cursor.PostCursorAsync<PaddockViewDoc>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = ViewCollection,
                    ["id"] = query.PaddockId.ToString()
                }
            });

        var doc = cursor.Result.FirstOrDefault();
        if (doc is null) return null;

        return new PaddockDetailDto(
            Guid.Parse(doc._key),
            doc.Name,
            doc.Acreage,
            doc.LandType,
            doc.Status,
            doc.RestDaysElapsed,
            doc.CurrentHerdId != null ? Guid.Parse(doc.CurrentHerdId) : null,
            doc.BoundaryCoordinates,
            doc.BiomassTPAcre,
            doc.BiomassDate,
            doc.SoilpH,
            doc.SoilOM,
            doc.SoilTestDate);
    }
}

public sealed class AnimalQueryHandlers(IArangoDBClient arango) :
    IQueryHandler<GetAnimalsQuery, IReadOnlyList<AnimalSummaryDto>>,
    IQueryHandler<GetAnimalByIdQuery, AnimalDetailDto>
{
    private const string ViewCollection = "pasture_animal_view";

    public async Task<IReadOnlyList<AnimalSummaryDto>?> Handle(
        GetAnimalsQuery query, CancellationToken ct)
    {
        var filters = new List<string>();
        var bindVars = new Dictionary<string, object> { ["@collection"] = ViewCollection };

        if (!string.IsNullOrEmpty(query.Species))
        {
            filters.Add("a.Species == @species");
            bindVars["species"] = query.Species;
        }
        if (!string.IsNullOrEmpty(query.Status))
        {
            filters.Add("a.Status == @status");
            bindVars["status"] = query.Status;
        }

        var filterClause = filters.Count > 0 ? "FILTER " + string.Join(" AND ", filters) : "";

        var aql = $@"
            FOR a IN @@collection
                {filterClause}
                SORT a.PrimaryTag ASC
                RETURN a
        ";

        var cursor = await arango.Cursor.PostCursorAsync<AnimalViewDoc>(
            new PostCursorBody { Query = aql, BindVars = bindVars });

        return cursor.Result.Select(a => new AnimalSummaryDto(
            Guid.Parse(a._key),
            a.PrimaryTag,
            a.Species,
            a.Breed,
            a.Sex,
            a.Status,
            a.Nickname,
            a.CurrentHerdId != null ? Guid.Parse(a.CurrentHerdId) : null
        )).ToList();
    }

    public async Task<AnimalDetailDto?> Handle(
        GetAnimalByIdQuery query, CancellationToken ct)
    {
        var aql = @"
            FOR a IN @@collection
                FILTER a._key == @id
                LIMIT 1
                RETURN a
        ";

        var cursor = await arango.Cursor.PostCursorAsync<AnimalViewDoc>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = ViewCollection,
                    ["id"] = query.AnimalId.ToString()
                }
            });

        var doc = cursor.Result.FirstOrDefault();
        if (doc is null) return null;

        return new AnimalDetailDto(
            Guid.Parse(doc._key),
            doc.Tags?.Select(t => new TagDto(t.Type, t.Value)).ToList() ?? [],
            doc.Species,
            doc.Breed,
            doc.Sex,
            doc.Status,
            doc.Nickname,
            doc.DateAcquired,
            doc.CurrentHerdId != null ? Guid.Parse(doc.CurrentHerdId) : null,
            doc.Pregnancy != null ? new PregnancyDto(doc.Pregnancy.Confirmed, doc.Pregnancy.ExpectedDue, doc.Pregnancy.SireId != null ? Guid.Parse(doc.Pregnancy.SireId) : null) : null,
            doc.WeightHistory?.Select(w => new WeightEntryDto(w.Value, w.Unit, w.Date)).ToList() ?? [],
            doc.MedicalHistory?.Select(m => new MedicalEntryDto(m.Date, m.Diagnosis, m.TreatmentName)).ToList() ?? []
        );
    }
}

public sealed class HerdQueryHandlers(IArangoDBClient arango) :
    IQueryHandler<GetHerdsQuery, IReadOnlyList<HerdSummaryDto>>,
    IQueryHandler<GetHerdByIdQuery, HerdDetailDto>
{
    private const string HerdView = "pasture_herd_view";
    private const string AnimalView = "pasture_animal_view";

    public async Task<IReadOnlyList<HerdSummaryDto>?> Handle(
        GetHerdsQuery query, CancellationToken ct)
    {
        var aql = @"
            FOR h IN @@collection
                SORT h.Name ASC
                RETURN h
        ";

        var cursor = await arango.Cursor.PostCursorAsync<HerdViewDoc>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object> { ["@collection"] = HerdView }
            });

        return cursor.Result.Select(h => new HerdSummaryDto(
            Guid.Parse(h._key),
            h.Name,
            h.Type,
            h.CurrentPaddockId != null ? Guid.Parse(h.CurrentPaddockId) : null,
            h.MemberCount
        )).ToList();
    }

    public async Task<HerdDetailDto?> Handle(
        GetHerdByIdQuery query, CancellationToken ct)
    {
        var aql = @"
            LET herd = FIRST(FOR h IN @@herdCollection FILTER h._key == @id RETURN h)
            LET members = (
                FOR a IN @@animalCollection
                    FILTER a.CurrentHerdId == @id
                    RETURN a
            )
            RETURN { herd, members }
        ";

        var cursor = await arango.Cursor.PostCursorAsync<HerdWithMembersDoc>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@herdCollection"] = HerdView,
                    ["@animalCollection"] = AnimalView,
                    ["id"] = query.HerdId.ToString()
                }
            });

        var doc = cursor.Result.FirstOrDefault();
        if (doc?.herd is null) return null;

        var h = doc.herd;
        var members = doc.members?.Select(a => new AnimalSummaryDto(
            Guid.Parse(a._key), a.PrimaryTag, a.Species, a.Breed,
            a.Sex, a.Status, a.Nickname,
            a.CurrentHerdId != null ? Guid.Parse(a.CurrentHerdId) : null
        )).ToList() ?? [];

        return new HerdDetailDto(
            Guid.Parse(h._key), h.Name, h.Type,
            h.CurrentPaddockId != null ? Guid.Parse(h.CurrentPaddockId) : null,
            members);
    }
}

// ─── ArangoDB View Document Shapes (internal) ────────────────────────

internal record PaddockViewDoc
{
    public string _key { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal Acreage { get; init; }
    public string LandType { get; init; } = "";
    public string Status { get; init; } = "";
    public int RestDaysElapsed { get; init; }
    public string? CurrentHerdId { get; init; }
    public double[][]? BoundaryCoordinates { get; init; }
    public decimal? BiomassTPAcre { get; init; }
    public DateOnly? BiomassDate { get; init; }
    public decimal? SoilpH { get; init; }
    public decimal? SoilOM { get; init; }
    public DateOnly? SoilTestDate { get; init; }
}

internal record AnimalViewDoc
{
    public string _key { get; init; } = "";
    public string PrimaryTag { get; init; } = "";
    public string Species { get; init; } = "";
    public string? Breed { get; init; }
    public string Sex { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Nickname { get; init; }
    public DateOnly DateAcquired { get; init; }
    public string? CurrentHerdId { get; init; }
    public List<TagViewDoc>? Tags { get; init; }
    public PregnancyViewDoc? Pregnancy { get; init; }
    public List<WeightViewDoc>? WeightHistory { get; init; }
    public List<MedicalViewDoc>? MedicalHistory { get; init; }
}

internal record TagViewDoc(string Type, string Value);
internal record PregnancyViewDoc(DateOnly Confirmed, DateOnly ExpectedDue, string? SireId);
internal record WeightViewDoc(decimal Value, string Unit, DateOnly Date);
internal record MedicalViewDoc(DateOnly Date, string Diagnosis, string TreatmentName);

internal record HerdViewDoc
{
    public string _key { get; init; } = "";
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string? CurrentPaddockId { get; init; }
    public int MemberCount { get; init; }
}

internal record HerdWithMembersDoc
{
    public HerdViewDoc? herd { get; init; }
    public List<AnimalViewDoc>? members { get; init; }
}
