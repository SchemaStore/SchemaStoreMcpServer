namespace SchemaStoreMcpServer.Services;

using SchemaStoreMcpServer.Models;

/// <summary>
/// Provides access to the SchemaStore.org catalog with search, lookup, and pagination.
/// </summary>
public interface ISchemaCatalogService
{
    /// <summary>
    /// Refreshes the in-memory catalog by fetching the latest data from SchemaStore.org.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches schemas by matching the query against name and description (case-insensitive).
    /// </summary>
    IReadOnlyList<SchemaCatalogEntry> Search(string query);

    /// <summary>
    /// Finds a schema by name using case-insensitive partial matching.
    /// Returns null if no match is found.
    /// </summary>
    SchemaCatalogEntry? FindByName(string nameOrPartial);

    /// <summary>
    /// Returns a paginated slice of all schemas.
    /// </summary>
    IReadOnlyList<SchemaCatalogEntry> List(int limit, int offset);

    /// <summary>
    /// Fetches the full JSON schema content from the given URL.
    /// </summary>
    Task<string> GetSchemaContentAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Total number of schemas in the catalog.
    /// </summary>
    int Count { get; }
}
