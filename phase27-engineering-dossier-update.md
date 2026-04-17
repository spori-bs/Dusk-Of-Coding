# 🛠️ Phase 27: Engineering Dossier Update (Phase 21-26 & AI Quota Incident)

**Role:** Senior Frontend Architect (.NET 10, Blazor, Localization).
**Context:** The application has progressed up to Phase 26, but the `/how-it-was-made` page (Engineering Dossier) is stuck at Phase 20. We need to append the latest technical milestones and document a critical "Meta" incident regarding AI Token exhaustion during development.

**Task:** Update both the `.resx` file and the `HowItWasMade.razor` component with the new data.

### Step 1: Update the `.resx` Localization File (Hungarian)
Append the following key-value pairs to the Hungarian `.resx` file (do not delete existing entries):

1. **Build Log Updates:**
   - `BuildLog_Phase21`: "FÁZIS 21 // Feladatszerkesztő Cockpit"
   - `BuildLog_Phase21_Desc`: "Master-detail UI több fájlos tesztszerkesztéshez. Szinkronizált oldalsáv a Monaco editorral és MI-vezérelt tesztgenerálással."
   - `BuildLog_Phase22`: "FÁZIS 22 // Tesztmérnöki Stabilizáció"
   - `BuildLog_Phase22_Desc`: "Adatbázis-konkurencia (DbUpdateConcurrencyException) javítása a tesztgeneráló csővezetékben. Lecsatolt (disconnected) entitás minta bevezetése a Workerben."
   - `BuildLog_Phase23`: "FÁZIS 23 // UX Polírozás és Identitás"
   - `BuildLog_Phase23_Desc`: "Lézer-szkenneres szintaxis HUD, dinamikus szerepkör-jelvények (Role badges), és SignalR polling fallback."
   - `BuildLog_Phase24`: "FÁZIS 24 // Perzisztencia Migráció és Telemetria"
   - `BuildLog_Phase24_Desc`: "Adatbázis motor cseréje SQLite-ról MariaDB-re Aspire hangszereléssel. LlmTelemetryLog entitás az MI nyomon követésére."
   - `BuildLog_Phase25`: "FÁZIS 25 // Dinamikus SUT Konfiguráció"
   - `BuildLog_Phase25_Desc`: "ExpectedClassName bevezetése a hardkódolt 'Solution' helyett, dinamikus injektálással az MI promptokba."
   - `BuildLog_Phase26`: "FÁZIS 26 // Kódolótér UX Bugfixek"
   - `BuildLog_Phase26_Desc`: "SignalR claim-eltérések feloldása. Dinamikus SUT sablon inicializálása a Monaco editorban."

2. **Incident 004 (Meta AI Token Collapse):**
   - `Incident004_Id`: "INCIDENS-004"
   - `Incident004_Severity`: "SÚLYOSSÁG: KRITIKUS"
   - `Incident004_Component`: "KOMPONENS: Külső MI Eszközkészlet / API Kvóta"
   - `Incident004_Symptom`: "TÜNET: Token-kimerülés (Exhaustion). Az IDE-be integrált modellek leállása (100% kvóta limit), masszív kontextus-vesztés és hallucinált kódrészletek."
   - `Incident004_RootCause`: "GYÖKÉROK: A 'Vibe Coding' túlterhelése. Gyors, kis kontextusú modellek használata mély architekturális (Blazor state + EF Core) refaktorálásra. A kontextus ablak megtelt irreleváns DOM/CSS zajjal."
   - `Incident004_Resolution`: "ELHÁRÍTÁS: Átállás a 'Sniper Prompting' (Sebészi) stratégiára. Nehéztüzérség (High Compute AI) bevetése izolált kódrészletekkel és szigorú, egykéréses (One-Shot) specifikációs blokkokkal."
   - `Incident004_Directive`: "DIREKTÍVA: phase24-sniper-prompting-protocol"

3. **Status Strip Update:**
   - Update `HeroStatusStrip` from `FÁZISOK: 1 → 20` to `FÁZISOK: 1 → 26    //    ÁLLAPOT: AKTÍV    //    BUILD: .NET 10`

### Step 2: Update `HowItWasMade.razor`
1. **Build Log Section:** Below the existing `log-group` "IDENTITY", add a new `<div class="log-group dossier-reveal">` labeled "FINOMHANGOLÁS ÉS SKÁLÁZÁS". Add 6 `<div class="log-entry">` elements pointing to the newly added `BuildLog_Phase21` through `Phase26` localization keys. Make sure the last entry uses the `└──` connector, while the others use `├──`.
2. **The Abyss Section:** Inside `<div class="incident-grid">`, append a 4th `<div class="incident-panel dossier-reveal">`. Apply inline styling `style="border-color: rgba(239, 68, 68, 0.4);"` to the panel to highlight its severity. Bind its inner elements to the `Incident004_*` localization keys. Set the `Incident004_Resolution` line's color explicitly to `#ef4444`.

**Constraint:** Output ONLY the exact code changes necessary for the `.resx` XML additions and the `.razor` file insertions. Do not rewrite the entire files.