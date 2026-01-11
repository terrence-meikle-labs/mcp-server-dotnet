# mcp-server-dotnet

Small .NET 8 proof-of-concept showing a Model Context Protocol (MCP) server that exposes “tools” over HTTP and forwards calls to an internal API stub. It’s containerized with Docker Compose.

## What’s in this repo

- `Acme.McpServer` – MCP server.
  - HTTP mode: serves MCP at `POST /mcp` and health at `GET /healthz`.
  - STDIO mode: runs MCP over stdin/stdout (useful for desktop hosts like Claude Desktop).
  - Tools:
    - `search_items` – queries the internal API stub.
    - `get_my_org_summary` – calls the internal API stub.
  - Auth:
    - HTTP mode reads the JWT from `Authorization: Bearer ...`.
    - STDIO mode reads the JWT from `MCP_BEARER_TOKEN`.
- `Acme.InternalApiStub` – minimal HTTP API that simulates an internal service.
- `Acme.JwtMint` – tiny console app that mints a dev JWT matching the server’s Docker settings.
- `Acme.Contracts` – shared DTOs.

## Ports

When running via Compose:

- MCP server: `http://localhost:3004`
- Internal API stub (host-mapped): `http://localhost:5009`
- Internal API stub (container-to-container): `http://internal-api:5159`

## Run with Docker Compose

Prereqs:

- Docker Desktop (Linux containers)

Start:

```bash
docker compose up --build -d
```

Check:

```bash
docker compose ps
curl -sS http://localhost:3004/healthz
curl -sS http://localhost:5009/org/summary
```

Stop:

```bash
docker compose down
```

(Optional) remove the locally built images:

```bash
docker compose down --rmi local
```

## Run in STDIO mode (Claude Desktop / local)

`Acme.McpServer` chooses its transport by argument:

- With `--http`: runs the HTTP transport (used by Docker).
- Without `--http`: runs the STDIO transport.

Example (PowerShell):

```powershell
$env:MCP_BEARER_TOKEN = dotnet run --project .\Acme.JwtMint\Acme.JwtMint.csproj
dotnet run --project .\Acme.McpServer\Acme.McpServer.csproj
```

Note: STDIO mode still needs a reachable internal API base URL via configuration (e.g., `InternalApi:BaseUrl`).

## Call MCP tools (curl)

The MCP HTTP transport is JSON-RPC over `/mcp` and uses a session header.

1. Mint a JWT:

```bash
TOKEN=$(dotnet run --project ./Acme.JwtMint/Acme.JwtMint.csproj | tr -d '\r\n')
```

2. Initialize (captures `Mcp-Session-Id`):

```bash
SESSION=$(curl -sS -D - --max-time 10 http://localhost:3004/mcp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","clientInfo":{"name":"curl","version":"0.1"},"capabilities":{}}}' \
  | awk -F': ' 'tolower($1)=="mcp-session-id"{print $2}' | tr -d '\r')

echo "session=$SESSION"
```

3. List tools:

```bash
curl -sS --max-time 10 http://localhost:3004/mcp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Mcp-Session-Id: $SESSION" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
```

4. Call `search_items`:

```bash
curl -sS --max-time 10 http://localhost:3004/mcp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Mcp-Session-Id: $SESSION" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"search_items","arguments":{"query":"item-1","page":1,"pageSize":5}}}'
```

5. Call `get_my_org_summary`:

```bash
curl -sS --max-time 10 http://localhost:3004/mcp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Mcp-Session-Id: $SESSION" \
  -d '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"get_my_org_summary","arguments":{}}}'
```

## Notes

- `.dockerignore` is included to keep Docker build context small.
- `bin/`, `obj/`, and `.vs/` are intentionally not tracked. If you ever see `.vs` files that won’t delete, VS/VS Code may have them open; close the IDE and delete the folder.
