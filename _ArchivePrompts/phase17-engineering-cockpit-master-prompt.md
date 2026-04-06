# ⚡ Phase 17: The "Engineering Cockpit" – Master Prompt

## 🎯 Role & Objective

**Role:** Senior Frontend Architect & Lead Motion Designer specializing in **Blazor Server (.NET 10)** and high-density, industrial UI/UX.

**Objective:** Build a brand-new, standalone page called **`/cockpit`** for the **Dusk of Coding** platform. This page is the platform's **Engineering Cockpit** — a high-density "Mission Control" interface that showcases the platform's core features in a way that communicates power, precision, and human mastery over AI. It must create a "Technical WOW" factor that reinforces the platform's philosophy: **"Master the Craft. Wield the Tool."**

---

## 🏗️ Context: Existing Project Architecture

Before writing a single line of code, internalize this context:

- **Framework:** Blazor Server on **.NET 10** with `@rendermode InteractiveServer`. Interactive components require `@rendermode InteractiveServer` at the page level or component level.
- **Project Path:** `DuskOfCoding.WebUi/`
- **New Page File:** `Components/Pages/Cockpit.razor` and its scoped CSS `Components/Pages/Cockpit.razor.css`
- **Layout to use:** `@layout DuskOfCoding.WebUi.Components.Layout.MainLayout` — the sticky dark header with the `Outfit` font, `#09090b` background, orange underline nav links, and glassmorphism panels.
- **Global CSS tokens** (already defined in `wwwroot/app.css`):
  - `--dusk-bg-dark: #09090b` — existing deep background
  - `--dusk-bg-panel: #0f172a`
  - `--dusk-orange-glow: #ea580c` — existing accent (semantically equivalent to "Industrial Amber")
  - `--dusk-purple-light: #7c3aed`
  - `--dusk-text-primary: #f8fafc`
  - `--dusk-text-muted: #94a3b8`
  - `.dusk-glass-panel` — existing utility class for glassmorphism cards
- **Animations:** Use **only CSS `@keyframes` and CSS `transition`**. Do NOT use `framer-motion` — that is a React library and cannot be used natively in Blazor. For JavaScript interactions (mouse tracking, canvas drawing), use **Blazor JSInterop** (`IJSRuntime`) with an inline `<script>` block or a dedicated `.js` file in `wwwroot`.
- **Localization:** The page does **not** require localization (no `IStringLocalizer` needed). All text can be hardcoded in English, as this is a technical showcase page.
- **Routing:** Add a `NavLink` for `/cockpit` in `Components/Layout/NavMenu.razor` — visible to all authenticated users (`<AuthorizeView><Authorized>`), using the Industrial Amber color (`text-warning`).
- **No new NuGet packages.** No new npm packages. All interactivity is CSS + vanilla JS via JSInterop only.

---

## 🎨 Visual Identity: "The Obsidian Foundry"

This page introduces a new aesthetic **layer** on top of the existing design system. Do not break the existing `MainLayout`. Scope all new styles to `Cockpit.razor.css`.

| Token | Value | Rationale |
|---|---|---|
| **Page Background** | `#0D0D0E` (Obsidian — slightly darker than `--dusk-bg-dark`) | Deeper black for contrast |
| **Primary Accent** | `var(--dusk-orange-glow)` (#ea580c) re-themed as "Industrial Amber" | Reuses existing token |
| **Secondary** | `#1a1a1d` | Panel surfaces |
| **Typography** | `'Geist Mono', 'Fira Code', monospace` — pull via `@import` in the scoped CSS | Mathematical, sharp feel |
| **Border Style** | `1px solid rgba(234, 88, 12, 0.2)` for panel borders | Amber-tinted edges |
| **Shadows** | `0 0 0 1px rgba(234,88,12,0.1), 0 4px 24px rgba(0,0,0,0.6)` | Deep, hard shadows — no soft glows |
| **Corners** | Hard `4px` radius maximum on panels. `0px` on technical readout elements | "Hard 90-degree" feel |

**Anti-Soft Design Rules (strictly enforced):**
- ❌ No `border-radius` above `6px` on any Cockpit element.
- ❌ No `filter: blur()` on decorative elements.
- ❌ No `box-shadow` with large spread (soft glowing halos). Use tight, dark shadows only.
- ✅ Use `clip-path`, sharp `outline`, and `border` for emphasis instead.
- ✅ Prefer `opacity` transitions over `color` transitions.

---

## 🚀 Page Structure & Components

The page is divided into **four modules** that "deploy" visually as the user scrolls. Use CSS `@keyframes` with `animation-timeline: view()` (CSS View-Timeline API, supported natively in modern browsers) for scroll-driven reveals on each section. Provide a `@supports` fallback that simply shows the content without animation.

### 1. The Hero: "Master the Craft. Wield the Tool."

- **Layout:** Full viewport height (`min-height: 100vh`), centered content.
- **Background:** The "Logic PCB" — an `<svg>` of a static circuitry grid (horizontal and vertical traces, junction dots) rendered as a **fixed** background pattern. The SVG is embedded inline in the Razor markup, not as an `<img>`.
  - Grid lines in `rgba(234, 88, 12, 0.06)`.
  - Junction dots (tiny `<circle>` elements at intersections) in `rgba(234, 88, 12, 0.12)`.
- **PCB Hover Interaction:** Use `IJSRuntime` to attach a `mousemove` listener on the hero section. On mouse movement, calculate proximity of the cursor to each SVG line and dynamically set their `stroke` opacity to create a "lighting up" effect near the cursor. Implement this in a lightweight inline JS function — no external libraries.
- **Headline:** `<h1>Master the Craft.<br/>Wield the Tool.</h1>` styled with `font-family: 'Geist Mono'`, large (`clamp(2.5rem, 6vw, 5rem)`), color `#F8FAFC`.
- **De-Blur Sub-headline:** The sub-headline `"Engineering Cockpit — Precision. Control. Mastery."` starts with `filter: blur(8px)` and `opacity: 0.4`. On `:hover` of the Hero section, it transitions to `filter: blur(0)` and `opacity: 1` over `0.6s ease`. This is pure CSS — no JS needed.
- **CTA:** An amber-bordered button: `"Launch Practice"` → navigates to `/practice`.

### 2. The Status Grid (Collapsible Modules — "Bento-Switch")

A **4-column CSS Grid** of collapsible "instrument panels." Each panel represents a core platform capability:

| Panel ID | Title | Icon (Bootstrap Icons) | Metric Source |
|---|---|---|---|
| `panel-sandbox` | Execution Sandbox | `bi-cpu` | "Roslyn Compiler — Active" |
| `panel-tutor` | Socratic Mentor | `bi-stars` | "Gemini AI — Connected" |
| `panel-security` | Identity Fortress | `bi-shield-lock` | "Keycloak IAM — Secured" |
| `panel-pipeline` | Resilience Pipeline | `bi-diagram-3` | "Polly — 3 Strategies Active" |

**Collapsed State (default):** Each panel shows only its icon, title, and a pulsing amber status dot.

**Expanded State (on click):** The panel expands with a CSS `max-height` transition (from `60px` to `auto` using a CSS `grid-template-rows: 0fr → 1fr` trick) and reveals a short description and a stylized "readout" — a `<pre>` tag showing a relevant technical snippet (e.g., the Polly pipeline config pattern, the Keycloak OIDC setup).

**Snap Animation:** On click, add a CSS class `.panel-deployed` that triggers a `@keyframes snapOpen: 0% { transform: scaleY(0.95); } 100% { transform: scaleY(1); }` over `150ms` — giving the mechanical "snap" feel.

**Blazor implementation:** Manage expanded state with a `Dictionary<string, bool> _expandedPanels` in `@code` block. Toggle on `@onclick`.

### 3. The Socratic Wave Terminal

Replaces / complements the existing `TutorTerminal.razor`. This is a **visual status indicator** for the AI Mentor, not a full terminal. It is a horizontal 1-pixel-high `<div>` that spans the full width of the page, with a `<canvas>` element behind it for the waveform visualization.

- **When idle:** The wave is a flat amber line, low amplitude.
- **When "Guidance" mode is active** (a toggle on the panel): The line vibrates gently — a CSS `@keyframes` animation that shifts a `clip-path: polygon(...)` path to simulate a sine wave. Amber color.
- **When "Solution" mode is active:** The wave becomes jagged (`clip-path` with sharp peaks), color shifts to `#ef4444` (red). A small label appears: `"⚠ AI dependency detected"`.

The waveform is implemented using **CSS `@keyframes` on `clip-path`** for performance — no Canvas API needed. Provide two named `@keyframes` — `waveGuide` and `waveAlert` — both animating the `clip-path` property of the wave `<div>`.

The mode toggle is a Blazor `<select>` in the `@code` block (options: "Idle", "Guidance", "Warning"). State drives a CSS class on the wave container.

### 4. The Manifesto Footer

A full-width closing section:
- Large amber monospace text: `"> SYSTEM STATUS: OPERATIONAL"`
- Below it, a dimmed mission statement: `"You are the architect. The AI is the compiler."`
- And the platform tagline: `"Master the craft. Wield the tool."`
- A single `NavLink` button → `/practice`, styled as an amber-bordered terminal command: `[ ENTER PRACTICE ENVIRONMENT ]`

---

## 🛠️ Execution Checklist

The agent must complete exactly these files, in this order:

### Step 1: Create `Components/Pages/Cockpit.razor`
- Set route to `@page "/cockpit"`
- Set layout: `@layout DuskOfCoding.WebUi.Components.Layout.MainLayout`
- Set render mode: `@rendermode InteractiveServer`
- Inject `IJSRuntime JS`
- Implement all 4 sections above.
- Manage all interactive state in the `@code` block (panel expansion dictionary, wave mode enum/string).
- **JS Module Import:** The `@code` block must implement `IAsyncDisposable`. Declare a `private IJSObjectReference? _jsModule;` field. In `OnAfterRenderAsync(bool firstRender)`, on first render, load the collocated JS module: `_jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/Cockpit.razor.js");` then call `await _jsModule.InvokeVoidAsync("init", "cockpit-hero");` (where `"cockpit-hero"` is the `id` attribute on the hero `<div>`). Implement `DisposeAsync()` to call `await _jsModule.DisposeAsync()` — this prevents memory leaks when the user navigates away from the page.

### Step 2: Create `Components/Pages/Cockpit.razor.css`
- All styles scoped to this page.
- Import `Geist Mono` at the top: `@import url('https://fonts.googleapis.com/css2?family=Geist+Mono:wght@300;400;500;700&display=swap');`
- Define all new CSS variables for the Cockpit theme.
- Implement `@keyframes` for: `snapOpen`, `waveGuide`, `waveAlert`, `pulseDot`, and scroll-driven reveal (`@supports (animation-timeline: view())`).
- No `border-radius` above `6px`.

### Step 3: Create `Components/Pages/Cockpit.razor.js` (Blazor JS Isolation)
- Use **Blazor JS Isolation** (collocated JS): the file must be named `Cockpit.razor.js` and placed next to `Cockpit.razor` in `Components/Pages/`. Blazor will automatically make it importable via the `./Components/Pages/Cockpit.razor.js` path.
- The file must use **ES Module syntax** (`export function init(sectionId) { ... }`) — do **not** register anything on `window` and do **not** use `export default`.
- The `init(sectionId)` function gets the hero section element by `id`, finds all `<line>` elements inside its inline SVG, and attaches a `mousemove` listener to the section.
- On each `mousemove`, calculate the distance from the cursor to each `<line>` midpoint (average of `x1/y1` and `x2/y2`, transformed to page coordinates). Lines within `120px` of the cursor get their `stroke` set to `rgba(234, 88, 12, 0.75)`. Lines beyond that revert to `rgba(234, 88, 12, 0.06)`.
- Wrap the DOM mutation in `requestAnimationFrame` to avoid layout thrash.
- **No global `window` registration. No `<script>` tag in App.razor.** The module lifetime is managed entirely by Blazor's `IJSObjectReference` — it is imported on page load and disposed via `DisposeAsync()` when the user navigates away.

### Step 4: Update `Components/Layout/NavMenu.razor`
- Add a new `<div class="nav-item me-3">` with a `NavLink` to `/cockpit` inside `<AuthorizeView><Authorized>`.
- Use Bootstrap icon `bi-cpu` and the label `Cockpit`.
- Apply `text-warning` class to match the Industrial Amber theme and distinguish it from the standard nav items.

---

## ✅ Verification Criteria

The implementation is complete when:
1. Navigating to `/cockpit` (while logged in) renders the page without errors.
2. The PCB SVG background is visible and lines near the cursor light up on mouse movement.
3. The De-Blur sub-headline transition works on hover.
4. All 4 panels render collapsed, expand on click with the snap animation, and collapse again.
5. The Socratic Wave changes CSS class and waveform style when the mode select is changed.
6. The `Cockpit` nav link appears in the header for authenticated users.
7. No existing pages (`/`, `/practice`, `/tutor`, `/how-it-was-made`) are broken.
8. The page uses `Geist Mono` font (verify via DevTools).