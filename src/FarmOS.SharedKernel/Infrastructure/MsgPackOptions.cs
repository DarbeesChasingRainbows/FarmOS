using MessagePack;
using MessagePack.Resolvers;

namespace FarmOS.SharedKernel.Infrastructure;

/// <summary>
/// Single source of truth for MessagePack serialization options across all FarmOS services.
///
/// Uses ContractlessStandardResolver so domain types need zero attributes —
/// serialization remains an infrastructure concern per DDD/SOLID principles.
///
/// LZ4 block array compression provides ~60-70% size reduction over JSON
/// with negligible CPU overhead.
/// </summary>
public static class MsgPackOptions
{
    /// <summary>
    /// Standard options for all internal serialization (event store, RabbitMQ, API wire format).
    /// </summary>
    public static readonly MessagePackSerializerOptions Standard =
        MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithSecurity(MessagePackSecurity.UntrustedData);

    /// <summary>
    /// Serialize a domain event to a Base64 string for ArangoDB storage.
    /// ArangoDB documents are JSON, so binary payloads are Base64-encoded.
    /// </summary>
    public static string SerializeToBase64(object value, Type type) =>
        Convert.ToBase64String(MessagePackSerializer.Serialize(type, value, Standard));

    /// <summary>
    /// Deserialize a domain event from a Base64-encoded ArangoDB payload.
    /// </summary>
    public static object? DeserializeFromBase64(string base64, Type type) =>
        MessagePackSerializer.Deserialize(type, Convert.FromBase64String(base64), Standard);

    /// <summary>
    /// Serialize directly to byte[] for RabbitMQ wire format (no Base64 overhead).
    /// </summary>
    public static byte[] SerializeToBytes<T>(T value) =>
        MessagePackSerializer.Serialize(value, Standard);

    /// <summary>
    /// Deserialize directly from byte[] for RabbitMQ wire format.
    /// </summary>
    public static T DeserializeFromBytes<T>(ReadOnlyMemory<byte> bytes) =>
        MessagePackSerializer.Deserialize<T>(bytes, Standard);
}
