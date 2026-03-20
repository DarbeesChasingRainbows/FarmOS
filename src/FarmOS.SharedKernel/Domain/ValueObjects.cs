namespace FarmOS.SharedKernel;

// ─── Universal Measurement ───────────────────────────────────────────

/// <summary>
/// Universal measurement type used across all contexts.
/// Examples: (42.7, "percent", "ratio", "Soil Moisture"), (350, "lbs", "weight", "Hanging Weight")
/// </summary>
public record Quantity(decimal Value, string Unit, string Measure, string? Label = null);

// ─── GIS / Geospatial ────────────────────────────────────────────────

/// <summary>
/// GeoJSON geometry for polygons (paddock boundaries), lines (irrigation), and points.
/// </summary>
public record GeoJsonGeometry(string Type, double[][][] Coordinates);

/// <summary>
/// Simple lat/lng point for asset locations, animal positions, hive positions, etc.
/// </summary>
public record GeoPosition(double Latitude, double Longitude, double? Elevation = null);

/// <summary>
/// Simple X/Y Cartesian coordinate for indoor relative positioning (e.g. shelves).
/// </summary>
public record GridPosition(double X, double Y, string? Label = null);

/// <summary>
/// Universal cross-context asset referencing pointer.
/// Allows IoT devices to assign themselves to assets in other operational contexts.
/// </summary>
public record AssetRef(string Context, string AssetType, Guid AssetId, string Label);

// ─── File Attachments ────────────────────────────────────────────────

public record FileAttachment(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StoragePath,
    DateTimeOffset UploadedAt);

public record ImageAttachment(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StoragePath,
    int Width,
    int Height,
    DateTimeOffset UploadedAt);

// ─── Identification ──────────────────────────────────────────────────

/// <summary>
/// Flexible ID tagging supporting multiple identification schemes per asset.
/// Examples: ("ear_tag", "047"), ("eid", "840003123456789"), ("leg_band", "B-12")
/// </summary>
public record IdTag(string Type, string Value);

// ─── Temporal ────────────────────────────────────────────────────────

public record DateRange(DateOnly Start, DateOnly End);

public record Season(int Year, string Name);
