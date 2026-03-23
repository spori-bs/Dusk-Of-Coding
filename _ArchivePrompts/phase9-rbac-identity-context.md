# RBAC & Identity UX Context (State Holder)

> **Goal**: Implement admin/student roles, refine auth-aware UI, and build an Admin Dashboard.
> **Source**: `phase4-rbac-identity-master-prompt.md`

## 📈 Status tracker

- [x] **Phase 1: Keycloak Role Configuration**
  - [x] Add `admin`/`student` realm roles to `realm.json`
  - [x] Add realm role protocol mapper to JWT claims
  - [x] Configure backend `RoleClaimType` mapping in WebApi
  - [x] Configure frontend claims transformation in WebUi
  - [x] Apply `[Authorize(Roles = "admin")]` to task management endpoints

- [x] **Phase 2: Navigation & Landing Page Refactor**
  - [x] Wrap "Manage Tasks" in `<AuthorizeView Roles="admin">`
  - [x] Add Login link for unauthenticated users in NavMenu
  - [x] Refactor Hero CTAs with `<AuthorizeView>`
  - [x] Hide LeadFormComponent for authenticated users
  - [x] Add `<Authorizing>` skeleton templates

- [x] **Phase 3: Admin Dashboard**
  - [x] Create `/admin` page with aggregate analytics
  - [x] Implement `GET /admin/stats` WebApi endpoint
  - [x] Add "Dashboard" nav link for admin role
