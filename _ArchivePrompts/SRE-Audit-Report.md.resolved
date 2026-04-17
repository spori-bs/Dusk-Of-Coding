# 🔴 SRE Red Team Audit Report — Dusk of Coding Dokumentáció

**Auditor**: Principal SRE / Red Team  
**Dátum**: 2026-04-17  
**Scope**: `Documentation-Deployment-and-Operations-EN.md` + `Documentation-Deployment-and-Operations-HU.md`  
**Kódbázis cross-reference**: ✅ Forráskód-elemzés végrehajtva

---

## Phase 1: Security & Identity Audit (Keycloak/OIDC Threat Model)

### 1.1 — IIS Reverse Proxy: Hiányzó Forwarded Headers konfiguráció

> [!CAUTION]
> **KRITIKUS SEBEZHETŐSÉG**: A Runbook B szekciója megemlíti az `X-Forwarded-For` és `X-Forwarded-Proto` header-ek forwarding-ját az IIS Reverse Proxy-ban, de **egyetlen konkrét IIS/ARR konfigurációs lépést sem dokumentál**.

**Mi fog eltörni:**
- A `ForwardedHeaders` middleware **nincs regisztrálva** a WebAPI `Program.cs`-ben (kódelemzéssel megerősítve — a `grep` nulla találatot adott `ForwardedHeaders`-re). Ez azt jelenti, hogy az ASP.NET Core alkalmazás a Kestrel-ről érkező kéréseket a proxy belső IP-jéről érkező HTTP kérésként fogja látni, nem HTTPS-ként.
- A Keycloak OIDC callback URI (`redirect_uri`) ebből kifolyólag `http://` séma-val fog generálódni, miközben a Keycloak kliens konfigurációja `https://` URI-t vár. **Eredmény: végtelen redirect loop vagy `invalid_redirect_uri` hiba**.

**Hiányzó lépések a Runbook-ból:**
1. **ASP.NET Core `ForwardedHeaders` middleware regisztrálása** a `Program.cs`-ben:
   ```csharp
   builder.Services.Configure<ForwardedHeadersOptions>(options =>
   {
       options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
       options.KnownNetworks.Clear();
       options.KnownProxies.Clear();
   });
   // ...
   app.UseForwardedHeaders(); // MUST be BEFORE UseAuthentication()
   ```
2. **IIS ARR "Reverse rewrite host in response headers"** — a Runbook nem említi, hogy az ARR-ben engedélyezni kell az `HTTP_X_FORWARDED_PROTO` Server Variable-t az URL Rewrite Rule-ban. Enélkül az IIS nem küldi tovább a proto header-t.
3. **IIS `web.config` Server Variables hozzáadása** — szükséges a `ORIGINAL_URL`, `HTTP_X_FORWARDED_HOST`, `HTTP_X_FORWARDED_PROTO` engedélyezése.

### 1.2 — Azure Container Apps: Keycloak Internal vs. External URL mismatch

> [!WARNING]
> A `ValidIssuers` lista a `WebApi/Program.cs`-ben hardcoded fejlesztési értékeket tartalmaz (`localhost:8080`, `keycloak:8080`, `https+http://keycloak`). Ezek **nem érvényesek Azure Container Apps környezetben**.

**Mi fog eltörni (Azure-on):**
- Az Azure Container Apps-ben a Keycloak egy saját Container App-ként fut, a belső FQDN `keycloak.internal.<env>.azurecontainerapps.io` formátumú lesz, de a JWT `iss` claim a **publikus URL-t** fogja tartalmazni (pl. `https://keycloak.myapp.azurecontainerapps.io/realms/DuskOfCoding`).
- Ha a `Keycloak__Authority` belső hálózati URL-re mutat (backchannel metadata discovery), de a JWT-ben a külső `iss` van, a token validation sikertelen lesz `IDX10205: Issuer validation failed` hibával.
- **A Runbook egyáltalán nem említi**, hogy az Azure-ban szükség van a `ValidIssuers` lista frissítésére az aktuális ingress FQDN-nel, vagy a `ValidateIssuer = false` beállítása szükséges (ami viszont biztonsági kockázat).

**Hiányzó konfiguráció:**
- A Keycloak Container App-nál kötelező az **EXTERNAL ingress URL** beállítása a `KC_HOSTNAME` environment variable-ben, hogy a kibocsátott tokenek `iss` claimje egyezzen a `ValidIssuers`-ben konfigurált értékkel.
- A WebAPI-ban a `Keycloak__Authority` **backchannel** URL-t kell kapjon (internal FQDN), míg a `ValidIssuers` a **frontchannel** (public) URL-t.

### 1.3 — `ValidateAudience = false` biztonsági kockázat

> [!WARNING]
> A `WebApi/Program.cs` 76. sorában: `ValidateAudience = false` — a komment szerint ez "POC" döntés. A dokumentáció **nem említi**, hogy ez production-be kerülés előtt KÖTELEZŐEN javítandó.

Ha egy kompromittált vagy rosszhiszemű Keycloak kliens access token-t szerez (akár egy másik realm kliensétől), az **bármely audience-ra** érvényes lesz, és a WebAPI elfogadja. Ez egy privilege escalation vektor.

---

## Phase 2: System Resilience & Deadlock Audit (RabbitMQ/Worker Flow)

### 2.1 — Dead Letter Queue (DLQ) teljes hiánya

> [!CAUTION]
> **KRITIKUS ARCHITEKTÚRA-HIBA**: A `RabbitMQService.cs` kódjában megvizsgálva egyetlen queue sem rendelkezik Dead Letter Exchange (DLX) konfigurációval. A `QueueDeclareAsync` hívások nem tartalmaznak `x-dead-letter-exchange` vagy `x-dead-letter-routing-key` argumentumot.

**Mi fog történni Gemini totalitás outage esetén:**
1. A TutorWorker megpróbálja feldolgozni az üzenetet → a Polly retry (max 3 attempt, exponential backoff) lefut → mind sikertelen.
2. Az exception handler a `ConsumeAsync`-ben **`requeue: true`**-val visszadobja az üzenetet a queue-ba (222. sor).
3. Az üzenet **azonnal újra kézbesítésre kerül** → újabb 3 retry → újra requeue → **végtelen poison message loop**.
4. A RabbitMQ CPU-használata 100%-ra ugrik, a queue mérete exponenciálisan nő, és **az összes queue blokkolt állapotba kerülhet** (memory alarm).

**A Runbook nem említi, hogy:**
- Szükséges egy DLX exchange + DLQ queue konfigurálása a `DeclareTopologyAsync`-ben:
  ```csharp
  var queueArgs = new Dictionary<string, object?>
  {
      { "x-dead-letter-exchange", "practice.dlx" },
      { "x-dead-letter-routing-key", "deadletter" },
      { "x-message-ttl", 300000 } // 5 perc TTL retry-k között
  };
  ```
- A `ConsumeAsync` `catch` blokkjában a `requeue: true` →  `requeue: false`-ra kellene változnia, ha a retry count egy határt elért (vagy DLX-re irányítva).

### 2.2 — Container Memory Limits hiánya (Docker Compose / Roslyn Sandbox)

> [!WARNING]
> A Runbook C szekciója nem említi a `mem_limit` / `deploy.resources.limits.memory` beállítást a `docker-compose.yml`-ben.

**Mi fog történni:**
- A Roslyn Compilation API (amely a WebAPI / ExecutionApi processzen belül fut) **tetszőleges mennyiségű memóriát allokálhat** egy rosszindulatú vagy hibás submission kompilálása közben.
- Egy egyetlen `while(true) { var x = new byte[1024*1024]; }` típusú submission **az egész Docker host memóriáját kiéhezítteti**, amivel:
  - A RabbitMQ OOM-killer áldozata lesz
  - A MariaDB/SQL Server processz terminálódik
  - A Keycloak leáll → **teljes SPOF kaszkád**

**A Runbook-ból hiányzik:**
```yaml
services:
  webapi:
    deploy:
      resources:
        limits:
          memory: 512M
  executionapi:
    deploy:
      resources:
        limits:
          memory: 256M  # LEGKRITIKUSABB — itt fut a Roslyn sandbox
  tutorworker:
    deploy:
      resources:
        limits:
          memory: 512M
```

### 2.3 — `--min-replicas 1` nem elegendő — Queue backpressure kezelés hiánya

A Runbook helyesen figyelmeztet a `--min-replicas 1` szükségességére, de **nem kezeli az ellentétes forgatókönyvet**: mi történik, ha a Worker 1 replikával van, de a Gemini API lassú (30+ másodperces válaszidő)?

- A `prefetchCount: 1` beállítás (192. sor) azt jelenti, hogy **egy időben egyetlen üzenetet** dolgoz fel a Worker.
- Ha a Gemini válaszideje 30 másodperc, és 100 submission érkezik percenként, a queue **mérete percenként ~98 üzenettel nő**.
- A Runbook nem említi a KEDA (Kubernetes Event-Driven Autoscaling) konfigurációját Azure Container Apps kontextusban, pedig a 4. szekció K8s-nél utal rá.

---

## Phase 3: „Mi Hiányzik?" — 3 Kritikus Hiányzó Parancs/Konfiguráció

> [!IMPORTANT]
> Az alábbi 3 elem **garantáltan megakaszt egy Junior DevOps mérnököt egy hajnali 3 órás deployment során**.

### ❌ 1. Hiányzó: Health Check Endpoint-ok Production-ban LETILTVA

A `ServiceDefaults/Extensions.cs` 130. sora:
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapHealthChecks(HealthEndpointPath);
    app.MapHealthChecks(AlivenessEndpointPath, ...);
}
```

**A health check endpoint-ok KIZÁRÓLAG Development környezetben vannak regisztrálva.** Production-ban a `/health` és `/alive` endpoint-ok **404-et adnak vissza**.

Ez azt jelenti, hogy:
- Az Azure Container Apps health probe **mindig sikertelen** lesz → a Container App restart-loop-ba kerül
- A Docker Compose `depends_on: condition: service_healthy` **soha nem fog teljesülni** → deadlock induláskor
- Az IIS Application Request Routing health monitoring nem fog működni

**A Runbook egyik szekciója sem említi, hogy az `ASPNETCORE_ENVIRONMENT` environment variable-t `Production`-ről módosítani kell, VAGY a kódban a health check regisztrációt ki kell venni az `if (IsDevelopment())` blokkból.**

Ez a **legsúlyosabb dokumentáció-hiba az egész Runbook-ban**.

---

### ❌ 2. Hiányzó: `dotnet ef` Tool telepítési parancs a Runbook-ból

A Runbook A szekciója az alábbi parancsot tartalmazza:
```bash
dotnet ef database update --project DuskOfCoding.Infrastructure --startup-project DuskOfCoding.WebApi
```

De a `dotnet ef` **NEM része a .NET SDK alaptelepítésnek**. A Junior DevOps mérnök a következő hibaüzenetet fogja kapni:
```
Could not execute because the specified command or file was not found.
```

**Hiányzó parancs (a migrációs lépés ELŐTT kell szerepelnie):**
```bash
dotnet tool install --global dotnet-ef
# VAGY a CI/CD pipeline-ban:
dotnet tool restore  # ha van tool-manifest (.config/dotnet-tools.json)
```

---

### ❌ 3. Hiányzó: Blazor UI Container Image build + push (Azure Runbook)

Az Azure Runbook A szekciója **kizárólag** a `webapi` és `tutorworker` image-ek build-jét és push-ját dokumentálja:
```bash
az acr build --registry MyRegistry --image webapi:latest ./DuskOfCoding.WebApi
az acr build --registry MyRegistry --image tutorworker:latest ./DuskOfCoding.TutorWorker
```

**A Blazor WebUI image teljesen hiányzik.** A C4 Architecture diagram egyértelműen mutatja, hogy a Blazor UI egy különálló Container — de a Runbook nem tartalmazza a build/push lépését:
```bash
az acr build --registry MyRegistry --image webui:latest ./DuskOfCoding.WebUi
```

Enélkül: az `az containerapp create` a `webui` image-re hivatkozva `ImagePullError` / `ImageNotFound` hibát fog kapni az ACR-ből, és a Container App **nem indul el**. Hajnali 3-kor ez minimum 30 perc debuggolás, amíg rájönnek, hogy egyszerűen nincs felpusholva az image.

---

## Összefoglaló Mátrix

| # | Kategória | Súlyosság | Runbook Szekció | Hatás |
|---|-----------|-----------|-----------------|-------|
| 1.1 | `ForwardedHeaders` hiánya | 🔴 KRITIKUS | B (IIS) | OIDC redirect loop |
| 1.2 | Keycloak issuer URL mismatch | 🟠 MAGAS | A (Azure) | JWT validation failure |
| 1.3 | `ValidateAudience = false` | 🟠 MAGAS | N/A (kód) | Privilege escalation |
| 2.1 | Dead Letter Queue hiánya | 🔴 KRITIKUS | A, B, C | Poison message loop |
| 2.2 | Container memory limits | 🟠 MAGAS | C (Docker) | OOM kaszkád-leállás |
| 2.3 | Backpressure / autoscale | 🟡 KÖZEPES | A (Azure) | Queue overflow |
| 3.1 | Health check only in Dev | 🔴 KRITIKUS | A, B, C | Restart loop / deadlock |
| 3.2 | `dotnet-ef` tool hiánya | 🟡 KÖZEPES | A (Azure) | Migration fail |
| 3.3 | WebUI image hiánya | 🟠 MAGAS | A (Azure) | ImagePullError |

---

*Audit lezárva. A fenti 9 pont közül 3 „KRITIKUS" besorolású — ezek javítása nélkül az éles deployment garantáltan sikertelen lesz.*
