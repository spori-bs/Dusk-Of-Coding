# RBAC & Identity UX Context (State Holder)

> **Goal**: Implement admin/student roles, refine auth-aware UI, and build an Admin Dashboard.
> **Source**: `phase4-rbac-identity-master-prompt.md`

## 📈 Status tracker

- [ ] **Phase 1: Keycloak Role Configuration**
  - [ ] Add `admin`/`student` realm roles to `realm.json`
  - [ ] Add realm role protocol mapper to JWT claims
  - [ ] Configure backend `RoleClaimType` mapping in WebApi
  - [ ] Configure frontend claims transformation in WebUi
  - [ ] Apply `[Authorize(Roles = "admin")]` to task management endpoints

- [ ] **Phase 2: Navigation & Landing Page Refactor**
  - [ ] Wrap "Manage Tasks" in `<AuthorizeView Roles="admin">`
  - [ ] Add Login link for unauthenticated users in NavMenu
  - [ ] Refactor Hero CTAs with `<AuthorizeView>`
  - [ ] Hide LeadFormComponent for authenticated users
  - [ ] Add `<Authorizing>` skeleton templates

- [ ] **Phase 3: Admin Dashboard**
  - [ ] Create `/admin` page with aggregate analytics
  - [ ] Implement `GET /admin/stats` WebApi endpoint
  - [ ] Add "Dashboard" nav link for admin role
