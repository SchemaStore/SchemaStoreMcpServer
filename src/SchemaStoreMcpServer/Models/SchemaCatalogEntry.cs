namespace SchemaStoreMcpServer.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a single schema entry from the SchemaStore.org catalog.
/// </summary>
public sealed record SchemaCatalogEntry
{
    /// <summary>
    /// The human-readable name of the schema.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// A brief description of what the schema covers.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Glob patterns for files that this schema applies to.
    /// </summary>
    [JsonPropertyName("fileMatch")]
    public string[]? FileMatch { get; init; }

    /// <summary>
    /// The URL where the full JSON Schema can be fetched.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

/// <summary>
/// Root object of the SchemaStore catalog API response.
/// </summary>
internal sealed record SchemaCatalog
{
    [JsonPropertyName("schemas")]
    public SchemaCatalogEntry[] Schemas { get; init; } = [];
}
