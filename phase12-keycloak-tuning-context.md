# Keycloak Tuning Context (State Holder)

> **Goal**: Refine Keycloak integration, robustly handle HTTP 431, and perfect the UI/UX around authentication states.
> **Source**: `phase12-keycloak-tuning-master-promt.md`

## 📈 Status tracker

- [ ] **Phase 1: Keycloak Architecture Review**
  - [ ] Analyze current HTTP 431 solutions and JWT chunking
  - [ ] Implement robust load-balancing/Podman setup fixes
  - [ ] Rethink and harden auth security scenarios

- [ ] **Phase 2: Authorization & UI Intelligence**
  - [ ] Hide unavailable functions (e.g. task management) via RBAC capabilities
  - [ ] Fix Access Denied page localization and routing for direct unauthorized links
  - [ ] Eliminate visual glitches (e.g. double login buttons on inaccessible sites)

- [ ] **Phase 3: Login UX & Smart Navigation**
  - [ ] Replace "Get Started" terminology with a clear "Login" button
  - [ ] Implement smart redirects back to the referring page (e.g., Practice page) after login
  - [ ] Display clearly visible User ID on all pages post-login
  - [ ] Add clearly visible sign/indicator for privileged (e.g., admin) users

## 📌 Dependencies
- Depends on previous Keycloak base and RBAC implementation phases.
- Requires robust logic within Blazor `AuthorizeRouteView` and `AuthenticationStateProvider`.
