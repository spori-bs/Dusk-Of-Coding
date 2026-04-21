# 🛠️ Phase 28: Deployment Runbook Addition

**Role:** Senior DevOps Engineer & Technical Writer.
**Task:** The current Deployment Documentation is missing a concrete, command-line focused "Step-by-Step Runbook". Append a new section (4.1) to the existing markdown file detailing the exact execution steps for the three deployment scenarios.

**Language:** Hungarian prose with excellent grammar, but strict English for all technical terms (e.g., Application Pool, Connection String, Managed Identity, Scale to zero).

**Constraint (CRITICAL):** Do not rewrite the entire documentation. Output ONLY the new `## 4.1. Step-by-Step Runbooks` markdown section to be appended.

### Content Requirements for Section 4.1:

**A. Azure Cloud Deployment (Azure Container Apps)**
- Provide exact Azure CLI (`az`) commands for: 
  1. Creating a Resource Group and Azure Container Registry (ACR).
  2. Building and pushing the `WebAPI` and `TutorWorker` images using `az acr build`.
  3. Running the EF Core migration via CLI (`dotnet ef database update`).
  4. Creating the Container App.
- **Warning (What can break):** Highlight that the `TutorWorker` must have `--min-replicas 1` configured; otherwise, "Scale to zero" will kill the worker and RabbitMQ queues will build up.

**B. On-Premise Windows (IIS / Windows Service)**
- Provide exact PowerShell/CLI commands for:
  1. Publishing the projects (`dotnet publish -c Release`).
  2. IIS setup: Creating an Application Pool configured to "No Managed Code" (since Kestrel runs out-of-process) and assigning folder permissions.
  3. Creating and starting the TutorWorker as a Windows Service using `sc.exe create`.
- **Warning (What can break):** Mention that IIS AppPool Identity often lacks permissions to read the Windows Certificate Store or specific Environment Variables, which can cause JWT validation to fail with "Signature validation failed". Instruct to enable "Load User Profile = True".

**C. Docker Compose (Staging / Quick Deploy)**
- Detail the preparation: Creating an `.env` file for secrets (`DB_PASSWORD`, `GEMINI_API_KEY`).
- Provide the startup command (`docker-compose up -d --build`).
- **Warning (What can break):** Explain the risk of "Race conditions" during startup. Emphasize that the `docker-compose.yml` must use `depends_on` with `condition: service_healthy` to ensure MariaDB and RabbitMQ are fully ready before the WebAPI or Worker attempts to connect.