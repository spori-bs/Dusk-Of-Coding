# Rebrand Page Modification Master Prompt

You are an **Expert Blazor Developer, UI/UX Architect, and Conversion Rate Optimization Specialist operating in 2026**.

## 🎯 Objective
Redesign the **"Dusk of Coding"** landing page and the broader Blazor web application to fully represent the newly established brand identity. The design must be breathtaking, modern ("fresh for 2026"), and intuitively simple to navigate. 

Following the landing page overhaul, the core app pages (Practice UI, Task Management, AI Tutor Terminal) must be updated to inherit the same premium aesthetics, ensuring a seamless visual journey from the front door to the internal platform workspace.

---

## 🎨 Design System & Aesthetics (The "Dusk" Theme)
The application must feel premium, state-of-the-art, and emotionally resonant with the "Dusk to Dawn" concept.

- **Color Palette**: Deep twilight purples (`#1e1b4b`), warm horizon oranges (`#ea580c`), transitioning into a sleek, dark-mode-first aesthetic (`#0f172a` backgrounds) that represents the night.
- **Modern 2026 UI Trends**: Employ glassmorphism (translucent panels with background blur), soft volumetric shadows, smooth gradients, and subtle micro-animations on hover/click to make the interface feel "alive".
- **Typography**: Clean, highly readable, modern geometric sans-serif (e.g., Inter, Figtree, or Outfit).
- **Simplicity**: Keep cognitive load exceptionally low. The UI should be "easy to understand" at first glance—no cluttered dashboards.

---

## 🌍 Language & Localization Requirements (CRITICAL)
The application is bilingual (English and Hungarian). 
- **Exceptional Hungarian**: The Hungarian phrases must NOT sound like machine translations. They must be natural, highly engaging, and grammatically perfect for a modern SaaS platform.
- **Punchy English**: The English copy must remain concise, impactful, and clearly articulate the "AI is a tool, not a brain" philosophy.
- **Action**: You must update the `SharedResource.hu.resx` and `SharedResource.en.resx` files alongside UI changes to reflect this high-fidelity copy.

---

## 🛠️ Technical Guidelines & Safety
- **DO NOT Break Links or Routing**: The Blazor `@page` directives, `href` navigation links, component parameters, and existing SignalR/API integrations must remain fully intact.
- **Component Architecture**: Keep the UI modular. If you create new visual sections, encapsulate them in cleanly named `.razor` components.
- **CSS Management**: Use scoped CSS (`.razor.css`) or the shared `app.css` systematically. Do not introduce conflicting inline styles unless necessary for dynamic rendering.

---

## 📋 Execution Plan

### Phase 1: The Landing Page (`Home.razor` & Landing Components)
Refactor the landing layout and its sub-components (`HeroComponent`, `FeaturesComponent`, `MediaDemoComponent`, `SocialProofComponent`, `LeadFormComponent`).
- Create a stunning, full-height Hero section with dramatic dusk gradients and a clear Call-to-Action.
- Ensure the "Features" vividly explain the transition era of coding and the value of sandboxed, AI-assisted practice.

### Phase 2: The App Layout & Navigation (`MainLayout.razor` & `NavMenu.razor`)
- Overhaul the main application shell. Replace basic gray/white structures with the dark twilight theme.
- Modernize the navigation bar to be frictionless, perhaps adopting a sleek top-app-bar or a floating side-dock approach.
- Update the `LanguageSelector` component to match the new premium feel.

### Phase 3: Core Application Pages (`Practice.razor`, `TasksList.razor`, `TaskEditor.razor`)
- Style the integrated `BlazorMonaco` code editor container to sit flush within the dark mode aesthetic.
- Modernize data tables/lists in `TasksList` to look like premium SaaS cards or cleanly spaced rows with micro-animations.
- Update the `TutorTerminal.razor` to look like a futuristic, clean command-line or chat interface where the AI Mentor resides.

### Phase 4: Localization Sweep (`.resx` files)
- Review every new UI string introduced during the design phase.
- Ensure the Hungarian translation in `SharedResource.hu.resx` uses perfect marketing and technical terminology (e.g., "Kódolás hajnala", "MI Mentor", "Gyakorlótér").

---

**Begin your execution by confirming you have read the architecture in `rebrand-context.md` and then proceed to Phase 1: Landing Page Refactor.**
