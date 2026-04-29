# SchemaStore MCP Server

An MCP (Model Context Protocol) server for [SchemaStore.org](https://www.schemastore.org/) — search, browse, and retrieve JSON schemas from the SchemaStore catalog.

Built with .NET 10 and the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## Quick Start — Use the Hosted Server

The SchemaStore MCP Server is publicly hosted at **<https://mcp.schemastore.org**.> No local setup required.

### Visual Studio

Add a `.mcp.json` file to your solution root (or add it via **Copilot Chat** → **Select Tools** → **+**):

```json
{
  "inputs": [],
  "servers": {
    "SchemaStore": {
      "url": "https://mcp.schemastore.org/",
      "type": "http",
      "headers": {}
    }
  }
}
```

### Visual Studio Code

Create `.vscode/mcp.json` in your workspace (or use Command Palette → `MCP: Add Server`):

```json
{
  "servers": {
    "SchemaStore": {
      "url": "https://mcp.schemastore.org/",
      "type": "http"
    }
  }
}
```

### Any MCP Client

Point your client at `https://mcp.schemastore.org/` using HTTP transport.

---

## Tools

| Tool            | Description                                                                                    |
| --------------- | ---------------------------------------------------------------------------------------------- |
| `searchSchemas` | Search the catalog by name, description, or file pattern. Returns results ranked by relevance. |
| `getSchema`     | Get the full JSON Schema document by name (case-insensitive partial match).                    |
| `listSchemas`   | List all schemas with pagination (default: 50 per page).                                       |

## Running the Server

```bash
cd src/SchemaStoreMcpServer
dotnet run
```

The server starts on `http://localhost:5189` by default (see `Properties/launchSettings.json`).

- **MCP endpoint**: `POST /` (Streamable HTTP transport)
- **Legacy SSE**: `GET /sse` + `POST /message`
- **Health check**: `GET /health`

## Adding the MCP Server to Your Editor

### Visual Studio (Solution-scoped)

Add a `.mcp.json` file to your solution root:

```json
{
  "inputs": [],
  "servers": {
    "SchemaStoreMcpServer": {
      "url": "http://localhost:5189/",
      "type": "http",
      "headers": {}
    }
  }
}
```

Or use the UI: **GitHub Copilot Chat** → **Select Tools** (wrench icon) → **+** → set Type to `http` and URL to `http://localhost:5189/`.

### Visual Studio Code

Create `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "SchemaStoreMcpServer": {
      "url": "http://localhost:5189/",
      "type": "http"
    }
  }
}
```

Or use the Command Palette: `MCP: Add Server` → HTTP → `http://localhost:5189/` → give it a name.

### Any MCP Client (stdio transport alternative)

If you prefer stdio transport (no need to keep a server running), point your client at:

```json
{
  "servers": {
    "SchemaStoreMcpServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "path/to/src/SchemaStoreMcpServer/SchemaStoreMcpServer.csproj"]
    }
  }
}
```

> **Note:** The stdio transport requires adding `.WithStdioServerTransport()` to `Program.cs` instead of `.WithHttpTransport()`.

## Testing with GitHub Copilot

1. Start the server (`dotnet run`)
2. Open GitHub Copilot Chat in **Agent mode**
3. Click the **Select Tools** icon — you should see the three schema tools listed
4. Try a prompt like: *"Search SchemaStore for Docker-related schemas"*

## Testing with .http Files

See the [`test/mcp-session.http`](test/mcp-session.http) file for manual HTTP testing in Visual Studio.

**Workflow:**

1. Run the server
2. Execute the `initialize` POST request
3. Copy the `Mcp-Session-Id` from the response headers
4. Paste it into `@sessionId = ...`
5. Execute the tool call requests

## Architecture

```txt
src/SchemaStoreMcpServer/
├── Program.cs                      # DI, MCP server, health checks, HTTP transport
├── Models/
│   └── SchemaCatalogEntry.cs       # DTOs (name, description, fileMatch, url)
├── Services/
│   ├── ISchemaCatalogService.cs    # Interface
│   └── SchemaCatalogService.cs     # Singleton: fetch, search, lookup, pagination
├── BackgroundServices/
│   └── CatalogRefreshService.cs    # Refreshes catalog every 30 minutes
└── Tools/
    └── SchemaTools.cs              # MCP tool definitions
```

- The schema catalog is fetched from `https://www.schemastore.org/api/json/catalog.json` at startup and refreshed every 30 minutes.
- Search uses multi-term scoring across name (×10), description (×5), and fileMatch (×3).
- Thread-safe catalog refresh via volatile reference swap.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
