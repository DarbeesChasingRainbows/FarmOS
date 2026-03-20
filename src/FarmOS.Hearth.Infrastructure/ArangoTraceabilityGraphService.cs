using System.Text.Json;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using FarmOS.Hearth.Application;

namespace FarmOS.Hearth.Infrastructure;

/// <summary>
/// ArangoDB implementation of the traceability graph service.
/// Uses AQL graph traversals on the traceability_graph named graph
/// for sub-millisecond recall queries (IngredientLot → ProductBatch → Customer).
/// </summary>
public sealed class ArangoTraceabilityGraphService(IArangoDBClient client) : ITraceabilityGraphService
{
    private const string GraphName = "traceability_graph";
    private const string LotsCollection = "traceability_lots";
    private const string BatchesCollection = "traceability_batches";
    private const string CustomersCollection = "traceability_customers";
    private const string SuppliersCollection = "traceability_suppliers";

    // ─── Graph Traversal Queries ──────────────────────────────────────

    public async Task<RecallGraphDto> TraceRecallForwardAsync(string lotId, CancellationToken ct)
    {
        var aql = @"
            LET startNode = CONCAT('traceability_lots/', @lotId)
            FOR v, e, p IN 1..10 OUTBOUND startNode
                GRAPH @graphName
                RETURN { vertex: v, edge: e }
        ";

        return await ExecuteGraphQueryAsync(aql, new Dictionary<string, object>
        {
            ["lotId"] = lotId,
            ["graphName"] = GraphName
        });
    }

    public async Task<RecallGraphDto> TraceRecallBackwardAsync(string batchId, CancellationToken ct)
    {
        var aql = @"
            LET startNode = CONCAT('traceability_batches/', @batchId)
            FOR v, e, p IN 1..10 INBOUND startNode
                GRAPH @graphName
                RETURN { vertex: v, edge: e }
        ";

        return await ExecuteGraphQueryAsync(aql, new Dictionary<string, object>
        {
            ["batchId"] = batchId,
            ["graphName"] = GraphName
        });
    }

    public async Task<RecallGraphDto> GetFullRecallGraphAsync(string lotId, CancellationToken ct)
    {
        var aql = @"
            LET startNode = CONCAT('traceability_lots/', @lotId)
            LET forwardResults = (
                FOR v, e IN 1..10 OUTBOUND startNode GRAPH @graphName
                    RETURN { vertex: v, edge: e }
            )
            LET backwardResults = (
                FOR v, e IN 1..10 INBOUND startNode GRAPH @graphName
                    RETURN { vertex: v, edge: e }
            )
            LET startDoc = DOCUMENT(startNode)
            RETURN {
                forward: forwardResults,
                backward: backwardResults,
                start: startDoc
            }
        ";

        var cursor = await client.Cursor.PostCursorAsync<JsonElement>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["lotId"] = lotId,
                    ["graphName"] = GraphName
                }
            });

        var nodes = new Dictionary<string, RecallNodeDto>();
        var edges = new List<RecallEdgeDto>();

        foreach (var result in cursor.Result)
        {
            if (result.TryGetProperty("start", out var startDoc) && startDoc.ValueKind != JsonValueKind.Null)
            {
                AddNodeFromJson(nodes, startDoc);
            }

            if (result.TryGetProperty("forward", out var forward))
            {
                ProcessTraversalResults(forward, nodes, edges);
            }

            if (result.TryGetProperty("backward", out var backward))
            {
                ProcessTraversalResults(backward, nodes, edges);
            }
        }

        return new RecallGraphDto(nodes.Values.ToList(), edges);
    }

    // ─── Node/Edge Creation ──────────────────────────────────────────

    public async Task<string> CreateLotAsync(TraceabilityLotDto lot, CancellationToken ct)
    {
        return await InsertDocumentAsync(LotsCollection, new
        {
            _key = lot.LotCode,
            lotCode = lot.LotCode,
            description = lot.Description,
            category = lot.Category,
            receivedAt = lot.ReceivedAt,
            nodeType = "Lot"
        });
    }

    public async Task<string> CreateBatchAsync(TraceabilityBatchDto batch, CancellationToken ct)
    {
        return await InsertDocumentAsync(BatchesCollection, new
        {
            _key = batch.BatchCode,
            batchCode = batch.BatchCode,
            productName = batch.ProductName,
            category = batch.Category,
            producedAt = batch.ProducedAt,
            nodeType = "Batch"
        });
    }

    public async Task<string> CreateCustomerAsync(TraceabilityCustomerDto customer, CancellationToken ct)
    {
        var key = customer.Name.Replace(" ", "_").ToLowerInvariant();
        return await InsertDocumentAsync(CustomersCollection, new
        {
            _key = key,
            name = customer.Name,
            contact = customer.Contact,
            address = customer.Address,
            nodeType = "Customer"
        });
    }

    public async Task<string> CreateSupplierAsync(TraceabilitySupplierDto supplier, CancellationToken ct)
    {
        var key = supplier.Name.Replace(" ", "_").ToLowerInvariant();
        return await InsertDocumentAsync(SuppliersCollection, new
        {
            _key = key,
            name = supplier.Name,
            contact = supplier.Contact,
            address = supplier.Address,
            nodeType = "Supplier"
        });
    }

    public async Task LinkLotToBatchAsync(string lotId, string batchId, CancellationToken ct)
    {
        await InsertEdgeAsync("used_in",
            $"{LotsCollection}/{lotId}",
            $"{BatchesCollection}/{batchId}");
    }

    public async Task LinkBatchToCustomerAsync(string batchId, string customerId, CancellationToken ct)
    {
        await InsertEdgeAsync("sold_to",
            $"{BatchesCollection}/{batchId}",
            $"{CustomersCollection}/{customerId}");
    }

    public async Task LinkLotToSupplierAsync(string lotId, string supplierId, CancellationToken ct)
    {
        await InsertEdgeAsync("sourced_from",
            $"{LotsCollection}/{lotId}",
            $"{SuppliersCollection}/{supplierId}");
    }

    // ─── Private Helpers ─────────────────────────────────────────────

    private async Task<RecallGraphDto> ExecuteGraphQueryAsync(
        string aql, Dictionary<string, object> bindVars)
    {
        var cursor = await client.Cursor.PostCursorAsync<JsonElement>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = bindVars
            });

        var nodes = new Dictionary<string, RecallNodeDto>();
        var edges = new List<RecallEdgeDto>();

        foreach (var result in cursor.Result)
        {
            if (result.TryGetProperty("vertex", out var vertex) && vertex.ValueKind != JsonValueKind.Null)
            {
                AddNodeFromJson(nodes, vertex);
            }

            if (result.TryGetProperty("edge", out var edge) && edge.ValueKind != JsonValueKind.Null)
            {
                var from = edge.GetProperty("_from").GetString() ?? "";
                var to = edge.GetProperty("_to").GetString() ?? "";
                var edgeId = edge.GetProperty("_id").GetString() ?? "";
                var collection = edgeId.Split('/').FirstOrDefault() ?? "";

                edges.Add(new RecallEdgeDto(from, to, collection));
            }
        }

        return new RecallGraphDto(nodes.Values.ToList(), edges);
    }

    private static void AddNodeFromJson(Dictionary<string, RecallNodeDto> nodes, JsonElement vertex)
    {
        var id = vertex.GetProperty("_id").GetString() ?? "";
        if (nodes.ContainsKey(id)) return;

        var nodeType = vertex.TryGetProperty("nodeType", out var nt) ? nt.GetString() ?? "Unknown" : "Unknown";
        var label = vertex.TryGetProperty("lotCode", out var lc) ? lc.GetString() ?? id :
                    vertex.TryGetProperty("batchCode", out var bc) ? bc.GetString() ?? id :
                    vertex.TryGetProperty("name", out var nm) ? nm.GetString() ?? id : id;

        var props = new Dictionary<string, string>();
        foreach (var prop in vertex.EnumerateObject())
        {
            if (!prop.Name.StartsWith("_") && prop.Name != "nodeType")
            {
                props[prop.Name] = prop.Value.ToString();
            }
        }

        nodes[id] = new RecallNodeDto(id, label, nodeType, props);
    }

    private static void ProcessTraversalResults(
        JsonElement results,
        Dictionary<string, RecallNodeDto> nodes,
        List<RecallEdgeDto> edges)
    {
        foreach (var item in results.EnumerateArray())
        {
            if (item.TryGetProperty("vertex", out var vertex) && vertex.ValueKind != JsonValueKind.Null)
            {
                AddNodeFromJson(nodes, vertex);
            }

            if (item.TryGetProperty("edge", out var edge) && edge.ValueKind != JsonValueKind.Null)
            {
                var from = edge.GetProperty("_from").GetString() ?? "";
                var to = edge.GetProperty("_to").GetString() ?? "";
                var edgeId = edge.GetProperty("_id").GetString() ?? "";
                var collection = edgeId.Split('/').FirstOrDefault() ?? "";

                edges.Add(new RecallEdgeDto(from, to, collection));
            }
        }
    }

    private async Task<string> InsertDocumentAsync(string collection, object document)
    {
        var aql = @"
            INSERT @doc INTO @@collection
            OPTIONS { overwriteMode: ""ignore"" }
            RETURN NEW._key
        ";

        var cursor = await client.Cursor.PostCursorAsync<string>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = collection,
                    ["doc"] = document
                }
            });

        return cursor.Result.FirstOrDefault() ?? "";
    }

    private async Task InsertEdgeAsync(string edgeCollection, string from, string to)
    {
        var aql = @"
            UPSERT { _from: @from, _to: @to }
            INSERT { _from: @from, _to: @to, createdAt: DATE_NOW() }
            UPDATE {} IN @@collection
        ";

        await client.Cursor.PostCursorAsync<object>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = edgeCollection,
                    ["from"] = from,
                    ["to"] = to
                }
            });
    }
}
