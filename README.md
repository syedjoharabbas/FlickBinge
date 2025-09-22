# FlickBinge

FlickBinge is a microservices-based movie discovery platform implemented with .NET 9. It demonstrates a small distributed-system architecture with separable APIs, infrastructure layers, a Blazor UI, an API gateway (YARP), inter-service messaging (RabbitMQ), resilience (Polly), and external integrations (OMDb, OpenAI via Semantic Kernel).

---

## Projects (high-level)
- `ApiGateway` — YARP reverse-proxy, central JWT authentication/authorization and routing to backend services.
- `FlickBinge.UI` — Blazor WebAssembly front-end that talks to the gateway/backends.
- `MovieService.Api` (+ Core + Infrastructure) — fetches movie data from OMDb and exposes movie endpoints.
- `RecommendationService.Api` (+ Core + Infrastructure) — builds recommendations using Microsoft Semantic Kernel (OpenAI) and is protected by resilience policies.
- `UserService.Api` (+ Core + Infrastructure) — user registration, auth (JWT), refresh tokens, and publishes `UserCreated` events to RabbitMQ.
- `WatchlistService.Api` (+ Core + Infrastructure) — per-user watchlists; subscribes to `UserCreated` events via RabbitMQ consumer and maintains watchlists in a database.

Each service follows the Core/Api/Infrastructure pattern: domain interfaces & models in Core, concrete implementations (EF, RabbitMQ, connectors) in Infrastructure, and minimal API in Api projects.

---

## Key features implemented
- JWT-based authentication and authorization with validation at startup (fail-fast when config missing).
- API Gateway (YARP) that routes requests to backend services and enforces authentication.
- RabbitMQ pub/sub:
  - `UserService` publishes `UserCreated` events (JSON) to `WatchlistQueue`.
  - `WatchlistService` runs a background consumer that creates a watchlist for new users.
- Resilience via Polly:
  - Retry + circuit-breaker policies applied to outbound HTTP (OMDb) and to Semantic Kernel calls.
  - Policies are centralized in a PolicyRegistry and applied via `AddPolicyHandlerFromRegistry` or executed from the registry.
- Structured logging via `ILogger<T>` across gateway, services, and RabbitMQ handlers.
- Blazor WebAssembly UI that calls the backend APIs (through the gateway in typical setups).
- Semantic Kernel connector for OpenAI-based recommendations (requires OpenAI API key).

---

## Prerequisites (local)
- .NET 9 SDK (preview may be used in this workspace)
- RabbitMQ running locally (default at `localhost:5672`) or a reachable RabbitMQ instance
- OMDb API key (for movie data)
- OpenAI API key (for recommendations)

Environment variables or configuration keys used by the services:
- `OMDb__ApiKey` or `OMDb:ApiKey` — OMDb API key (MovieService)
- `OpenAI__ApiKey` or `OpenAI:ApiKey` — OpenAI key (RecommendationService)
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` — JWT settings (UserService, ApiGateway expects them)
- Connection strings: `DefaultConnection` (UserService), `WatchlistConnection` (WatchlistService)

---

## Quick local run (development)
1. Start RabbitMQ locally (Docker example):
   - docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
2. Set secrets / environment variables (example PowerShell):
   - $env:OMDb__ApiKey = "your-omdb-key"
   - $env:OpenAI__ApiKey = "your-openai-key"
   - $env:Jwt__Key = "a-very-secret-key"
   - Set connection strings for the SQL Server instances used by services (or update appsettings).
3. From repo root — restore & build:
   - dotnet restore FlickBinge.sln
   - dotnet build FlickBinge.sln
4. Start services (in separate terminals or use an IDE launch profile):
   - ApiGateway
   - UserService.Api
   - MovieService.Api
   - RecommendationService.Api
   - WatchlistService.Api
   - FlickBinge.UI (Blazor WASM)

Ports (local dev launch settings):
- MovieService.Api: http://localhost:5001
- RecommendationService.Api: http://localhost:5002
- UserService.Api: http://localhost:5003
- WatchlistService.Api: http://localhost:5004
- ApiGateway: configured via `ReverseProxy` section in `appsettings.json` (routes to above)
- FlickBinge.UI: http://localhost:5022 (CORS configured in gateway)

---

## Resilience & Observability
- Retry + circuit-breaker policies protect outbound HTTP and AI calls. Policy parameters can be tuned in code or moved to configuration.
- Policy events (retry, break, reset, half-open) are logged with `ILogger` for visibility.
- Consider adding metrics (Prometheus) and a health endpoint to surface circuit state in production.

---

## Message contract (current)
- `UserCreated` event JSON shape published by `UserService`:
  {
    "EventType": "UserCreated",
    "UserId": "<GUID>"
  }

The Watchlist consumer expects this contract and creates an empty watchlist for the new user.

---

## Notes & next steps
- Move policy parameters into `appsettings.json` and bind them at startup for easier tuning.
- Add persistent fallback/caching strategies for degraded modes.
- Add Dockerfiles and docker-compose to bring up the full stack (RabbitMQ, SQL, and services).
- Add unit and integration tests around policy behavior and message flows.

---

## Contributing
1. Fork the repo
2. Create a feature branch
3. Make your changes and run tests
4. Open a PR describing the change

---

## License
MIT

---

## Docker (local development)

A docker-compose setup is provided to run the platform locally. By default the compose file brings up RabbitMQ and builds the service containers. SQL Server is expected to be provided externally (the compose file no longer starts MSSQL — update connection strings to point at your host DB).

Files added:
- `docker-compose.yml` — composes RabbitMQ and all services (builds service images from the repository)
- `Dockerfile` in each Api/UI project
- `.env` — environment variable placeholders (OMDb, OpenAI, JWT)

Quick start (example):
1. Edit `.env` and set your keys and values (OMDB_API_KEY, OPENAI_API_KEY, JWT_KEY, JWT_ISSUER, JWT_AUDIENCE).
2. Ensure your SQL Server is accessible from containers (use `host.docker.internal` on Windows) and appsettings for `UserService.Api` and `WatchlistService.Api` point to that DB.
3. Build and run the stack:
   ```powershell
   docker-compose up -d --build
   ```
4. Monitor logs:
   ```powershell
   docker-compose logs -f movie-service
   docker-compose logs -f user-service
   ```
5. Tear down:
   ```powershell
   docker-compose down
   ```

Notes:
- If you prefer the compose file to provide SQL Server too, I can re-enable it and add healthchecks and data volumes.
- For production use, create proper Docker images, add secrets management, and ensure hardened database credentials.
