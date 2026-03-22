# Error Handling & Resilience Context (State Holder)

> **Goal**: Replace default Blazor error bar with premium error UX and handle 401/403 gracefully.
> **Source**: `phase3-error-handling-master-prompt.md`

## 📈 Status tracker

- [ ] **Phase 1: Global Error Boundary**
  - [ ] Add `<ErrorBoundary>` wrapper in `MainLayout.razor`
  - [ ] Create `CustomErrorContent.razor` with branded glassmorphism design
  - [ ] Wire "Try Again" button to `ErrorBoundary.Recover()`

- [ ] **Phase 2: Graceful 401/403 Handling**
  - [ ] Create `AuthDelegatingHandler` in `WebUi/Services/`
  - [ ] Register handler in `ApiClient` HttpClient pipeline
  - [ ] Create branded `/access-denied` page
  - [ ] Harden `ApiClient` with try-catch fallbacks

- [ ] **Phase 3: Validation & Polish**
  - [ ] Verify expired token → login redirect flow
  - [ ] Verify ErrorBoundary catches SignalR exceptions
  - [ ] Confirm no raw stack traces in production console
