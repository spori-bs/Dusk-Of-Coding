# 🌌 How It Was Made Context (State Holder)

> **Goal**: Create a cinematic, scroll-driven storytelling page at `/how-it-was-made` that serves as a technical manifesto for the "Vibecoding" journey.
> **Source**: `phase6-how-it-was-made-master-prompt.md`

## 📈 Status Tracker

- [ ] **Phase 1: Cinematic Foundation & Hero**
  - [ ] Implement `HowItWasMade.razor` route and basic layout structure.
  - [ ] Configure **CSS View-Timeline** for native scroll-driven animations (avoiding heavy JS).
  - [ ] Build **"The Big Bang" Hero Section** featuring "Tracking-in" text animations and gradient shredding effects.

- [ ] **Phase 2: Narrative Timeline Sections**
  - [ ] **The Spark**: Visualize the Roslyn Compilation engine & Sandbox (Phases 1-2 logic).
  - [ ] **The Identity**: Create the "Holographic Perimeter" visual for Keycloak/OIDC integration (Phase 6 logic).
  - [ ] **The Evolution**: Build the **Interactive Comparison Slider** (Phase 5: Rebrand) to show Generic UI vs. Dusk UI.

- [ ] **Phase 3: Interactive Artifacts & Manifesto**
  - [ ] Build `PromptCard.razor` component with **Glassmorphism** and **3D-tilt** hover effects.
  - [ ] Populate the **Prompt Gallery** using snippets from the `_ArchivePrompts` directory.
  - [ ] Implement the **"Vibecoding Manifesto"** and the "Join the New Dawn" CTA.
  - [ ] Finalize HU/EN localization via `IStringLocalizer` for all narrative text.

---

## 🛠️ Technical Stack Alignment
- **Framework**: .NET 10 / Blazor Server.
- **Styling**: Scoped CSS (`HowItWasMade.razor.css`) utilizing `view-timeline` and `animation-range`.
- **UI Patterns**: Glassmorphism (`backdrop-filter: blur(12px)`), 3D Transforms (hover), and SVG-based technical diagrams.
- **Localization**: Bilingual support (HU/EN) following the project's established `.resx` patterns.

---

## 🌌 Narrative Beats
1. **The Big Bang**: The industry shift from manual coding to high-level AI orchestration.
2. **The Spark**: Building the "heart" of the platform—safe code execution via Roslyn/Docker.
3. **The Perimeter**: Securing the vision and user data with Keycloak IAM.
4. **The New Dawn**: The aesthetic evolution into the "Dusk" glassmorphism identity.
5. **The Methodology**: Defining "Vibecoding"—mastery of the craft through directing AI with intent.

---

## 📝 Key Design Tokens
- **Primary Colors**: Vibrant Cyan to Deep Purple gradients.
- **Glass Effect**: `backdrop-filter: blur(12px)` with `rgba(255, 255, 255, 0.1)` borders.
- **Animations**: Scroll-driven entrance fades, scale transitions, and 3D object rotations on hover.