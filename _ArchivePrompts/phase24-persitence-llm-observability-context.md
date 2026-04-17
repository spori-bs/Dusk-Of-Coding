# Phase 24 Context: Production Persistence & LLM Observability

## 1. AppHost Configuration
**Configuration target:** `DuskOfCoding.AppHost/AppHost.cs`
- Currently, `webapi` (`Projects.DuskOfCoding_WebApi`) and `tutorworker` (`Projects.DuskOfCoding_TutorWorker`) are registered.
- Goal: Orchestrate MariaDB by inserting `builder.AddMariaDb("dusk-database").WithDataVolume().WithLifetime(ContainerLifetime.Persistent)`.
- Pass `.WithReference(mariadb)` to both `webApi` and `tutorWorker`.

## 2. EF Core Migration (SQLite to MariaDB)
**Project File:** `DuskOfCoding.Infrastructure/DuskOfCoding.Infrastructure.csproj`
- Package Operations: Remove `Microsoft.EntityFrameworkCore.Sqlite`. Add `Pomelo.EntityFrameworkCore.MySql` (version compatible with .NET 10).

**Registration:** `DuskOfCoding.Infrastructure/DependencyInjection.cs`
- Change `services.AddDbContext<AppDbContext>(options => options.UseSqlite(...))` to `options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))`.

**Migrations Cleanup:** 
- Delete `DuskOfCoding.Infrastructure/Migrations` folder.
- Delete `DuskOfCoding.db` file.
- Generate new `InitialMariaDb` migration via `dotnet ef migrations add`.

## 3. LLM Observability implementation
**Domain Entity:** Create `DuskOfCoding.Domain/Entities/LlmTelemetryLog.cs` with the specified 7 properties.

**DbContext Integration:** Add `DbSet<LlmTelemetryLog> LlmTelemetryLogs` to `DuskOfCoding.Infrastructure/Persistence/AppDbContext.cs`. Ensure no index is placed on `Payload` to maintain append-only high performance.

**Worker Integration:** `DuskOfCoding.TutorWorker/TestGenerationWorkerService.cs`
- `InvokeLlmAsync` handles the sequence generation.
- After receiving the `response` from `_chatClient.GetResponseAsync`, serialize the transaction (System prompt, User prompt, AI raw text) to a JSON string.
- Create an `LlmTelemetryLog` entity, populate its schema and payload, and persist it either inline via the same disconnected `AppDbContext` currently used in `HandleCommandAsync` or a new fire-and-forget scope.
