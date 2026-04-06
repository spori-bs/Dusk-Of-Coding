# 📼 Phase 20: "How It Was Made" — Obsidian Foundry Rebuild

## 🎯 Role & Objective

**Role:** Senior Frontend Architect & Technical Writer specializing in **Blazor Server (.NET 10)** and engineering documentation design.

**Objective:** Completely rebuild the **"How It Was Made" page** (`/how-it-was-made`) in the **Obsidian Foundry** design language. This is the platform's **engineering post-mortem page** — a technical manifesto that documents the vibecoding methodology and the architectural crises it navigated. The old cinematic glassmorphism (Phase 15/16) is replaced with a **technical log / engineering dossier** aesthetic that feels like a real-world incident report meets a premium developer portfolio.

**The Old Feel (Phase 15/16):** Cinematic scroll, purple/cyan orbs, glassmorphism bento cells, "deep space" gradients. Beautiful but disconnected from the Cockpit aesthetic.

**The New Feel (Phase 20):** An engineering dossier. Like a classified briefing document or a production post-mortem — monospace, amber-accented, structured like a technical specification with narrative weight. Still scroll-driven. Still premium. But now unmistakably part of the same design system.

---

## 🏗️ Context

- **Framework:** Blazor Server, .NET 10. The page uses `MainLayout` (standard header/footer). No `@rendermode` needed — static content only, no JS interop required.
- **Files to rewrite:**
  - `Components/Pages/HowItWasMade.razor` — full markup rebuild
  - `Components/Pages/HowItWasMade.razor.css` — full CSS rewrite
  - `Components/Pages/HowItWasMade.en.resx` — content update (new keys, remove old)
  - `Components/Pages/HowItWasMade.hu.resx` — Hungarian rework (poetic, professional)
- **Localization:** Keep `IStringLocalizer<HowItWasMade>`. All new text keys must be added to both `.resx` files simultaneously. Hungarian must be premium quality — not Google Translate.
- **NO `@import` in `.razor.css`.** Fonts are globally loaded.
- **Global tokens:** All `--ck-*` variables from Phase 18 are available.
- **Scroll animations:** Use CSS View-Timeline (`animation-timeline: view()`) with `@supports` fallback — same pattern as Cockpit. The old `filter: blur(10px)` reveal is replaced with a sharper `translateY(20px) → 0` + `opacity: 0 → 1` reveal.

---

## 📐 Page Architecture: The Engineering Dossier

The page is structured as a **classified briefing document** — each section is a "chapter" prefixed with an amber section number (`// 01`, `// 02`, etc.) just like the Cockpit. The narrative flows chronologically through the build history.

```
[ HEADER / DOSSIER COVER ]    → Top: classification label, title, abstract
[ // 01 — THE DIRECTIVE ]     → Why this was built. Philosophy.
[ // 02 — THE SYSTEM MAP ]    → What was built. Architecture overview (Cockpit instrument grid).
[ // 03 — BUILD LOG ]         → How it was built. Phase-by-phase timeline.
[ // 04 — THE ABYSS ]         → Where it collapsed. Root-cause incidents.
[ // 05 — RESOLUTION ]        → How it was stabilized. The Socratic method applied.
[ // 06 — FIELD MANUAL ]      → Prompt Gallery — the actual directives used.
[ FOOTER — MANIFESTO ]        → Closing statement + CTA.
```

---

## 🚀 Section-by-Section Specification

### Section 0: Dossier Cover (replaces the old Hero section)

**Visual:** Full-viewport height. Obsidian background (`#0D0D0E`). No orbs. No blur. Static.

**Top-left corner:** An amber bracket `[` ... `]` wrapping a classification label:
```
[ ENGINEERING DOSSIER // CLASSIFIED — INTERNAL REVIEW ]
```
Geist Mono, 0.65rem, amber, letter-spacing 0.2em.

**Center:**
- Document number: `DOC-2025-VIBECODING-001` — amber, monospace, small
- **H1:** `"A Kódolás Alkonya."` (HU) / `"The Twilight of Manual Coding."` (EN)
  - Geist Mono, `clamp(2.5rem, 5vw, 4.5rem)`, weight 700, pure white, no gradient
- **Abstract:** 2-line paragraph in monospace, muted color:
  ```
  "This document chronicles the construction of the Dusk of Coding platform —
  built without writing most of its code, directed by human architecture, assembled by AI."
  ```
- **Two metadata lines (monospace table style):**
  ```
  METHODOLOGY    //  Vibecoding (AI-directed architecture)
  STACK          //  Blazor Server · Keycloak · Roslyn · Gemini · Polly
  ```
- **Bottom status strip** (identical to Cockpit hero): `PHASES: 1 → 20    STATUS: ACTIVE    BUILD: .NET 10`

**No CTA here** — the page is a document, not a conversion funnel.

---

### Section 1: `// 01 — The Directive`

**Purpose:** Why this was built. The philosophy statement.

**Left column:** The section header pattern. Then two paragraphs:
> *"The sun is setting on the era of the lone developer — the craftsman who knew every line, every function, every side-effect of their system. AI does not end that era gracefully. It amputates it. What remains are two kinds of developers: those who lost the skill, and those who chose to deepen it while learning to wield the new instrument."*

> *"Dusk of Coding exists at this threshold. A platform built specifically to prove that the most powerful workflow is not human alone, nor AI alone — but a precise, disciplined collaboration between the two."*

**Right column:** A Cockpit-style instrument panel (static, non-collapsible) with the platform's core stat:
```
┌──────────────────────────────┐
│ MISSION // PRIMARY           │
│                              │
│ Teach the synthesis of:      │
│  → Deep engineering mastery  │
│  → Precise AI direction      │
│                              │
│ STATUS: ACTIVE               │
└──────────────────────────────┘
```

---

### Section 2: `// 02 — The System Map`

**Purpose:** What was built. Technical architecture overview.

**Reuse the Cockpit Status Grid pattern** — same 4 collapsible instrument panels (`panel-sandbox`, `panel-tutor`, `panel-security`, `panel-pipeline`) with identical styling. These are **not interactive** on this page — always in the expanded state (show the description and readout immediately, no click needed). This keeps the page CSS-only with no JS requirement.

**Below the grid:** A dependency flow diagram showing how the services connect. **Do NOT draw this as an inline SVG** — AI-generated SVGs with text labels and box sizing are consistently misaligned. Instead, build it using HTML `<div>` elements styled with CSS Flexbox (`display: flex; align-items: center; gap: 1rem;`). Use Bootstrap Icon `<i class="bi bi-arrow-right">` (amber-colored) as connectors. Each node is a `<div class="arch-node">` with hard corners and `var(--ck-border)` border. Example structure:

```
[ Blazor WebUI ] →  [ WebAPI ] →  [ ExecutionAPI ]
                         ↓
                  [ TutorWorker ] → [ Gemini AI ]
                         ↑
                   [ Keycloak ]
```

Style `arch-node` in `HowItWasMade.razor.css`: Geist Mono, 0.7rem, `1px solid var(--ck-border)`, `padding: 0.3rem 0.7rem`, amber text on hover. This ensures perfect text scaling, full responsiveness, and zero alignment issues.

---

### Section 3: `// 03 — Build Log`

**Purpose:** Phase-by-phase timeline of what was built.

**Visual pattern:** A vertical timeline — a single amber vertical line (1px, `var(--ck-border)`) on the left, with horizontal connector stubs (`──`) pointing to timeline entries.

Each entry:
```
├── PHASE 01  //  Architecture Foundation
│             Roslyn compiler. Sandbox execution. Clean Architecture scaffold.
│
├── PHASE 06  //  Identity Fortress
│             Keycloak OIDC integration. JWT Bearer API security. Self-registration.
│
├── PHASE 12  //  The Security Audit
│             Root-cause analysis of OIDC correlation failures and issuer mismatches.
│
...
```

Group phases logically:
- **FOUNDATION** (1-5): Architecture, execution, localization, rebrand
- **SECURITY** (6, 9, 10, 12): Keycloak, RBAC, HTTP 431, hardening
- **INTELLIGENCE** (11, 13, 14): Feedback, EF Core, AI mentor
- **IDENTITY** (15, 16, 17): HowItWasMade, Landing, Cockpit

Each group is labeled with an amber uppercase category header. The timeline should scroll-reveal each group using CSS View-Timeline.

---

### Section 4: `// 04 — The Abyss`

**Purpose:** Document the three major architectural failures. This is the most dramatic section.

**Opening statement (monospace, centered, amber):**
```
> THREE CRITICAL FAILURES. ALL AI-GENERATED. ALL RECOVERED.
```

**Three incident panels** — styled like real incident reports:

```
┌─ INCIDENT-001 ─────────────────────────────────────┐
│ SEVERITY: HIGH                                       │
│ COMPONENT: Keycloak OIDC / Kestrel                  │
│ SYMPTOM: HTTP 431 — Request Header Too Large         │
│ ROOT CAUSE: Correlation cookies multiplied per       │
│   redirect. SameSite=None policy misaligned.         │
│ RESOLUTION: Memory-backed ITicketStore.              │
│   Targeted AI directive: "phase10-resolution"        │
└─────────────────────────────────────────────────────┘
```

Three such panels for:
1. **INCIDENT-001** — HTTP 431 / Cookie explosion (Phase 10/12)
2. **INCIDENT-002** — Dual-Network Illusion (Phase 12.2 — Keycloak issuer mismatch between Aspire container network and browser)
3. **INCIDENT-003** — EF Core Concurrency Collapse (Phase 13 — Change Tracker violations)

Visual: Each panel has a red `SEVERITY: HIGH` line that flickers (CSS `@keyframes` opacity: 1→0.4→1 at 2s). The word `RESOLUTION` is amber. No purple. No cyan.

**Below the panels:** A recovery waveform — the exact same SVG healing graph from Phase 16 (`fractured-graph` → `healing-wave`) but recolored: broken nodes in red `#ef4444`, healed nodes in amber `#ea580c` (not green). This matches the Cockpit's amber-first aesthetic.

---

### Section 5: `// 05 — Resolution`

**Purpose:** The Socratic method applied. How the methodology actually worked.

**Content:** The refined narrative from Phase 16:
> *"The architecture eventually collapsed under its own generated complexity. The AI components were logically sound in isolation but tore each other apart in a distributed environment. The solution was not to write more manual code. We mapped the underlying logical failures and designed highly surgical, Socratic prompts — forcing the AI to interrogate its own flaws and piece the system back together securely."*

**Right side:** A typing animation (pure CSS `@keyframes width` animation) showing a prompt being written to the Socratic AI directive:
```
> Analyzing: Keycloak correlation failure...
> Hypothesis: Cookie policy mismatch on redirect...
> Directive: Split Authority from MetadataAddress...
> Result: ISSUER_RESOLVED ✓
```
Styled as a terminal readout, Geist Mono, amber text, `#111113` background. No JS needed — `@keyframes` typing animation.

---

### Section 6: `// 06 — Field Manual` (Prompt Gallery)

**Purpose:** Display the actual prompts used — preserving the concept from Phase 15 but redesigned.

**Visual:** Three panels, identical to the Cockpit instrument panels (hard corners, amber borders, `#1a1a1d` background), each showing:
- Panel header: Phase number + title
- Short description
- `<pre><code>` readout with the actual prompt snippet

**Panels:**
1. `PHASE 01 // Architecture Foundation` — the original `.NET architecture` directive
2. `PHASE 12 // Root-Cause Directive` — the surgical Keycloak diagnosis prompt
3. `PHASE 14 // Socratic Guardrails` — the AI mentor behavioral specification

Same data as `Cockpit.razor`'s Prompt Gallery — reuse the data, update the visual treatment.

---

### Section 7: Manifesto Footer

**Identical in structure to Cockpit's manifesto section**, but with different text:

```
> DOSSIER STATUS: COMPLETE

"The methodology works. The system is stable. The platform is live."

"This is what vibecoding looks like when the human is the architect."

[ READ: PHASE 18 ONWARDS ]     or     [ ENTER THE PLATFORM ]
```

Two amber-bordered terminal buttons side by side:
- Left: `[ ENTER THE PLATFORM ]` → `/register` (unauth) or `/practice` (auth)
- Right: `[ RETURN TO DOSSIER COVER ]` → **Use a simple `<a href="#dossier-cover" class="btn btn-amber">` anchor. Do NOT use JSInterop or `@onclick`.** Add `id="dossier-cover"` to the Section 0 wrapper `<div>`. Ensure `html { scroll-behavior: smooth; }` is set in `HowItWasMade.razor.css`. This keeps the page fully static (no SignalR circuit needed for a scroll).

---

## 🛠️ Implementation Checklist

### Step 1: Rewrite `HowItWasMade.razor`
- Completely replace the existing markup.
- 7 sections as specified above.
- Keep `@inject IStringLocalizer<HowItWasMade> Loc` and `@inject NavigationManager Nav`.
- Use `@page "/how-it-was-made"` — **do not add rendermode** (page is static). If the scroll-to-top button requires JS, add `@rendermode InteractiveServer` only if necessary.

### Step 2: Rewrite `HowItWasMade.razor.css`
- **Delete all existing content.** Start fresh.
- Reference `--ck-*` tokens from app.css.
- New styles: dossier cover, timeline, incident panels, typing animation keyframes, recovery SVG, scroll reveals.
- No `border-radius` above `4px` (the bento cells from Phase 15/16 had `24px` — remove all of that).
- No `filter: blur(...)` on any decorative element except the de-blur sub-headline.

### Step 3: Update `HowItWasMade.en.resx`
Remove all Phase 15/16 keys that no longer apply. Add:
- `PageTitle`, `DossierLabel`, `DocNumber`, `HeroTitle`, `HeroAbstract`
- `DirectiveTitle`, `DirectiveBody1`, `DirectiveBody2`
- `AbyssTitle`, `AbyssIntro`, `Incident001_*`, `Incident002_*`, `Incident003_*`
- `ResolutionTitle`, `ResolutionBody`
- `GalleryTitle`, `ManifestoLine1`, `ManifestoLine2`, `ManifestoLine3`

### Step 4: Update `HowItWasMade.hu.resx`
Premium Hungarian translation for all new keys. Tone: precise, professional, slightly literary. Not literal — idiomatic. See Phase 16 for established quality bar and vocabulary.

**Mandatory HU Translation Directives for Section 4 (The Abyss)** — do not deviate:
- `"Cookie explosion"` → `"Fokozódó süti-duplikáció (Cookie bloat)"` (not "süti robbanás" — unnatural)
- `"Dual-Network Illusion"` → `"Kettős Hálózat Illúzió (Konténer vs. Böngésző hálózat)"`
- `"Change Tracker violations"` → `"Change Tracker konzisztencia-sértések"` (not "aszinkronitási anomáliák" — the issue was state tracking inconsistency, not async)
- `"EF Core Concurrency Collapse"` → `"EF Core Konkurencia Összeomlás"`
- `"Root-cause incidents"` → `"Gyökérok-analízis"`

For all other keys: idiomatic Hungarian, register of a senior technical author, never Google-Translate phrasing.

---

## ✅ Verification Criteria

1. `/how-it-was-made` renders without exceptions (authenticated and unauthenticated — it's a public page via `MainLayout`).
2. All 7 sections visible on scroll.
3. Incident panels have red flickering SEVERITY and amber RESOLUTION.
4. Timeline renders with vertical amber line and phase entries.
5. Typing animation plays in Section 5.
6. No purple, no cyan, no `border-radius` above `4px` anywhere on this page.
7. Hungarian text is visible when language is set to HU — and it reads naturally.
8. `dotnet build` → 0 errors, 0 warnings.
9. `/cockpit`, `/practice`, `/` unchanged (regression).
