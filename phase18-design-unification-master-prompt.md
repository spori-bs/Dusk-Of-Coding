 # ⚙️ Phase 18: Design System Unification — Master Prompt


## 🎯 Role & Objective


**Role:** Senior Frontend Architect specializing in **Blazor Server (.NET 10)** and design system migrations.


**Objective:** Migrate all app pages to the **"Obsidian Foundry"** design language introduced in Phase 17 (`/cockpit`). This phase covers the **global design tokens**, both shared **layouts**, and all **authenticated app pages**. The landing page and "How It Was Made" page are *excluded* — they are addressed in Phase 19 and 20 respectively. **No business logic changes. CSS and markup only.**


---


## 🏗️ Context: Existing Architecture


- **Framework:** Blazor Server on .NET 10. All interactive pages use `@rendermode InteractiveServer`.

- **Global CSS:** `wwwroot/app.css` — contains all `--dusk-*` tokens and shared utility classes.

- **Strict CSS Isolation:** All component-specific styles MUST be placed in the component's scoped `.razor.css` file. **Do NOT write inline `<style>` blocks or `style="..."` attributes in `.razor` HTML.** Do NOT use `@import` in scoped CSS files — Blazor's bundler forbids it.

- **Font already loaded:** `Geist Mono` is now globally available via `app.css` (added in Phase 17).

- **Existing Cockpit tokens** (defined in `Cockpit.razor.css`, to be promoted globally):

  - `--ck-bg: #0D0D0E`, `--ck-surface: #1a1a1d`, `--ck-amber: #ea580c`, `--ck-amber-bright: #f97316`

  - `--ck-border: rgba(234, 88, 12, 0.2)`, `--ck-font: 'Geist Mono', 'Fira Code', monospace`

- **DO NOT MODIFY:** `HowItWasMade.razor`, `HowItWasMade.razor.css`, `Home.razor`, `LandingLayout.razor`, or any `Landing/` components — these are Phase 19/20 scope.


---


## 🎨 Design Language Reference (from Cockpit)


| Rule | Value |

|---|---|

| Primary accent | `#ea580c` (Industrial Amber) |

| Panel surface | `#1a1a1d` |

| Typography (labels, prefixes, readouts) | `'Geist Mono', monospace` |

| Panel border | `1px solid rgba(234, 88, 12, 0.2)` |

| Max border-radius | `4px` on panels, `0px` on inputs/buttons |

| Shadow | `0 0 0 1px rgba(234,88,12,0.1), 0 4px 24px rgba(0,0,0,0.6)` |

| Section prefix style | `font-size: 0.65rem; color: #ea580c; letter-spacing: 0.18em; font-family: Geist Mono` |

| Focus ring | `0 0 0 2px rgba(234, 88, 12, 0.35)` |


---


## 🛠️ Step-by-Step Execution


### Step 1: `wwwroot/app.css` — Global Token Upgrades


Make the following changes to `app.css`:


1. **Add global Cockpit tokens** to `:root` (so all pages can use them without importing):

   ```css

   --ck-bg:           #0D0D0E;

   --ck-surface:      #1a1a1d;

   --ck-amber:        #ea580c;

   --ck-amber-bright: #f97316;

   --ck-amber-dim:    rgba(234, 88, 12, 0.12);

   --ck-border:       rgba(234, 88, 12, 0.2);

   --ck-border-dim:   rgba(234, 88, 12, 0.06);

   --ck-font:         'Geist Mono', 'Fira Code', monospace;

   --ck-shadow:       0 0 0 1px rgba(234,88,12,0.1), 0 4px 24px rgba(0,0,0,0.6);

   --ck-muted:        #6b7280;

   ```


2. **Add `.btn-amber`** — a new global primary action button (do NOT replace `.btn-primary` — the old purple is still used on the landing page in Phase 19's scope):

   ```css

   .btn-amber {

       background: transparent;

       border: 1px solid var(--ck-amber);

       color: var(--ck-amber);

       font-family: var(--ck-font);

       font-size: 0.82rem;

       letter-spacing: 0.08em;

       padding: 0.6rem 1.5rem;

       border-radius: 0;

       transition: background 0.2s ease, color 0.2s ease;

       cursor: pointer;

   }

   .btn-amber:hover {

       background: var(--ck-amber-dim);

       color: var(--ck-amber-bright);

       border-color: var(--ck-amber-bright);

   }

   ```


3. **Add `.ck-section-header`** — the amber `// XX Title ───────` pattern used throughout the Cockpit:

   ```css

   .ck-section-header {

       display: flex;

       align-items: center;

       gap: 1rem;

       margin-bottom: 2rem;

   }

   .ck-section-prefix {

       font-family: var(--ck-font);

       font-size: 0.65rem;

       color: var(--ck-amber);

       letter-spacing: 0.18em;

       white-space: nowrap;

   }

   .ck-section-title {

       font-family: var(--ck-font);

       font-size: 1rem;

       font-weight: 500;

       color: var(--dusk-text-primary);

       white-space: nowrap;

       margin: 0;

   }

   .ck-section-line {

       flex: 1;

       height: 1px;

       background: var(--ck-border-dim);

   }

   ```


4. **Update `.dusk-glass-panel`** border to amber tint:

   - Change `border: 1px solid var(--glass-border)` → `border: 1px solid rgba(234, 88, 12, 0.12)`


5. **Add `.ck-input`** — amber-focused form input (global replacement for `dusk-input` in app pages):

   ```css

   .ck-input {

       background-color: #111113;

       border: 1px solid rgba(234, 88, 12, 0.2);

       color: var(--dusk-text-primary);

       border-radius: 0;

       font-family: var(--ck-font);

       font-size: 0.85rem;

       transition: border-color 0.2s;

   }

   .ck-input:focus {

       background-color: #111113;

       border-color: var(--ck-amber);

       box-shadow: 0 0 0 2px rgba(234, 88, 12, 0.2);

       color: var(--dusk-text-primary);

   }

   .ck-input::placeholder {

       color: var(--ck-muted);

   }

   ```


6. **Add `.ck-status-dot`**:

   ```css

   .ck-status-dot {

       display: inline-block;

       width: 7px;

       height: 7px;

       border-radius: 0;

       background: var(--ck-amber);

       animation: ckPulseDot 2s ease-in-out infinite;

   }

   @keyframes ckPulseDot {

       0%, 100% { opacity: 1; }

       50%       { opacity: 0.25; }

   }

   ```


---


### Step 2: `Components/Layout/MainLayout.razor` — Header Upgrade


Move all inline `<style>` content currently in `MainLayout.razor` into a new scoped `MainLayout.razor.css` file. **Do not leave a `<style>` block in the `.razor` file** — this is a CSS Isolation violation.


1. **`.app-logo`**: Add `font-family: var(--ck-font); letter-spacing: 0.05em;` to the scoped CSS. Prefix the logo text in the HTML with `<span class="logo-prefix">&gt; </span>` — styled in `MainLayout.razor.css` with `color: var(--ck-amber)`.

2. **`.app-header`**: Change `border-bottom` from `2px solid var(--dusk-orange-glow)` to `1px solid var(--ck-border)`. Add `box-shadow: 0 1px 0 0 rgba(234,88,12,0.08)` for the subtle amber scanline underneath.

3. **`.dusk-nav-link::after`** underline: Already amber ✅ — leave unchanged.


---


### Step 3: `Components/Pages/Practice.razor` and `Practice.razor.css` — Cockpit Treatment


All style changes go into `Practice.razor.css`. Remove the existing inline `<style>` block from `Practice.razor` entirely and move all rules to the scoped file.


1. **Add a Cockpit section header** above the task grid in the HTML:

   ```html

   <div class="ck-section-header mb-4">

       <span class="ck-section-prefix">// 01</span>

       <h2 class="ck-section-title">@L["Practice_Title"]</h2>

       <div class="ck-section-line"></div>

   </div>

   ```

   Remove the old `<div class="d-flex align-items-center mb-4">` page header and `feature-icon-wrapper`.


2. **Task card hover:** Change `border-color` from purple `rgba(124,58,237,0.4)` to `rgba(234,88,12,0.4)`.

3. **`.shadow-glow`:** Replace purple `box-shadow` with `0 0 0 1px var(--ck-amber), 0 4px 20px rgba(234,88,12,0.15)`.

4. **Submit button:** Change `class="btn btn-primary"` → `class="btn btn-amber"`.

5. **`.dusk-input`:** Replace with `.ck-input` — change the class on all `<select>` elements in the code editor toolbar.

6. **`.dusk-badge-info`** (difficulty badge): Change from cyan to amber:

   - `background: rgba(234,88,12,0.1); color: var(--ck-amber-bright); border: 1px solid rgba(234,88,12,0.25)`.

7. **`@keyframes blink-green`**: Keep as-is (success feedback, green is semantically correct).


---


### Step 4: `Components/Pages/TutorDashboard.razor` — Cockpit Treatment


1. **Replace page header** `<div class="d-flex justify-content-between">` with the `ck-section-header` pattern:

   ```html

   <div class="ck-section-header mb-5">

       <span class="ck-section-prefix">// 01</span>

       <h2 class="ck-section-title">Platform Overview</h2>

       <div class="ck-section-line"></div>

       <span class="ck-status-dot"></span>

   </div>

   ```


2. **Metric cards:** Add `border-top: 2px solid rgba(234,88,12,0.3)` and `border-radius: 0` to each `.dusk-glass-panel.p-4` card via an inline `style` attribute or a scoped style block.


3. **Feedback table section header** `<h4>Task Feedback Analytics</h4>` → wrap in `ck-section-header`:

   ```html

   <div class="ck-section-header mb-4">

       <span class="ck-section-prefix">// 02</span>

       <span class="ck-section-title">Task Feedback Analytics</span>

       <div class="ck-section-line"></div>

   </div>

   ```


4. **Add a `TutorDashboard.razor.css` scoped file** for amber table styles. Do NOT use `!important` — instead add the class `ck-table` to the `<table>` element in the HTML, then use higher specificity selectors:

   ```css

   .ck-table tbody tr:hover { background: rgba(234,88,12,0.03); }

   .ck-table thead th { border-bottom: 1px solid rgba(234,88,12,0.2); }

   ```

   Also add `.ck-card-top { border-top: 2px solid rgba(234,88,12,0.3); border-radius: 0; }` to the scoped file and apply `class="ck-card-top"` to each metric card `<div>` in the HTML instead of using inline `style="..."` attributes.


---


### Step 5: `Components/Pages/TasksList.razor` — Cockpit Treatment


1. **Replace page header** with `ck-section-header`. Move the "Create New" button to the right of the line:

   ```html

   <div class="ck-section-header mb-4">

       <span class="ck-section-prefix">// 01</span>

       <span class="ck-section-title">@L["Tasks_Title"]</span>

       <div class="ck-section-line"></div>

       <a href="/tasks/create" class="btn btn-amber btn-sm">

           <i class="bi bi-plus me-1"></i>@L["Tasks_CreateNew"]

       </a>

   </div>

   ```


2. **Create `TasksList.razor.css`** and move all styles there. Remove the existing inline `<style>` block from `TasksList.razor`. Update the table styles:

   - `.dusk-table th { color: var(--ck-amber); letter-spacing: 0.12em; }`

   - `.dusk-table tbody tr:hover { background: rgba(234,88,12,0.03); }` — no `!important` needed since `.dusk-table` provides sufficient specificity over Bootstrap.


3. **Edit button:** Change class from `btn-sm dusk-btn-outline` → `btn-sm btn-amber`. Keep `btn-outline-danger` for delete.


4. **Badge style:** Update `.dusk-badge-info` to amber (same as Practice).


---


### Step 6: `Components/Pages/TaskEditor.razor` and `TaskEditor.razor.css` — Cockpit Treatment


1. **Replace page header** with `ck-section-header`.


2. **Form panel** (`dusk-glass-panel`): Add the CSS class `ck-form-panel` to the wrapper `<div>` in the HTML. In `TaskEditor.razor.css`, define: `.ck-form-panel { border-top: 2px solid rgba(234,88,12,0.3); border-radius: 0; }` — **do not use an inline `style="..."` attribute.**


3. **All `dusk-input` classes** → `ck-input`.


4. **Field label style:** Add the class `ck-label` to every `<label>` element in the form. In `TaskEditor.razor.css`, define:
   ```css
   .ck-label {
       font-family: var(--ck-font);
       letter-spacing: 0.1em;
       text-transform: uppercase;
       font-size: 0.72rem;
       color: var(--ck-muted);
   }
   .ck-label::after {
       content: ' //';
       color: var(--ck-amber);
   }
   ```
   Do NOT hardcode `//` in the HTML label text — screen readers would announce it as "slash slash".


5. **Save button:** Change `class="btn btn-primary"` → `class="btn btn-amber"`.


6. **Remove the existing inline `<style>` block** from `TaskEditor.razor`. Move all remaining rules to `TaskEditor.razor.css`.


---


### Step 7: `Components/Pages/AccessDenied.razor` — Cockpit Treatment


Full markup replacement:

```html

<div class="ck-denied-page">

    <div class="ck-denied-inner">

        <div class="ck-denied-prefix">// ACCESS RESTRICTED</div>

        <div class="ck-denied-icon">

            <i class="bi bi-shield-lock-fill"></i>

        </div>

        <h1 class="ck-denied-title">@L["AccessDenied_Heading"]</h1>

        <p class="ck-denied-msg">@L["AccessDenied_Message"]</p>

        <a href="/" class="btn btn-amber">[ RETURN TO BASE ]</a>

    </div>

</div>

```


Add scoped `AccessDenied.razor.css` (new file):

- Full-height centered layout

- `.ck-denied-prefix`: Geist Mono, amber, 0.65rem, letter-spacing 0.2em, blink animation

- `.ck-denied-icon`: `font-size: 3rem; color: var(--ck-amber); opacity: 0.8;`

- `.ck-denied-title`: Geist Mono, 1.5rem, white

- `.ck-denied-msg`: 0.9rem, `color: var(--ck-muted)`


---


## ✅ Verification


Build: `dotnet build DuskOfCoding.slnx --configuration Debug` — 0 errors, 0 warnings.


Manual checks:

1. `/practice` — amber task card borders on hover, amber submit button, amber focus rings on inputs

2. `/tutor` — `// 01 Platform Overview` header, amber top-edge on metric cards

3. `/tasks` — amber table header text, amber "Create" button, amber row hover

4. `/tasks/create` — amber input focus, amber save button, monospace field labels

5. `/access-denied` — amber icon, monospace prefix, amber return button

6. `/cockpit` — unchanged (regression check)

7. `/`, `/how-it-was-made` — **completely unchanged** (Phase 19/20 scope) 