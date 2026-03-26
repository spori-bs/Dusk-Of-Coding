# User Feedback System - Master Prompt

## Objective
Implement a lightweight feedback system that allows students to rate tasks, report issues, and provide free-form comments. Feedback is stored in the database, tied to the authenticated user, and visible to admins for continuous platform improvement.

## Context
The platform currently has no mechanism for students to give feedback on tasks. Adding this closes the loop between task creation (by admins) and task consumption (by students), providing valuable data for:
- Identifying confusing or poorly-worded tasks.
- Understanding which tasks are too easy or too hard.
- Detecting bugs in test cases or execution behavior.

### Prerequisites
- RBAC must be in place (`phase4-rbac-identity`) so that feedback is always tied to an authenticated `UserId`.
- The Error Boundary (`phase3-error-handling`) should be active to catch any feedback submission errors gracefully.

## Implementation Roadmap

### Phase 1: Domain & Persistence
1. **`UserFeedback` Entity** (Domain layer):
   ```csharp
   public class UserFeedback
   {
       public Guid Id { get; set; }
       public Guid TaskId { get; set; }
       public Guid UserId { get; set; }
       public int Rating { get; set; }           // 1-5 stars
       public string? Comment { get; set; }       // Optional free-form text
       public string FeedbackType { get; set; }   // "rating", "bug_report", "suggestion"
       public DateTime CreatedAt { get; set; }
   }
   ```
2. **EF Core Configuration**:
   - Add `DbSet<UserFeedback>` to `AppDbContext`.
   - Create migration.
   - Index on `(TaskId, UserId)` for efficient per-task queries.

### Phase 2: API Endpoints
1. **`POST /feedback`** — `[Authorize]`:
   - Accept: `{ taskId, rating, comment?, feedbackType }`.
   - Extract `UserId` from JWT claim.
   - Validate: rating 1-5, comment max 2000 chars.
   - Prevent duplicate ratings (one rating per user per task; updates replace).
2. **`GET /feedback/task/{taskId}/summary`** — `[Authorize(Roles = "admin")]`:
   - Returns: average rating, total feedback count, rating distribution (1-5), recent comments.
3. **`GET /feedback/overview`** — `[Authorize(Roles = "admin")]`:
   - Returns: platform-wide feedback summary, tasks sorted by lowest rating (attention needed).

### Phase 3: UI Components
1. **`FeedbackWidget.razor`** (new component):
   - Appears at the bottom of the Practice page after a successful submission.
   - Star rating (1-5, interactive hover effect).
   - Optional comment textarea (collapsible, expands on click).
   - Feedback type selector: "Rate this task" / "Report a bug" / "Suggestion".
   - Submit button with loading state.
   - Success toast/notification after submission.
2. **Admin Dashboard Integration**:
   - Add a "Feedback" tab/section to the Admin Dashboard.
   - Show a sortable table: Task Name | Avg Rating | Feedback Count | Action (view details).
   - Clicking a task shows its detailed feedback with comments.

## UX & Design Guidelines
- **Star Rating**: Use filled/unfilled star icons (Bootstrap Icons `bi-star-fill` / `bi-star`). Orange accent color for filled stars.
- **Widget Appearance**: Subtle glassmorphism card, slides up with a micro-animation after successful code submission.
- **Non-Intrusive**: The widget should NOT block the practice flow. It appears below the results, not as a modal.
- **Localization**: All labels must use `IStringLocalizer<SharedResource>`. Add keys for both HU and EN.
