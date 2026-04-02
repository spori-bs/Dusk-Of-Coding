# 🌑 Phase 19: Landing Page Rebuild — Master Prompt

## 🎯 Role & Objective

**Role:** Senior Frontend Architect & Creative Director specializing in **Blazor Server (.NET 10)** and conversion-focused landing page design.

**Objective:** Completely rebuild the **Dusk of Coding landing page** (`/`) using the **Obsidian Foundry** design language established in Phase 17 (`/cockpit`). This is not a reskin — it is a **content and design rethink**. The old glassmorphism/purple marketing page is replaced with a high-density, amber-accented, industrial-technical landing experience that actually reflects what the platform is.

**The Old Message (Phase 5):** Generic SaaS copy. Soft purple glows. Rounded pills. Feature cards. Demo section. Social proof. Lead form.

**The New Message (Phase 19):** A sharp, technical manifesto for developers. The landing page should feel like the entry airlock to a precision engineering facility, not a product marketing site. The aesthetic must create productive anxiety — this is a serious platform for serious developers.

---

## 🏗️ Context: Existing Architecture

- **Framework:** Blazor Server, .NET 10. Landing page uses `@layout LandingLayout`, `@rendermode InteractiveServer`.
- **Route:** `/` → `Components/Pages/Home.razor` (currently assembles `HeroComponent`, `FeaturesComponent`, `MediaDemoComponent`, `SocialProofComponent`, `LeadFormComponent`)
- **Layout file:** `Components/Layout/LandingLayout.razor` — has its own header and footer.
- **Localization:** All text must remain localized via `IStringLocalizer<SharedResource>`. New text keys must be added to both `SharedResource.en.resx` and `SharedResource.hu.resx`. The Hungarian translation must be as precise and fitting as the English — no literal word-for-word translation.
- **Global tokens available:** All `--ck-*` variables from Phase 18 are now in `app.css`. Use them everywhere.
- **NO `@import` in `.razor.css` files.** All fonts are already loaded via `app.css`.

---

## 📐 Content Architecture: The New Landing

Replace the 5 old components with **4 new sections** that follow a tighter narrative arc:

```
[ Entry Screen ]       → Full-height hero. Statement, not pitch.
[ The Problem ]        → Why most developers will fail in the AI era.
[ The System ]         → What the platform actually does (technical, precise).
[ The Threshold ]      → CTA. Binary choice: enter or leave.
```

---

## 🚀 Section-by-Section Specification

### Section 1: The Entry Screen (replaces `HeroComponent`)

**Layout:** Full-viewport (`min-height: 100vh`). Centered. The PCB grid SVG background from Cockpit — identical implementation (inline SVG, amber lines, junction dots).

**Content:**

- **Static label (top-center):** `DUSK OF CODING // PLATFORM v1.0` — Geist Mono, 0.65rem, amber, letter-spacing 0.2em. Optionally blinking status dot.
- **Main headline (H1):**
  ```
  The last generation
  of developers who
  code alone.
  ```
  Geist Mono, `clamp(2.8rem, 6vw, 5.5rem)`, weight 700, white. Line breaks intentional. No gradient text.
  
- **Sub-statement (de-blur on hover, identical to Cockpit hero):**  
  `"AI is the most powerful tool in history. Most developers will use it as a crutch. This platform is for the ones who won't."`  
  Geist Mono 0.95rem, muted, `filter: blur(5px) → blur(0)` on parent hover.

- **Two CTAs:**
  - Primary: `[ ENTER THE PLATFORM ]` → `/register?returnUrl=/practice` (unauthenticated) or → `/practice` (authenticated). Amber bordered terminal button style.
  - Secondary: `[ SEE HOW IT WAS BUILT ]` → `/how-it-was-made`. Dimmer border.

- **Bottom-of-hero readout strip (amber monospace, small):**
  ```
  SANDBOX // Roslyn Compiler    MENTOR // Gemini AI    IAM // Keycloak    RESILIENCE // Polly
  ```
  Horizontally spaced, 0.65rem, letter-spacing 0.15em. Static — not interactive. Like a status bar.

**HU localization keys to add/update:**
- `Hero_Headline`: "Az utolsó generáció, amely egyedül kódol."
- `Hero_Sub`: "A mesterséges intelligencia a történelem legerősebb eszköze. A legtöbb fejlesztő mankóként fogja használni. Ez a platform azoknak szól, akik nem."
- `Hero_CTA_Primary`: "[ BELÉPÉS A PLATFORMRA ]"
- `Hero_CTA_Secondary`: "[ HOGYAN KÉSZÜLT ]"

---

### Section 2: The Problem (replaces `FeaturesComponent` + `SocialProofComponent`)

**Layout:** Full-width, dark. Two columns: text left, visual right.

**Left — The Diagnosis:**

Heading: `// THE PROBLEM` — amber prefix, Geist Mono.  
Sub-heading (H2): `"AI doesn't make weak developers strong. It makes them invisible."`

Three diagnostic lines — each a `<div>` styled like a terminal error entry:
```
[ERR] Architecture hallucinated — passes tests, collapses in production.
[ERR] Debug loop dependency — can't trace problems the AI introduced.
[ERR] Skill atrophy — can direct AI, but cannot build without it.
```
Each line: Geist Mono, 0.8rem, amber border-left, red `[ERR]` prefix. Background `#111113`.

**Right — The Reality:**

A simple counter/stat display in Cockpit instrument-panel style. Three panels:
```
┌─────────────────────────────┐
│ STAT // 01                  │
│ Developers using AI daily   │
│                    > 70%    │
└─────────────────────────────┘
```
Three such panels stacked — hard corners, amber borders, monospace readout. Stats are illustrative/estimated:
- `> 70%` — developers using AI code generation daily
- `< 30%` — who can debug AI-generated code without AI
- `0` — platforms teaching the synthesis of both

---

### Section 3: The System (replaces `MediaDemoComponent`)

**Layout:** Full-width dark section with the section header pattern `// THE SYSTEM`.

**Content — Two rows, two columns each:**

This replaces the old "demo video" and "features list" with a **Cockpit-style 2×2 instrument grid** — identical pattern to the Cockpit's Status Grid, but simplified (non-collapsible, always-visible descriptions).

| Panel | Title | Content |
|---|---|---|
| `sys-sandbox` | Execution Sandbox | "Your code compiles inside a collectible AssemblyLoadContext. 5s timeout. xUnit runner. Zero host contamination." |
| `sys-mentor` | Socratic AI Mentor | "It never gives you the answer. It interrogates your thinking using Gemini AI — streamed via SignalR." |
| `sys-identity` | Identity Fortress | "Keycloak OIDC. JWT Bearer. Role-based access. Session stored in memory — no cookie bloat." |
| `sys-resilience` | Resilience Engine | "Every external call — AI, execution, API — runs through Polly: Retry(3) → CircuitBreaker → Timeout." |

Below the grid, a single line (amber, monospace):
`"> All systems are interconnected. Failure in one triggers recovery in another."`

---

### Section 4: The Threshold (replaces `LeadFormComponent`)

**Layout:** Full-width. Centered. Dark background. No form — the lead form is removed entirely. This is a binary moment.

**Top:** A horizontal divider line with an amber `[ CHECKPOINT ]` label centered on it.

**Content:**
```
You've seen the system.
The question is whether you're ready to use it correctly.
```
Geist Mono, 1.1rem, centered.

**Two CTA blocks side by side — styled as terminal choices:**

```
┌─────────────────────────┐       ┌─────────────────────────┐
│ [ REGISTER & ENTER ]    │  vs.  │ [ I'M NOT READY YET ]   │
│ Create your account.    │       │ Come back when you are.  │
│ Start practicing today. │       │ The platform will wait.  │
└─────────────────────────┘       └─────────────────────────┘
```

Left button: amber border, calls `/register`.  
Right button: dimmed border (muted text) — links to `/how-it-was-made` (understanding the platform first).

**Bottom (authenticated state):** If user is already logged in, replace both with:
`"> AUTHENTICATED // Welcome back, [username]. [ RESUME PRACTICE → ]"`

---

## 🛠️ Implementation Checklist

### Step 1: Rebuild `Components/Layout/LandingLayout.razor`
- Change `.landing-logo` to use `font-family: var(--ck-font)` with a `"> "` amber prefix span.
- Change `landing-header` bottom border to `1px solid var(--ck-border)`.
- Remove rounded corners from all header buttons. Use `btn-amber` style.
- The footer: change to monospace, add `"// DUSK OF CODING — @year"` prefix style.

### Step 2: Rebuild `Components/Landing/HeroComponent.razor` and `HeroComponent.razor.css`
- Replace with Section 1 spec above.
- Reuse the inline SVG PCB pattern from `Cockpit.razor` (copy the `<svg>` block and Cockpit hero CSS).
- **PCB Animation (Zero-JS):** Do NOT use JSInterop or any inline `<script>` tag. Make the PCB grid **static**, but apply a CSS `@keyframes` animation named `ckPulseGrid` to the SVG `<line>` elements that slowly cycles `stroke-opacity` between `0.02` and `0.08` over 8 seconds with `animation-timing-function: ease-in-out; animation-direction: alternate; animation-iteration-count: infinite`. This keeps the hero feeling alive without mouse tracking or JS. The de-blur sub-headline remains CSS-only via a `:hover` transition on the parent section.

### Step 3: Rebuild `Components/Landing/FeaturesComponent.razor`
- Replace with Section 2 ("The Problem") spec.
- No external assets. All styled with CSS + Bootstrap Icons.

### Step 4: **Delete** `Components/Landing/MediaDemoComponent.razor`
- Replace the reference in `Home.razor` with a new `SystemGridComponent.razor` (Section 3).
- The new component: `Components/Landing/SystemGridComponent.razor` + `.razor.css`.

### Step 5: **Delete** `Components/Landing/SocialProofComponent.razor`
- Remove from `Home.razor`. Its purpose is replaced by the stat panels in Section 2.

### Step 6: Rebuild `Components/Landing/LeadFormComponent.razor`
- Replace with Section 4 ("The Threshold") spec. No form. Pure CTA logic.
- The component still handles auth-state checking: `<AuthorizeView>` for the conditional authenticated display.

### Step 7: Update `Components/Pages/Home.razor`
Replace the component list:
```razor
<HeroComponent />
<FeaturesComponent />
<SystemGridComponent />
<ThresholdComponent />
```
(Rename `LeadFormComponent` to `ThresholdComponent` to match new purpose.)

### Step 8: Localization — `SharedResource.en.resx` + `SharedResource.hu.resx`
Add all new keys from this prompt. Remove keys that are no longer used (MediaDemo, SocialProof-specific keys). The Hungarian copy must be professionally written — poetic precision, not literal translation.

**Mandatory HU Translation Directives** — do not deviate from these:
- `"Skill atrophy"` → `"Tudáskopás"` (not "képesség sorvadás" — unnatural)
- `"Debug loop dependency"` → `"Végtelen debug ciklus"`
- `"Architecture hallucinated"` → `"Hallucinált architektúra"`
- `"The Threshold"` → `"A Küszöb"`
- `"Entry airlock"` → `"Beléptető zsilip"`
- `"Productive anxiety"` → `"Produktív feszültség"`

For all other keys, apply the same quality bar: idiomatic Hungarian, register of a senior technical author, never Google-Translate phrasing.

---

## ✅ Verification Criteria

1. `/` renders without authentication — full page, no exceptions.
2. PCB background visible. Hero de-blur works on hover.
3. Status readout strip shows 4 system names.
4. "The Problem" diagnostic lines render with amber border-left.
5. "The System" 2×2 grid renders identically to Cockpit status panels.
6. "The Threshold" shows two CTA columns. Authenticated users see resume prompt.
7. `dotnet build` → 0 errors, 0 warnings.
8. `/cockpit`, `/practice` unchanged (regression).
