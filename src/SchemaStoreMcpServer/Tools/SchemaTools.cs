namespace SchemaStoreMcpServer.Tools;

using System.ComponentModel;
using ModelContextProtocol.Server;
using SchemaStoreMcpServer.Models;
using SchemaStoreMcpServer.Services;

/// <summary>
/// MCP tools for interacting with the SchemaStore.org JSON Schema catalog.
/// </summary>
[McpServerToolType]
public sealed class SchemaTools
{
    /// <summary>
    /// Searches the SchemaStore catalog for schemas matching a query string.
    /// The search performs fuzzy matching against schema names, descriptions, and file patterns.
    /// Returns a list of matching schemas ordered by relevance.
    /// </summary>
    /// <param name="query">
    /// The search query to match against schema names and descriptions.
    /// Supports multiple space-separated terms (all terms contribute to relevance scoring).
    /// Examples: "docker", "github actions", "typescript config"
    /// </param>
    /// <param name="catalogService">The schema catalog service (injected).</param>
    /// <returns>A list of matching schema entries with name, description, url, and fileMatch.</returns>
    [McpServerTool(Name = "searchSchemas"), Description("Search the SchemaStore.org catalog for JSON schemas by name, description, or file pattern. Returns matching schemas ordered by relevance.")]
    public static IReadOnlyList<SchemaCatalogEntry> SearchSchemas(
        [Description("Search query to match against schema names, descriptions, and file patterns (e.g. 'docker', 'github actions', 'eslint')")] string query,
        ISchemaCatalogService catalogService)
    {
        return catalogService.Search(query);
    }

    /// <summary>
    /// Retrieves the full JSON Schema document for a specific schema by name.
    /// First finds the schema in the catalog using case-insensitive partial matching,
    /// then fetches and returns the complete JSON Schema from the schema's URL.
    /// </summary>
    /// <param name="nameOrPartial">
    /// The name (or partial name) of the schema to retrieve.
    /// Uses case-insensitive matching; exact matches are preferred over partial matches.
    /// Examples: "package.json", "tsconfig", "Docker"
    /// </param>
    /// <param name="catalogService">The schema catalog service (injected).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full JSON Schema content as a string, or an error message if not found.</returns>
    [McpServerTool(Name = "getSchema"), Description("Get the full JSON Schema document for a specific schema by name. Performs case-insensitive partial name matching and returns the complete schema JSON.")]
    public static async Task<string> GetSchema(
        [Description("Schema name or partial name to look up (e.g. 'package.json', 'tsconfig', 'Docker')")] string nameOrPartial,
        ISchemaCatalogService catalogService,
        CancellationToken cancellationToken = default)
    {
        var entry = catalogService.FindByName(nameOrPartial);
        if (entry is null)
            return $"No schema found matching '{nameOrPartial}'. Try using searchSchemas to find the correct name.";

        return await catalogService.GetSchemaContentAsync(entry.Url, cancellationToken);
    }

    /// <summary>
    /// Returns a paginated list of all schemas in the SchemaStore catalog.
    /// Only returns metadata (name, description, url, fileMatch) — not the full schema content.
    /// Use getSchema to retrieve the full schema document for a specific entry.
    /// </summary>
    /// <param name="limit">Maximum number of schemas to return (1–200). Defaults to 50.</param>
    /// <param name="offset">Number of schemas to skip for pagination. Defaults to 0.</param>
    /// <param name="catalogService">The schema catalog service (injected).</param>
    /// <returns>A paginated list of schema catalog entries.</returns>
    [McpServerTool(Name = "listSchemas"), Description("List all available JSON schemas from SchemaStore.org with pagination. Returns metadata only (name, description, url, fileMatch). Use getSchema to fetch full schema content.")]
    public static ListSchemasResult ListSchemas(
        [Description("Maximum number of schemas to return (1-200, default: 50)")] int? limit,
        [Description("Number of schemas to skip for pagination (default: 0)")] int? offset,
        ISchemaCatalogService catalogService)
    {
        var actualLimit = Math.Clamp(limit ?? 50, 1, 200);
        var actualOffset = Math.Max(offset ?? 0, 0);

        var schemas = catalogService.List(actualLimit, actualOffset);
        return new ListSchemasResult(schemas, catalogService.Count, actualLimit, actualOffset);
    }
}

/// <summary>
/// Result object for the listSchemas tool, including pagination metadata.
/// </summary>
public sealed record ListSchemasResult(
    IReadOnlyList<SchemaCatalogEntry> Schemas,
    int TotalCount,
    int Limit,
    int Offset);
