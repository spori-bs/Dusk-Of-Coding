# Error Handling & Resilience Context (State Holder)

> **Goal**: Replace default Blazor error bar with premium error UX and handle 401/403 gracefully.
> **Source**: `phase3-error-handling-master-prompt.md`

## 📈 Status tracker

- [x] **Phase 1: Global Error Boundary**
  - [x] Add `<ErrorBoundary>` wrapper in `MainLayout.razor`
  - [x] Create `CustomErrorContent.razor` with branded glassmorphism design
  - [x] Wire "Try Again" button to `ErrorBoundary.Recover()`

- [x] **Phase 2: Graceful 401/403 Handling**
  - [x] Create `AuthDelegatingHandler` in `WebUi/Services/`
  - [x] Register handler in `ApiClient` HttpClient pipeline
  - [x] Create branded `/access-denied` page
  - [x] Harden `ApiClient` with try-catch fallbacks

- [x] **Phase 3: Validation & Polish**
  - [x] Verify expired token → login redirect flow
  - [x] Verify ErrorBoundary catches SignalR exceptions
  - [x] Confirm no raw stack traces in production console
