namespace SchemaStoreMcpServer.Services;

using System.Text.Json;
using SchemaStoreMcpServer.Models;

/// <summary>
/// Singleton service that maintains an in-memory copy of the SchemaStore.org catalog.
/// Thread-safe: catalog reference is swapped atomically on refresh.
/// </summary>
public sealed class SchemaCatalogService : ISchemaCatalogService
{
    private static readonly Uri CatalogUri = new("https://www.schemastore.org/api/json/catalog.json");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SchemaCatalogService> _logger;
    private volatile IReadOnlyList<SchemaCatalogEntry> _schemas = [];

    public SchemaCatalogService(IHttpClientFactory httpClientFactory, ILogger<SchemaCatalogService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public int Count => _schemas.Count;

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("SchemaStore");
            using var response = await client.GetAsync(CatalogUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var catalog = await JsonSerializer.DeserializeAsync<SchemaCatalog>(stream, cancellationToken: cancellationToken);

            if (catalog?.Schemas is { Length: > 0 } schemas)
            {
                _schemas = schemas;
                _logger.LogInformation("Schema catalog refreshed: {Count} schemas loaded", schemas.Length);
            }
            else
            {
                _logger.LogWarning("Schema catalog response contained no schemas");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh schema catalog");
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<SchemaCatalogEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var schemas = _schemas;

        return schemas
            .Select(s => new { Entry = s, Score = CalculateScore(s, terms) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Entry)
            .ToList();
    }

    /// <inheritdoc />
    public SchemaCatalogEntry? FindByName(string nameOrPartial)
    {
        if (string.IsNullOrWhiteSpace(nameOrPartial))
            return null;

        var schemas = _schemas;

        // Exact match first (case-insensitive)
        var exact = schemas.FirstOrDefault(s =>
            s.Name.Equals(nameOrPartial, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact;

        // Partial match (case-insensitive contains)
        return schemas.FirstOrDefault(s =>
            s.Name.Contains(nameOrPartial, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyList<SchemaCatalogEntry> List(int limit, int offset)
    {
        var schemas = _schemas;
        return schemas.Skip(offset).Take(limit).ToList();
    }

    /// <inheritdoc />
    public async Task<string> GetSchemaContentAsync(string url, CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient("SchemaStore");
        return await client.GetStringAsync(url, cancellationToken);
    }

    private static int CalculateScore(SchemaCatalogEntry entry, string[] terms)
    {
        var score = 0;
        foreach (var term in terms)
        {
            if (entry.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 10;
            if (entry.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)
                score += 5;
            if (entry.FileMatch?.Any(f => f.Contains(term, StringComparison.OrdinalIgnoreCase)) == true)
                score += 3;
        }
        return score;
    }
}
