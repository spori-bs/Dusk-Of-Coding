# ⚡ Phase 17 — Engineering Cockpit: Context & Progress

## Status: ✅ Complete

## Files to Create / Modify

| File | Status |
|---|---|
| `DuskOfCoding.WebUi/Components/Pages/Cockpit.razor` | ✅ Created |
| `DuskOfCoding.WebUi/Components/Pages/Cockpit.razor.css` | ✅ Created |
| `DuskOfCoding.WebUi/Components/Pages/Cockpit.razor.js` | ✅ Created |
| `DuskOfCoding.WebUi/Components/Layout/NavMenu.razor` | ✅ Updated |

## Key Design Decisions

- **Route:** `/cockpit`
- **Layout:** `MainLayout` (existing sticky header, dark bg)
- **JS Pattern:** Blazor JS Isolation — `Cockpit.razor.js` collocated, ES Module syntax, `IAsyncDisposable`
- **Animations:** CSS `@keyframes` only — no framer-motion, no canvas API
- **Font:** `Geist Mono` imported in scoped CSS
- **New CSS tokens:** Scoped to `Cockpit.razor.css`, do not modify `app.css`
- **No new NuGet or npm packages**
- **No localization** — English hardcoded

## Progress Log

- [x] Cockpit.razor created — hero, status grid, wave, manifesto
- [x] Cockpit.razor.css created — full Obsidian Foundry theme, all keyframes
- [x] Cockpit.razor.js created — ES Module, PCB mousemove via rAF, no window pollution
- [x] NavMenu.razor updated — Cockpit link with bi-cpu icon, amber (text-warning)

## Verification Checklist (run after app start)

1. [ ] `/cockpit` renders without errors while logged in
2. [ ] PCB lines light up on mouse proximity
3. [ ] Sub-headline de-blurs on hero hover
4. [ ] 4 panels collapse/expand with snap animation
5. [ ] Wave changes shape/color on mode select
6. [ ] Nav shows "Cockpit" link for authenticated users
7. [ ] Existing pages unbroken (`/`, `/practice`, `/tutor`, `/how-it-was-made`)
8. [ ] Geist Mono font loads (check DevTools Network tab)

