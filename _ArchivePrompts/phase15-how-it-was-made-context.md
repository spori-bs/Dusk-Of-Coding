# 🌌 Phase 15: "How It Was Made" Context (State Holder)

> **Goal**: Create a cinematic, scroll-driven storytelling page at `/how-it-was-made` that serves as a technical manifesto for the "Vibecoding" journey.
> **Source**: `phase101-how-it-was-made-master-prompt.md`

## 📈 Status Tracker

- [ ] **Phase 1: Cinematic Foundation & Hero**
  - [ ] Implement `HowItWasMade.razor` route and basic layout structure.
  - [ ] Configure **CSS View-Timeline** for native scroll-driven animations (avoiding heavy JS).
  - [ ] Build **"The Big Bang" Hero Section** featuring "Tracking-in" text animations and gradient shredding effects.

- [ ] **Phase 2: Narrative Timeline Sections**
  - [ ] **The Spark**: Visualize the Roslyn Compilation engine & Sandbox (Phases 1-2 logic).
  - [ ] **The Language**: Implement the "Global Reach" visual representing the App's Bilingual (HU/EN) capability (Phase 3 logic).
  - [ ] **The Aesthetic**: Build the **Interactive Comparison Slider** (Rebranding) to show Generic UI vs. Dusk UI (Phases 4-5 logic).
  - [ ] **The Fortress & Persistence**: Create the "Holographic Perimeter" visual for Keycloak/OIDC integration interlocking with a pulsing database core (Phases 6-10, 13 logic).
  - [ ] **The Abyss**: Design the "System Failure" glitch sequence showing HTTP-431 infinite loops and EF Core concurrency crashes, resolving into clean code via glowing "Master Root-Cause Prompts."
  - [ ] **The Mentor**: Implement a glowing neural network intersecting with code blocks representing the Socratic AI Tutor integration (Phases 11-15 logic).

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
3. **The Language**: Teaching the platform to speak—bilingual support and global accessibility.
4. **The Aesthetic**: The evolution into the "Dusk" glassmorphism UI.
5. **The Fortress**: Securing the vision and preserving data through Keycloak IAM and EF Core.
6. **The Abyss**: The brutal reality of AI-generated architecture—where "Vibecoding" crashed, but was salvaged by crafting laser-focused, isolated external AI prompts to dissect root causes and execute surgical solutions.
7. **The Mentor**: Transforming into a true AI-assisted learning platform with a Socratic Tutor.
8. **The Methodology**: Defining "Vibecoding"—mastery of the craft through directing AI with intent.

---

## 📝 Key Design Tokens
- **Primary Colors**: Vibrant Cyan to Deep Purple gradients.
- **Glass Effect**: `backdrop-filter: blur(12px)` with `rgba(255, 255, 255, 0.1)` borders.
- **Animations**: Scroll-driven entrance fades, scale transitions, and 3D object rotations on hover.