# UI Overhaul Context — Dusk of Coding

> **Last updated**: 2026-03-18  
> **Phase**: UI Overhaul (Dusk Theme)  
> **Branch**: `feature/rebranding-dusk-of-coding`

---

## 🎨 Theme Guidelines
The application must transition from a basic Bootstrap look to a premium 2026 aesthetic.
- **Backgrounds**: Dark mode primary (`#0f172a`, `#09090b`).
- **Accents**: Deep twilight purples (`#1e1b4b`, `#2e1065`, `#4c1d95`) and warm horizon oranges (`#c2410c`, `#ea580c`).
- **Typography**: Clean, sans-serif (Inter/Outfit style).
- **Cards & Surfaces**: Glassmorphism (semi-transparent dark panels with `backdrop-filter: blur()`), subtle colored borders for depth, soft volumetric shadows instead of flat elements.

## 🛠️ Architecture Boundaries
- **Framework**: Blazor Server (.NET 10).
- **CSS Hierarchy**: 
  - Define root variables in `DuskOfCoding.WebUi/wwwroot/app.css`.
  - Use scoped CSS (`.razor.css`) for component-specific styling (glass panels, micro-animations).
  - Bootstrap 5 utility classes are available (Flexbox, spacing, grid) but visual styling (borders, backgrounds) should rely on custom theme variables.
- **Routing & Interactivity**: Do not change `@page` routes or the `@rendermode InteractiveServer` instructions. Preserve all API bindings and signalR logic.

## 📈 Status
- [x] **Phase 1: Landing Page** (Completed - Glassmorphism, premium Hero, Social Proof, and Lead Form)
- [x] **Phase 2: App Shell** (Completed - Sticky Nav, blurred headers, dark-translucent layout)
- [x] **Phase 3: Core Pages** (Completed - Practice platform, Task List/Editor contrast fixes)
- [x] **Phase 4: Localization** (Completed - Full EN/HU support in .resx and UI)
