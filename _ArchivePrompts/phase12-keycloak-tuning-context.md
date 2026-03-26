# Keycloak Tuning Context (State Holder)

> **Goal**: Refine Keycloak integration, robustly handle HTTP 431, and perfect the UI/UX around authentication states.
> **Source**: `phase12-keycloak-tuning-master-promt.md`

## 📈 Status tracker

- [x] **Phase 1: Keycloak Architecture Review**
  - [x] Analyze current HTTP 431 solutions and JWT chunking
  - [x] Implement robust load-balancing/Podman setup fixes
  - [x] Rethink and harden auth security scenarios

- [x] **Phase 2: Authorization & UI Intelligence**
  - [x] Hide unavailable functions (e.g. task management) via RBAC capabilities
  - [x] Fix Access Denied page localization and routing for direct unauthorized links
  - [x] Eliminate visual glitches (e.g. double login buttons on inaccessible sites)

- [x] **Phase 3: Login UX & Smart Navigation**
  - [x] Replace "Get Started" terminology with a clear "Login" button
  - [x] Implement smart redirects back to the referring page (e.g., Practice page) after login
  - [x] Display clearly visible User ID on all pages post-login
  - [x] Add clearly visible sign/indicator for privileged (e.g., admin) users

## 📌 Dependencies
- Depends on previous Keycloak base and RBAC implementation phases.
- Requires robust logic within Blazor `AuthorizeRouteView` and `AuthenticationStateProvider`.
