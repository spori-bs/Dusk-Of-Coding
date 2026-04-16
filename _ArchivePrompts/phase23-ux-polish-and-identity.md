# 🎨 Phase 23: UX Polish & Identity — Header Unification, Notifications & User Profile

## 🎯 Role & Objective

**Role:** Senior Full-Stack Blazor Architect (.NET 10, Blazor Server, SignalR, Keycloak OIDC).

**Objective:** Execute a focused UX polish sprint addressing seven outstanding issues from the product backlog. This phase touches the `LoginDisplay`, `NavMenu`, `MainLayout`, `TaskEditor`, `Practice`, and page-level components. The goal is to unify the header bar to the **Obsidian Foundry** design language, fix the broken AI test-generation notification flow, add server-side test deletion, introduce a proper user identity dropdown with role badges, eliminate the duplicate login button flash, and upgrade the syntax-check feedback to a cinematic laser-scanner HUD effect.

*(Note: No domain entities or database schema changes are required. All work is confined to the WebUi project and minor WebApi endpoint additions.)*

---

## 🗑️ Part 1: Server-Side Test Case Deletion

### Problem
Generated test cases in `TaskEditor.razor` can only be removed from the client-side list via `RemoveTest()`. There is **no backend API endpoint** to delete a persisted `TaskTest` entity. If a user removes a test file from the sidebar and saves, the orphaned test may remain in the database because the Update logic may not handle removals.

### Step 1: WebApi — Delete Endpoint
Create a new endpoint in the Task controller (or a dedicated `TaskTestController`):
- **Route:** `DELETE /api/tasks/{taskId}/tests/{testId}`
- **Logic:** Validate the task exists and the test belongs to the task. Remove the `TaskTest` entity. Return `204 No Content`.
- **Authorization:** `[Authorize(Roles = "tutor,admin")]`

### Step 2: ApiClient — Wire Up
Add to `ApiClient.cs`:
```csharp
public async Task<Result> DeleteTestAsync(Guid taskId, Guid testId)
{
    var response = await _httpClient.DeleteAsync($"api/tasks/{taskId}/tests/{testId}");
    // ... standard error handling
}
```

### Step 3: TaskEditor — Call Backend on Remove
Update `RemoveTest(TaskTestDto test)` in `TaskEditor.razor`:
```csharp
private async Task RemoveTest(TaskTestDto test)
{
    // If persisted (has an Id), delete from backend first
    if (test.Id.HasValue && test.Id != Guid.Empty && IsEditMode)
    {
        var result = await Api.DeleteTestAsync(Id!.Value, test.Id.Value);
        if (!result.IsSuccess)
        {
            _errorMessage = result.ErrorMessage;
            return;
        }
    }

    _model.Tests.Remove(test);
    if (_selectedTest == test)
    {
        _selectedTest = null;
        _isEditorReady = false;
        if (_model.Tests.Any())
        {
            await SelectTest(_model.Tests.First());
        }
    }
}
```

---

## 🎨 Part 2: Header & Menu Bar Style Unification

### Problem
The Task list page (`TasksList.razor`) and the Practice page (`Practice.razor`) still use the legacy `dusk-glass-panel` class with glassmorphism styling (blurred backgrounds, rounded borders). These must be updated to the **Obsidian Foundry** design language: hard edges, dark surfaces, industrial amber accents — matching the Cockpit and TaskEditor pages.

### Step 1: Audit All Pages
Identify every page that still uses `dusk-glass-panel` on structural containers (cards, table wrappers, form panels). The following pages are known to use it:
- `Practice.razor` — task cards, submission panel
- `TasksList.razor` — table wrapper, error alerts
- `TaskEditor.razor` — form panel, error alerts
- `TutorDashboard.razor` — dashboard cards

### Step 2: Replace Glass Panels
For each affected page, replace `dusk-glass-panel` with the Cockpit-standard class `ck-form-panel` (or a new utility class `ck-panel`). Ensure the replacement class uses:
```css
.ck-panel {
    background: var(--ck-bg);
    border: 1px solid var(--ck-border-dim);
    border-radius: 2px;          /* Hard edges — NO rounded corners > 4px */
    padding: 0;
}
```

### Step 3: Practice Page — Monaco Editor Theme
The Practice page Monaco editor currently uses `Theme = "vs"` (light mode). This clashes with the dark Obsidian Foundry aesthetic. Change it to `Theme = "vs-dark"` to match the TaskEditor.

---

## 🔔 Part 3: Test Generation Notification Fix

### Problem
After the AI test suite generation completes successfully in the `TutorWorker`, the results are correctly saved to the database. However, the user only sees the **"GENERATING..."** spinner indefinitely. The SignalR `TestSuiteGenerationCompleted` event either:
1. Is never sent from the Worker, or
2. Is sent but the `TaskEditor` SignalR connection isn't established/subscribed in time.

### Step 1: Verify Worker Sends Notification
In the `TutorWorker` consumer that handles `GenerateTestSuiteCommand`, confirm the following SignalR call exists **after** the database save:
```csharp
await _hubContext.Clients.User(command.UserId)
    .SendAsync("TestSuiteGenerationCompleted", command.TaskId, true, "Test_Suite_Success_Key");
```
If this call is missing, add it. Similarly, add the error path notification in the `catch` block.

### Step 2: Verify Hub Authentication
The SignalR hub must map `User.Identity.Name` to the Keycloak `sub` claim (or `preferred_username`). Ensure the `IUserIdProvider` is correctly configured so that `Clients.User(userId)` targets the right connection.

### Step 3: Add Polling Fallback (Resilience)
As a safety net, add a timeout-based fallback in `TaskEditor.razor`. If the SignalR event doesn't arrive within **60 seconds**, poll the API to check if tests were generated:
```csharp
private CancellationTokenSource? _generationTimeoutCts;

// In GenerateTestSuite(), after setting _isGeneratingTests = true:
_generationTimeoutCts?.Cancel();
_generationTimeoutCts = new CancellationTokenSource();
_ = PollForGenerationComplete(_generationTimeoutCts.Token);

private async Task PollForGenerationComplete(CancellationToken ct)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(60), ct);
        if (!ct.IsCancellationRequested && _isGeneratingTests && IsEditMode)
        {
            await ReloadTaskData();
            _isGeneratingTests = false;
            await InvokeAsync(StateHasChanged);
            await ToastService.ShowInfo(L["TaskEditor_GenerationPollComplete"].Value);
        }
    }
    catch (TaskCanceledException) { /* SignalR arrived first — expected */ }
}
```
Cancel the timeout CTS in the SignalR callback when the event arrives normally.

---

## 👤 Part 4: Role Badge Display Fix

### Problem
Both the **Tutor** and **Sysadmin** (Admin) roles currently display the same generic **"Administrator"** badge in the top-right corner (`LoginDisplay.razor`). The original plan was to show role-specific badges: `Student`, `Tutor`, and `Administrator`.

### Step 1: Update LoginDisplay.razor
The current `LoginDisplay.razor` already has the correct `if/else if` logic checking `IsInRole(AppRoles.Admin)` and `IsInRole(AppRoles.Tutor)`. The issue is likely that the localization keys `Auth_AdminBadge` and `Auth_TutorBadge` both resolve to "Administrator". 

Verify and fix the resource files:
- `SharedResource.hu.resx`: `Auth_AdminBadge` → `"Adminisztrátor"`, `Auth_TutorBadge` → `"Oktató"`
- `SharedResource.en.resx`: `Auth_AdminBadge` → `"Administrator"`, `Auth_TutorBadge` → `"Tutor"`

### Step 2: Add Student Badge
Add a third badge case for students who are logged in but have neither Tutor nor Admin roles:
```razor
@if (context.User.IsInRole(AppRoles.Admin))
{
    <span class="badge bg-danger text-white me-2 px-2 py-1" style="font-size: 0.75rem;">
        <i class="bi bi-shield-lock-fill me-1"></i>@L["Auth_AdminBadge"]
    </span>
}
else if (context.User.IsInRole(AppRoles.Tutor))
{
    <span class="badge bg-warning text-dark me-2 px-2 py-1" style="font-size: 0.75rem;">
        <i class="bi bi-mortarboard-fill me-1"></i>@L["Auth_TutorBadge"]
    </span>
}
else
{
    <span class="badge bg-info text-dark me-2 px-2 py-1" style="font-size: 0.75rem;">
        <i class="bi bi-person-fill me-1"></i>@L["Auth_StudentBadge"]
    </span>
}
```

### Step 3: Localization Keys
Add the new key:
- `Auth_StudentBadge` → `"Student"` (en) / `"Hallgató"` (hu)

---

## 👤 Part 5: User Profile Dropdown & Username Display

### Problem
The logged-in user's name is not visible in the header on mobile viewports (`d-none d-md-inline` hides it). Also, there is no dropdown menu for profile actions — only a flat "Logout" button.

### Step 1: Replace Flat Layout with Dropdown
Replace the current `LoginDisplay.razor` authorized section with a Bootstrap dropdown:
```razor
<Authorized>
    <div class="dropdown">
        <button class="btn btn-sm dusk-btn-outline dropdown-toggle d-flex align-items-center gap-2" 
                type="button" data-bs-toggle="dropdown" aria-expanded="false" id="userProfileDropdown">
            <i class="bi bi-person-circle"></i>
            <span class="d-none d-sm-inline">@context.User.Identity?.Name</span>
            @* Role badge inline *@
            @if (context.User.IsInRole(AppRoles.Admin))
            {
                <span class="badge bg-danger ms-1" style="font-size: 0.65rem;">@L["Auth_AdminBadge"]</span>
            }
            else if (context.User.IsInRole(AppRoles.Tutor))
            {
                <span class="badge bg-warning text-dark ms-1" style="font-size: 0.65rem;">@L["Auth_TutorBadge"]</span>
            }
            else
            {
                <span class="badge bg-info text-dark ms-1" style="font-size: 0.65rem;">@L["Auth_StudentBadge"]</span>
            }
        </button>
        <ul class="dropdown-menu dropdown-menu-end dropdown-menu-dark" 
            aria-labelledby="userProfileDropdown"
            style="background: var(--ck-surface); border: 1px solid var(--ck-border-dim); border-radius: 2px;">
            <li>
                <span class="dropdown-item-text text-secondary small">
                    <i class="bi bi-envelope me-2"></i>@context.User.FindFirst("email")?.Value
                </span>
            </li>
            <li><hr class="dropdown-divider" style="border-color: var(--ck-border-dim);"></li>
            <li>
                <form action="logout" method="post" class="m-0">
                    <AntiforgeryToken />
                    <button type="submit" class="dropdown-item text-danger">
                        <i class="bi bi-box-arrow-right me-2"></i>@L["Auth_Logout"]
                    </button>
                </form>
            </li>
        </ul>
    </div>
</Authorized>
```

### Step 2: Responsive Username
The username is now shown from `d-sm-inline` (≥576px) instead of the previous `d-md-inline` (≥768px), making it visible on more viewport sizes. On extra-small screens, only the person icon is visible — clicking it opens the dropdown to reveal the full name.

### Step 3: Obsidian Foundry Dropdown Styling
Ensure the dropdown uses the dark design tokens:
- Background: `var(--ck-surface)`
- Border: `1px solid var(--ck-border-dim)`
- Border-radius: `2px` (hard edge)
- Text: `var(--dusk-text-primary)` for items, `var(--dusk-text-muted)` for secondary info

---

---

## 🔐 Part 6: Duplicate Login Button Fix

### Problem
When an unauthenticated user visits the site, **two separate Login buttons** appear in the header simultaneously:
1. `NavMenu.razor` renders a `<NotAuthorized>` nav-link login button (line 30-36)
2. `LoginDisplay.razor` renders its own `<NotAuthorized>` login button (line 31-35)

`LoginDisplay` is embedded inside `NavMenu` (line 54-56), so both are visible at the same time. When the user clicks Login, the page briefly flashes this dual-button state before the Keycloak redirect fires.

### Step 1: Remove Login Button from NavMenu
The Nav-level `<NotAuthorized>` login link in `NavMenu.razor` is redundant. `LoginDisplay.razor` is the single source of truth for login/logout/identity. Remove the entire `<NotAuthorized>` block from the first `<AuthorizeView>` in `NavMenu.razor`:

**Delete this block (NavMenu.razor lines 30-36):**
```razor
<NotAuthorized>
    <div class="nav-item me-3">
        <NavLink class="nav-link dusk-nav-link fw-semibold" href="login">
            <i class="bi bi-box-arrow-in-right me-1"></i> @L["Auth_Login"]
        </NavLink>
    </div>
</NotAuthorized>
```

### Step 2: Ensure LoginDisplay Uses `forceLoad`
The remaining login button in `LoginDisplay.razor` uses a plain `<a href="login">` which triggers Blazor's client-side router first, causing a brief render of the page before the server-side `/login` endpoint fires the OIDC Challenge.

Replace the `<a>` tag with a `NavigationManager.NavigateTo` call using `forceLoad: true`, or switch to a `<form>` POST, or simply ensure the anchor has `data-enhance-nav="false"` to bypass Blazor's enhanced navigation:

```razor
<NotAuthorized>
    <div class="d-flex align-items-center me-3">
        <a href="login?returnUrl=@Uri.EscapeDataString(NavigationManager.ToBaseRelativePath(NavigationManager.Uri))" 
           data-enhance-nav="false"
           class="btn btn-sm btn-amber px-3 fw-semibold">
            <i class="bi bi-box-arrow-in-right me-1"></i>@L["Auth_Login"]
        </a>
    </div>
</NotAuthorized>
```

The `data-enhance-nav="false"` attribute ensures Blazor doesn't intercept the navigation — the browser goes directly to the server endpoint which issues the 302 redirect to Keycloak. This eliminates the intermediate page flash entirely.

### Step 3: Style Consistency
Update the button from `btn-primary` to `btn-amber` to match the Obsidian Foundry design language.

---

## ⚡ Part 7: Practice Page — Laser Scanner Syntax Check HUD

### Problem
The current syntax-check success feedback is a subtle green `border-success-blink` animation on the editor container border. On the dark Obsidian Foundry theme this is **barely visible** — users don't notice that the check passed.

### Step 1: Razor — Overlay Inside Editor Container
Replace the `border-success-blink` class toggle with an **inner overlay** rendered conditionally inside the editor container. The overlay uses `pointer-events: none` so it doesn't block the Monaco editor.

Modify `Practice.razor` — replace the editor container block:
```razor
<div class="editor-container overflow-hidden" style="height: 500px; width: 100%; position: relative; border: 1px solid rgba(255,255,255,0.1); background: var(--ck-bg);">
    <StandaloneCodeEditor @ref="_editor" ConstructionOptions="EditorConstructionOptions" CssClass="w-100 h-100 placeholder-glow" />

    @if (showSuccessBlink)
    {
        <div class="laser-scanner-overlay">
            <div class="laser-bolt"></div>
            <div class="hud-success-text">
                <i class="bi bi-shield-check me-2"></i> SYNTAX VERIFIED
            </div>
        </div>
    }
</div>
```

**Key changes:**
- Remove the `rounded` class (Obsidian Foundry = hard edges)
- Remove the `@(showSuccessBlink ? "border-success-blink" : "")` class toggle
- Add the conditional overlay div inside the container

### Step 2: C# — Extend Animation Duration
In the `CheckSyntax()` method, increase the delay from `1000ms` to `1800ms` so the full animation cycle (1.2s bolt + 1.4s HUD reveal) has time to complete:
```csharp
else
{
    showSuccessBlink = true;
    StateHasChanged();
    await Task.Delay(1800);  // Was 1000 — allow laser + HUD animation to finish
    showSuccessBlink = false;
}
```

### Step 3: CSS — Laser Scanner Animations
Add the following to `Practice.razor.css`. Replace the old `blink-green` / `border-success-blink` rules:

```css
/* ── Laser Scanner Overlay ────────────────────────────────── */

/* Full-coverage overlay — non-interactive (pointer-events: none) */
.laser-scanner-overlay {
    position: absolute;
    top: 0; left: 0; right: 0; bottom: 0;
    pointer-events: none;
    z-index: 20;
    box-shadow: inset 0 0 0 1px rgba(16, 185, 129, 0.2);
}

/* The blaster bolt — a short streak of light that runs along the container perimeter */
.laser-bolt {
    position: absolute;
    top: 0; left: 0;
    width: 150px;
    height: 2px;
    background: linear-gradient(90deg, transparent, #10b981, #ffffff);
    box-shadow: 0 0 15px #10b981, 0 0 30px #10b981;
    border-radius: 2px;
    animation: blaster-perimeter 1.2s linear forwards;
}

/* Central HUD text — fades in with a scale punch after the bolt starts */
.hud-success-text {
    position: absolute;
    top: 50%; left: 50%;
    transform: translate(-50%, -50%) scale(0.8);
    background: rgba(16, 185, 129, 0.1);
    color: #10b981;
    border: 1px solid rgba(16, 185, 129, 0.4);
    padding: 1rem 2rem;
    font-family: monospace;
    font-size: 1.5rem;
    font-weight: bold;
    letter-spacing: 4px;
    backdrop-filter: blur(8px);
    border-radius: 2px;  /* Obsidian Foundry: hard edge */
    opacity: 0;
    animation: hud-reveal 1s cubic-bezier(0.175, 0.885, 0.32, 1.275) 0.4s forwards;
}

/* ── Keyframes ────────────────────────────────────────────── */

/* Bolt runs around the perimeter: Top → Right → Bottom → Left */
@keyframes blaster-perimeter {
    0%    { top: 0; left: -150px; width: 150px; height: 2px; }
    25%   { top: 0; left: 100%;   width: 150px; height: 2px; }

    25.1% { top: -150px; left: calc(100% - 2px); width: 2px; height: 150px;
            background: linear-gradient(180deg, transparent, #10b981, #ffffff); }
    50%   { top: 100%;   left: calc(100% - 2px); width: 2px; height: 150px; }

    50.1% { top: calc(100% - 2px); left: 100%; width: 150px; height: 2px;
            background: linear-gradient(-90deg, transparent, #10b981, #ffffff); }
    75%   { top: calc(100% - 2px); left: -150px; width: 150px; height: 2px; }

    75.1% { top: 100%; left: 0; width: 2px; height: 150px;
            background: linear-gradient(0deg, transparent, #10b981, #ffffff); }
    100%  { top: -150px; left: 0; width: 2px; height: 150px; }
}

/* HUD text: scale-punch entrance → hold → fade out */
@keyframes hud-reveal {
    0%   { opacity: 0; transform: translate(-50%, -50%) scale(0.8); }
    30%  { opacity: 1; transform: translate(-50%, -50%) scale(1.05);
           box-shadow: 0 0 40px rgba(16, 185, 129, 0.3); }
    40%  { opacity: 1; transform: translate(-50%, -50%) scale(1); }
    80%  { opacity: 1; transform: translate(-50%, -50%) scale(1); }
    100% { opacity: 0; transform: translate(-50%, -50%) scale(1.1); }
}
```

### Important Notes
- The old `@keyframes blink-green` and `.border-success-blink` rules in `Practice.razor.css` should be **deleted** — they are replaced by this system.
- The editor container uses `overflow: hidden` which clips the bolt at boundaries — this is intentional so the bolt "enters" and "exits" each edge cleanly.
- `pointer-events: none` ensures the overlay never blocks clicks/typing in the Monaco editor.
- `backdrop-filter: blur(8px)` only blurs behind the HUD text box, not the entire editor.

---

## ✅ Verification

1. **Test Deletion:** Removing a persisted test file from the TaskEditor sidebar calls `DELETE /api/tasks/{taskId}/tests/{testId}` and the test is permanently removed from the database.
2. **Style Unification:** All pages (Practice, TasksList, TaskEditor, TutorDashboard) use the Obsidian Foundry panel style — no glassmorphism artifacts remain. Monaco editor on Practice page uses `vs-dark` theme.
3. **Generation Notification:** After AI test generation completes, the spinner stops and the generated tests appear in the sidebar. If SignalR fails to deliver, the 60-second polling fallback triggers a reload.
4. **Role Badges:** Admin sees "Administrator" (red), Tutor sees "Tutor/Oktató" (amber), Student sees "Student/Hallgató" (info blue). Each badge is correct per the logged-in user's highest role.
5. **User Dropdown:** Clicking the user area in the header opens a dropdown showing the username, email, role badge, and logout button. Works correctly on mobile and desktop viewports.
6. **Login Button:** Only ONE login button is visible in the header when unauthenticated. Clicking it redirects directly to Keycloak with NO intermediate page flash.
7. **Syntax Check HUD:** Clicking "Check Syntax" on valid code triggers a green laser bolt running along the editor perimeter, followed by a centered "SYNTAX VERIFIED" HUD overlay. The animation completes in ~1.8s and does not block editor interaction.
