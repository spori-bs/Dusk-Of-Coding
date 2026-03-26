# 🌌 Phase 6: "How It Was Made" – Master Prompt

## 🎯 Role & Objective
**Role:** Senior Frontend Architect & Creative Director specializing in **Blazor Server (.NET 10)** and **Premium UI/UX**.
**Objective:** Create a cinematic, scroll-driven "How It Was Made" page (`/how-it-was-made`) for the **Dusk of Coding** platform. 

This page serves as a technical manifesto and teaser. It must explain the "Vibecoding" methodology—where the human architects the system and the AI builds the capabilities—while maintaining a high-end, mysterious, and technical aesthetic.

---

## 🎨 Visual & Design Language
- **Theme:** Dark Mode with **Glassmorphism** (Background blur: 12px, border-opacity: 0.1).
- **Aesthetic:** "Modern Dark" using deep space gradients and vibrant neon accents (Cyan/Purple).
- **Animations:** - Use native **CSS View-Timeline** for scroll-driven reveals (no heavy JS).
    - Implement "Tracking-in" text animations for major headings.
    - Interactive **3D-tilt effects** on hover for "Prompt Cards."
- **Typography:** Bold, wide-spaced sans-serif for titles; monospaced fonts for technical snippets.

---

## 🏗️ Page Structure (The Narrative Timeline)

Implement the following sections as the user scrolls:

### 1. The Hero (The Big Bang)
- **Title:** "The Twilight of Manual Coding."
- **Sub-headline:** "How we built a platform by wielding AI as a high-frequency power tool."
- **Visual:** A shimmering gradient background with a "shredded code" particle effect.

### 2. The Spark (Phases 1-2)
- **Focus:** The core Roslyn engine and the Sandbox.
- **Narrative:** Explain the birth of the execution environment. 
- **Component:** A visual representation of the "Heart of the System" (The Secure Compilation Service).

### 3. The Language (Phase 3 - Localization)
- **Focus:** Bilingual support and global reach.
- **Narrative:** "Teaching the platform to speak."
- **Visual:** A floating globe or dynamic text translation effect showing English and Hungarian strings swapping.

### 4. The Identity (Phase 4 - Keycloak)
- **Focus:** Security and IAM.
- **Narrative:** "Securing the perimeter."
- **Visual:** A "Holographic Perimeter" animation representing Keycloak’s OIDC protection.

### 5. The Evolution (Rebranding)
- **Interaction:** An **Interactive Comparison Slider** component.
- **Content:** Show the transformation from the original "Generic Sidebar UI" to the "Dusk Glassmorphism" layout.

### 6. The Prompt Gallery
- **Component:** **Holographic Prompt Cards**. 
- **Content:** Display snippets of actual prompts (e.g., the Master Prompt, the Localization Prompt).
- **Style:** Cards should look like floating glass artifacts with syntax-highlighted code.

### 7. The Manifesto (The Future)
- **Content:** "Master the craft. Wield the tool." 
- **CTA:** A high-contrast "Join the New Dawn" button redirecting to `/register`.

---

## 🛠️ Technical Implementation Details
- **Framework:** Blazor Server on .NET 10.
- **Localization:** Use `IStringLocalizer<HowItWasMade>` for all text.
- **Architecture:** Keep the UI logic in `HowItWasMade.razor` and scoped CSS in `HowItWasMade.razor.css`.
- **Performance:** Use SVGs for diagrams. Avoid large PNGs to maintain a high "Lighthouse" score.

---

## 🌐 Localization Table (HU / EN)

| Key | English (EN) | Hungarian (HU) |
| :--- | :--- | :--- |
| `PageTitle` | How It Was Made | Így készült |
| `HeroSubtitle` | A journey from manual lines to AI-driven mastery. | Utazás a kézi kódolástól az AI-vezérelt szakértelemig. |
| `VibeManifesto` | We don't just write code; we wield intent. | Nem csak kódot írunk; szándékot irányítunk. |
| `SparkTitle` | The Spark | A Szikra |
| `LanguageTitle` | The Language | A Nyelv |
| `IdentityTitle` | The Identity | Az Identitás |
| `EvolutionTitle` | The Evolution | Az Evolúció |
| `JoinPlatform` | Join the New Dawn | Csatlakozz az új hajnalhoz |