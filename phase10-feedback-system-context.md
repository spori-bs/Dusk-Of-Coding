# User Feedback System Context (State Holder)

> **Goal**: Build a rating & feedback system for students, stored in DB, visible to admins.
> **Source**: `phase5-feedback-system-master-prompt.md`

## 📈 Status tracker

- [ ] **Phase 1: Domain & Persistence**
  - [ ] Create `UserFeedback` entity in Domain layer
  - [ ] Add `DbSet<UserFeedback>` to `AppDbContext`
  - [ ] Create and apply EF Core migration

- [ ] **Phase 2: API Endpoints**
  - [ ] `POST /feedback` with validation and dedup
  - [ ] `GET /feedback/task/{taskId}/summary` (admin)
  - [ ] `GET /feedback/overview` (admin)

- [ ] **Phase 3: UI Components**
  - [ ] Create `FeedbackWidget.razor` with star rating
  - [ ] Integrate widget into Practice page (post-submission)
  - [ ] Add feedback tab to Admin Dashboard
  - [ ] Add HU/EN localization keys

## 📌 Dependencies
- Requires `phase4-rbac-identity` (RBAC must be active for `UserId` and admin endpoints).
- Benefits from `phase3-error-handling` (graceful error recovery for feedback submissions).
