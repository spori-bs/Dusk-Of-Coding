# Phase 26: Practice UX & Test Generation Bug Fixes

## Objectives
Resolve three specific functional and UX defects reported during the Phase 25 testing session. 

---

### Bug 1: Test Generation Button "Stuck" (SignalR Disconnect)
**Symptom:** 
When triggering AI Test Generation from the Task Editor, the task successfully completes in the background (as seen via the Aspire Telemetry Trace running ~20s), but the Blazor UI generate button remains in a spinning/stuck state. The user has to manually refresh (F5) to see the generated tests.
**Analysis:**
- The trace shows the `tutorworker` successfully processing the RabbitMQ message and the `webapi` successfully receiving the `TestGenerationResultMessage`.
- The `TestGenerationBridge` attempts to target `_hubContext.Clients.User(message.UserId)`. 
- The disconnect is likely between the `UserId` saved in the RabbitMQ payload during the HTTP POST vs the `UserId` resolved by `KeycloakUserIdProvider` in the SignalR pipeline. 
    - HTTP POST: `httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
    - SignalR Provider: `connection.User?.FindFirstValue(ClaimTypes.NameIdentifier)`
- Alternatively, `TaskEditor.razor`'s HubConnection might be missing the appropriate mapping or there's an exception inside `ReloadTaskData()` that silently fails the UI update thread. 
**Action Plan:**
- Standardize the `UserId` claim extraction mechanism across `Program.cs` and `KeycloakUserIdProvider` (e.g., explicitly rely on `"sub"` since Keycloak maps its UUID there, or unify OIDC mappings).
- Add robust `ILogger` traces in `TestGenerationBridge` and Blazor UI Console to pinpoint where the `TestSuiteGenerationCompleted` payload vanishes.

---

### Bug 2: Truncated Task Instructions on Practice Page
**Symptom:** 
In `Practice.razor`, clicking a task from the grid opens the editor view, but the full problem description/instructions are nowhere to be found. The user only sees a truncated `...` summary in the task card.
**Analysis:**
- The `task-card` loop restricts the description with `<p class="... text-truncate">@task.Description</p>`.
- In the `selectedTask != null` UI block, there is currently NO code rendering `selectedTask.Description`.
**Action Plan:**
- Introduce a new documentation block inside the selected task view (below the title `Practice_SubmitSolution`).
- Render `selectedTask.Description` cleanly with whitespace preservation or markdown (depending on how descriptions are stored), ensuring students have a reference manual while coding.

---

### Bug 3: Missing Dynamic SUT Template in Practice Editor
**Symptom:** 
After establishing the `ExpectedClassName` (SUT) dynamically in Phase 25, the `Practice.razor` code editor continues to default to `public class Solution`, or initializes blank. This confuses students who receive immediate build errors complaining about missing target classes.
**Analysis:**
- In `Practice.razor`, the Monaco editor is instantiated with hardcoded `EditorConstructionOptions` returning `"public class Solution ... "`.
- When switching tasks via `SelectTask(TaskDto task)`, the code explicitly clears the editor using `_editor.SetValue("")`.
**Action Plan:**
- Modify `SelectTask(TaskDto task)` to dynamically read `task.ExpectedClassName`.
- Generate a C# skeleton template using the actual class name:
  ```csharp
  var sut = string.IsNullOrWhiteSpace(task.ExpectedClassName) ? "Solution" : task.ExpectedClassName;
  var template = $"public class {sut}\n{{\n    // Write your code here\n}}";
  await _editor.SetValue(template);
  ```
- Ensure the editor's initial load behaves dynamically as well.
