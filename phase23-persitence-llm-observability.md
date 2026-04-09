# 🛠️ Phase 23: Production Persistence & LLM Observability

## 🎯 Role & Objective

**Role:** Senior .NET Architect (.NET 10, Aspire, EF Core).

**Objective:** Execute a foundational infrastructure shift. Replace the development SQLite database with a production-ready **MariaDB** container orchestrated via **.NET Aspire**. Concurrently, implement an **LLM Observability** layer by creating a telemetry table that utilizes MariaDB's JSON capabilities to store raw LLM prompts and responses without degrading transactional performance.

---

## 🏗️ Part 1: .NET Aspire Orchestration

### Step 1: AppHost Configuration
Modify the `Program.cs` in your `.AppHost` project to orchestrate MariaDB.
```csharp
var mariadb = builder.AddMariaDb("dusk-database")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var apiService = builder.AddProject<Projects.DuskOfCoding_WebApi>("webapi")
    .WithReference(mariadb);

var workerService = builder.AddProject<Projects.DuskOfCoding_TutorWorker>("worker")
    .WithReference(mariadb);
```

---

## 🗄️ Part 2: EF Core Migration (SQLite to MariaDB)

### Step 1: Provider Switch
1. Remove `Microsoft.EntityFrameworkCore.Sqlite` packages from the Infrastructure/API projects.
2. Install `Pomelo.EntityFrameworkCore.MySql` (or the official equivalent for .NET 10/Aspire).
3. Update the `DbContext` registration in your `Program.cs` or DI extensions to use MySQL/MariaDB:
   ```csharp
   var connectionString = builder.Configuration.GetConnectionString("dusk-database");
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
   ```

### Step 2: Clean Slate Migrations
Since you are switching database engines, existing SQLite migrations will fail.
1. Delete the existing `Migrations` folder in your Infrastructure project.
2. Drop the old SQLite `.db` file.

---

## 📡 Part 3: LLM Observability (Telemetry)

### Step 1: Domain Entity
Create the telemetry entity in `DuskOfCoding.Domain.Entities`:
```csharp
public class LlmTelemetryLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public Guid? UserId { get; init; }
    public Guid? TaskId { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public int TokenCount { get; init; }
    public bool IsSuccess { get; init; }
    
    // Unindexed "Dump" column for raw request/response data
    public string Payload { get; init; } = string.Empty; 
}
```

### Step 2: DbContext Integration
Add `DbSet<LlmTelemetryLog> LlmTelemetryLogs { get; set; }` to your `ApplicationDbContext`. Ensure no indexes are placed on the `Payload` column to maintain fast append-only performance. 
Generate the new Initial Migration for MariaDB (`dotnet ef migrations add InitialMariaDb`).

### Step 3: Worker Integration (Fire-and-Forget Logging)
In your `TutorWorker` (where you handle the `GenerateTestSuiteCommand` from RabbitMQ):
- After interacting with the `IChatClient`, create an `LlmTelemetryLog` record.
- Serialize the exact system prompt, user prompt, and raw LLM text response into a single JSON object.
- Assign this JSON string to the