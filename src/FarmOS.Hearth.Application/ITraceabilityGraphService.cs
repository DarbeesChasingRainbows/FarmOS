namespace FarmOS.Hearth.Application;

/// <summary>
/// Service for querying the ArangoDB traceability graph.
/// Supports forward/backward recall traces and full graph queries
/// for FDA FSMA 204 compliance (IngredientLot → ProductBatch → Customer).
/// </summary>
public interface ITraceabilityGraphService
{
    /// <summary>Forward trace: lot → all downstream batches → all customers.</summary>
    Task<RecallGraphDto> TraceRecallForwardAsync(string lotId, CancellationToken ct);

    /// <summary>Backward trace: batch → all upstream ingredient lots → suppliers.</summary>
    Task<RecallGraphDto> TraceRecallBackwardAsync(string batchId, CancellationToken ct);

    /// <summary>Bidirectional recall graph for investigation.</summary>
    Task<RecallGraphDto> GetFullRecallGraphAsync(string lotId, CancellationToken ct);

    /// <summary>Create a lot node in the traceability graph.</summary>
    Task<string> CreateLotAsync(TraceabilityLotDto lot, CancellationToken ct);

    /// <summary>Create a batch node in the traceability graph.</summary>
    Task<string> CreateBatchAsync(TraceabilityBatchDto batch, CancellationToken ct);

    /// <summary>Create a customer node in the traceability graph.</summary>
    Task<string> CreateCustomerAsync(TraceabilityCustomerDto customer, CancellationToken ct);

    /// <summary>Create a supplier node in the traceability graph.</summary>
    Task<string> CreateSupplierAsync(TraceabilitySupplierDto supplier, CancellationToken ct);

    /// <summary>Link a lot to a batch (lot was used_in batch).</summary>
    Task LinkLotToBatchAsync(string lotId, string batchId, CancellationToken ct);

    /// <summary>Link a batch to a customer (batch was sold_to customer).</summary>
    Task LinkBatchToCustomerAsync(string batchId, string customerId, CancellationToken ct);

    /// <summary>Link a lot to a supplier (lot was sourced_from supplier).</summary>
    Task LinkLotToSupplierAsync(string lotId, string supplierId, CancellationToken ct);
}

// ─── DTOs ────────────────────────────────────────────────────────────────

public record TraceabilityLotDto(
    string LotCode,
    string Description,
    string? Category,
    DateTimeOffset ReceivedAt);

public record TraceabilityBatchDto(
    string BatchCode,
    string ProductName,
    string? Category,
    DateTimeOffset ProducedAt);

public record TraceabilityCustomerDto(
    string Name,
    string? Contact,
    string? Address);

public record TraceabilitySupplierDto(
    string Name,
    string? Contact,
    string? Address);

public record RecallGraphDto(
    List<RecallNodeDto> Nodes,
    List<RecallEdgeDto> Edges);

public record RecallNodeDto(
    string Id,
    string Label,
    string NodeType,
    Dictionary<string, string>? Properties);

public record RecallEdgeDto(
    string From,
    string To,
    string Relationship);
