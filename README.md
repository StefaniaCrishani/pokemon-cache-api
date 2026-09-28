# Pokemon Cache API

An ASP.NET Core (.NET 8) Web API that fetches data from a public third-party API
([PokeAPI](https://pokeapi.co)), caches it in SQL Server, and serves it back through
a small REST API — with the database used as a local cache in front of the
third-party API.

## Contents

- [Why PokeAPI](#why-pokeapi)
- [Architecture](#architecture)
- [Why these libraries](#why-these-libraries-requirement-4)
- [Endpoints](#endpoints)
- [Caching behaviour](#caching-behaviour-requirement-3)
- [Project structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Running it](#running-it)
- [Environment variables](#environment-variables-requirement-9)
- [Error handling](#error-handling)
- [Assumptions & possible follow-ups](#assumptions--possible-follow-ups)

## Why PokeAPI

[PokeAPI](https://pokeapi.co/docs/v2) is free, requires **no API key**, and exposes
exactly the shape this assignment needs:

- a paged **list** endpoint (`GET /pokemon?limit=&offset=`)
- a **get-by-id-or-name** endpoint (`GET /pokemon/{idOrName}`)

That maps directly onto the two required endpoints without inventing an artificial
"record" type.

## Architecture

Classic layered structure, one direction of dependency (Controller → Service →
Repository / External client):

```
Controller  →  PokemonService  →  IPokemonRepository (SQL Server, raw ADO.NET)
                               →  IPokeApiClient      (HttpClient → PokeAPI)
```

- **Controllers/** — thin HTTP layer, only request validation and status codes.
- **Services/PokemonService** — owns the cache-first business rule (requirement #3).
- **Services/PokeApiClient** — talks to PokeAPI over `HttpClient`, maps transport
  failures into a typed `UpstreamApiException`.
- **Data/PokemonRepository** — all SQL lives here, parameterised `SqlCommand`
  calls against SQL Server via `Microsoft.Data.SqlClient`. No ORM, per requirement #5.
- **Middleware/ExceptionHandlingMiddleware** — one place that turns exceptions into
  consistent JSON error responses (see [Error handling](#error-handling)).

Everything is wired through interfaces (`IPokemonRepository`, `IPokeApiClient`,
`IPokemonService`) and constructor injection, so each layer can be unit tested with
the others mocked out.

## Why these libraries (requirement #4)

| Library | Why |
|---|---|
| `Microsoft.Data.SqlClient` | Microsoft's actively maintained SQL Server driver. Used directly with parameterised `SqlCommand`s — **not** an ORM — to satisfy requirement #5. |
| `Swashbuckle.AspNetCore` | Generates a Swagger UI (`/swagger`) so the two endpoints are easy to explore, demo and grade without a separate REST client. Dev-only. |

Everything else (`HttpClient`, `System.Text.Json`, ASP.NET Core MVC/Controllers) is
part of the framework — no extra third-party dependency was needed for HTTP calls
or JSON handling.

## Endpoints

### `GET /api/pokemon?limit=20&offset=0`
Returns a page of cached records. `limit` (1–100, default 20) and `offset`
(default 0) are optional query parameters.

```bash
curl "http://localhost:5080/api/pokemon?limit=5&offset=0"
```

### `GET /api/pokemon/{idOrName}`
Returns a single Pokemon by numeric id or by name (case-insensitive).

```bash
curl http://localhost:5080/api/pokemon/25
curl http://localhost:5080/api/pokemon/pikachu
```

Sample response:

```json
{
  "id": 25,
  "name": "pikachu",
  "heightDecimetres": 4,
  "weightHectograms": 60,
  "types": ["electric"],
  "imageUrl": "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/25.png",
  "source": "api"
}
```

`source` is `"api"` the first time a record is fetched, and `"cache"` on every
subsequent request for the same record — this is deliberately included so the
caching behaviour is visible/verifiable, not just described.

Swagger UI is available at `http://localhost:5080/swagger` when running in the
Development environment.

## Caching behaviour (requirement #3)

- **`GET /api/pokemon/{idOrName}`** — checks the database first. On a hit, returns
  it straight from SQL Server. On a miss, calls PokeAPI, saves the result, then
  returns it.
- **`GET /api/pokemon`** — serves whatever is already cached in the database, paged
  and ordered by id. The very first call (empty cache) has nothing to page through
  yet, so it bootstraps by fetching that page's records from PokeAPI, caching each
  one, and then reading the same page back from the database — so the read path is
  identical either way, and every later call for that range is a pure cache read.

A unique index on `Name` plus a catch around SQL Server's duplicate-key error
(`PokemonRepository.InsertIfNotExistsAsync`) makes the insert idempotent, so two
concurrent requests racing to cache the same new Pokemon don't crash — the second
insert is simply skipped.

## Project structure

```
PokemonCacheApi/
├── PokemonCacheApi.sln
├── docker-compose.yml
├── database/
│   └── schema.sql
└── src/PokemonCacheApi/
    ├── Program.cs
    ├── appsettings.json
    ├── Controllers/
    │   └── PokemonController.cs
    ├── Services/
    │   ├── IPokemonService.cs / PokemonService.cs
    │   └── IPokeApiClient.cs / PokeApiClient.cs
    ├── Data/
    │   ├── IPokemonRepository.cs / PokemonRepository.cs
    │   └── IDbConnectionFactory.cs / SqlConnectionFactory.cs
    ├── Models/
    │   ├── PokemonRecord.cs
    │   ├── Dtos/            (API response shapes)
    │   └── External/        (PokeAPI response shapes)
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs
    └── Exceptions/
        └── UpstreamApiException.cs
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Docker (for the easiest way to run SQL Server), **or** any SQL Server instance
  (LocalDB, a full install, or Azure SQL) you already have.

## Running it

### 1. Start SQL Server

```bash
docker compose up -d
```

This starts SQL Server 2022 on `localhost:1433` with `sa` / `YourStrong!Passw0rd`
(matches the connection string in `appsettings.json` — change both together if you
use a different password).

If you're pointing at your own SQL Server instance instead, skip this step and
just update the connection string (see [Environment variables](#environment-variables-requirement-9)).

### 2. Create the database and table

```bash
docker exec -i pokemon-cache-sqlserver \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong!Passw0rd' -C \
  < database/schema.sql
```

(Using `sqlcmd` directly instead? `sqlcmd -S localhost -U sa -P 'YourStrong!Passw0rd' -C -i database/schema.sql`.)

### 3. Run the API

```bash
cd src/PokemonCacheApi
dotnet restore
dotnet run
```

The API starts on `http://localhost:5080` (see `Properties/launchSettings.json`).
Swagger UI opens automatically at `http://localhost:5080/swagger`.

### 4. Try it

```bash
curl http://localhost:5080/api/pokemon/pikachu
curl "http://localhost:5080/api/pokemon?limit=10"
```

## Environment variables (requirement #9)

PokeAPI itself needs **no API key**, so nothing is required there.

The one setting you're likely to want to override outside of `appsettings.json` is
the database connection string. ASP.NET Core lets any configuration key be
supplied as an environment variable by replacing `:` with `__`:

```bash
export ConnectionStrings__PokemonDb="Server=localhost,1433;Database=PokemonCacheDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
dotnet run
```

If you were swapping in an API that *does* require a key, the same pattern would
apply, e.g. `export PokeApi__ApiKey="..."` and reading it via
`builder.Configuration["PokeApi:ApiKey"]` in `Program.cs`.

## Error handling

`ExceptionHandlingMiddleware` wraps every request and maps exceptions to a
consistent `application/problem+json` body:

| Situation | Status |
|---|---|
| PokeAPI unreachable / times out | `502 Bad Gateway` |
| SQL Server error | `500 Internal Server Error` |
| Anything else unexpected | `500 Internal Server Error` |
| Pokemon not found (by design, not an exception) | `404 Not Found` |
| Invalid `limit`/`offset` | `400 Bad Request` |

Controllers never leak raw exception messages or stack traces to the client;
details are logged server-side instead.

## Assumptions & possible follow-ups

- **Record type**: the assignment leaves the record type open, so this uses
  Pokemon since PokeAPI's shape fits the two-endpoint requirement well.
- **Repository workflow**: this repository was produced as a single deliverable
  rather than incrementally, so it doesn't show the requested "commit as you go"
  history. If you're picking this up to submit, `git init` it, push it to a fresh
  GitHub repo, and make a few small real commits (e.g. schema → repository layer →
  service/controller → README) so the history reflects incremental work.
- **Tests**: given the time box, unit tests were left out in favour of a complete,
  working end-to-end slice. `PokemonService`, `IPokemonRepository` and
  `IPokeApiClient` are all interface-based specifically so they can be unit tested
  with mocks later (e.g. with xUnit + Moq) without changing the design.
- **Pagination beyond the cache**: `GET /api/pokemon` pages over what's cached
  locally, not over PokeAPI's full ~1300-entry catalogue. Requesting an
  `offset` past what's ever been cached returns an empty page rather than
  reaching further into PokeAPI — a reasonable interpretation of "list of
  records" for a caching exercise, but worth calling out.
