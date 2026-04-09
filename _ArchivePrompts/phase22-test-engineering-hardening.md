# 🛠️ Phase 22: Test Engineering Hardening — Async Pipeline & Editor Stability

## 🎯 Role & Objective

**Role:** Senior Full-Stack Architect (.NET 10, Blazor Server, RabbitMQ, SignalR).

**Objective:** Upgrade the existing multi-file `TaskEditor.razor` implementation. The current setup needs to migrate the AI Test Suite Generation to an **Event-Driven (Fire-and-Forget)** model to prevent HTTP timeouts. You must also harden the Monaco Editor state management to prevent race conditions during rapid file switching, implement Save-Guards, and add localized real-time **Toast notifications** via SignalR.

*(Note: The 1-to-N database relationship, `TaskTest` entities, and basic UI layout are already implemented. Do not modify domain entities or DbContext).*

---

## ⚙️ Part 1: Asynchronous AI Pipeline (Backend & Worker)

### Step 1: Messaging Contract
Create/update the message record in your shared contracts. **Include UserId** for targeted notifications:
```csharp
public record GenerateTestSuiteCommand(Guid TaskId, string Title, string Description, string UserId);
```

### Step 2: API Refactoring (Fire & Forget)
Refactor the `POST /api/tasks/{id}/generate-tests` endpoint:
- **Logic:** Validate the task exists. Extract `UserId` from the authenticated user context. Publish `GenerateTestSuiteCommand` to RabbitMQ/MassTransit.
- **Return:** `HTTP 202 Accepted` immediately. Do NOT await the LLM here.

### Step 3: Background Worker Processing
In your background service (e.g., `TutorWorker`):
- Consume `GenerateTestSuiteCommand`.
- **LLM Call:** Use `Microsoft.Extensions.AI`. Return ONLY a JSON array of objects with 'Name' and 'Code'.
- **Robust Parsing (CRITICAL):** Sanitize the string before deserialization. Strip markdown blocks (` ```json ` and ` ``` `) as LLMs frequently ignore formatting instructions.
- **Database Save:** Deserialize into `TaskTest` entities, attach to `TaskId`, and save to EF Core.
- **SignalR Notification (Targeted):** Broadcast success or failure back to the specific user:
  ```csharp
  // On Success:
  await _hubContext.Clients.User(command.UserId).SendAsync("TestSuiteGenerationCompleted", command.TaskId, true, "Test_Suite_Success_Key");
  // On Error:
  await _hubContext.Clients.User(command.UserId).SendAsync("TestSuiteGenerationCompleted", command.TaskId, false, "Test_Suite_Error_Key");
  ```

---

## 🎨 Part 2: Frontend Hardening (`Components/Pages/TaskEditor.razor`)

### Step 1: SignalR Integration & Localized Toasts
- Inject `HubConnection`, your Toast service, and `IStringLocalizer<SharedResource> L`. Implement `IAsyncDisposable`.
- In `OnInitializedAsync`, subscribe:
  ```csharp
  _hubConnection.On<Guid, bool, string>("TestSuiteGenerationCompleted", async (taskId, isSuccess, messageKey) => {
      if (isSuccess) ToastService.ShowSuccess(L[messageKey].Value);
      else ToastService.ShowError(L[messageKey].Value);

      if (IsEditMode && Id == taskId) {
          await ReloadTaskData(); // Refresh tests from DB
          _isGeneratingTests = false;
          await InvokeAsync(StateHasChanged);
      }
  });
  ```

### Step 2: Editor State & Lifecycle (CRITICAL)
Prevent race conditions during sidebar file switching:
1. **State Flags:** Add `private bool _isEditorReady;`.
2. **OnEditorInit Event:** Hook into BlazorMonaco's init event. Set `_isEditorReady = true` ONLY when the editor fires its "Ready" event, then populate it with `_selectedTest.Code`.
3. **Safe Switching Logic:**
   ```csharp
   private async Task SelectTest(TaskTestDto test) {
       // Save current
       if (_selectedTest != null && _testEditor != null && _isEditorReady) {
           _selectedTest.Code = await _testEditor.GetValue();
       }
       // Switch
       _selectedTest = test;
       // Load new
       if (_testEditor != null && _isEditorReady) {
           await _testEditor.SetValue(test.Code);
       }
   }
   ```
4. **Before Save Sync:** In `HandleValidSubmit`, explicitly extract the editor value into `_selectedTest.Code` before sending the payload.

### Step 3: Non-Blocking AI UI Interaction & Save Guard
The background worker requires a valid `TaskId`. You must handle the Create/Edit state safely using localized texts.
- When the `[ GENERATE ]` button is clicked:
  1. **If `!IsEditMode` (New Task):** Prompt via JSInterop: 
     `bool confirmed = await JS.InvokeAsync<bool>("confirm", L["TaskEditor_SaveBeforeGeneratePrompt"].Value);`
     - If `!confirmed`, abort.
     - If `confirmed`, explicitly await `HandleValidSubmit()` to save the task and assign an `Id`.
  2. **If `IsEditMode` (Existing Task):** Await `HandleValidSubmit()` to save any pending UI changes.
  3. **Trigger Generation:** Set `_isGeneratingTests = true`, call the 202 API endpoint with the valid `Id`.
  4. Show Toast: `ToastService.ShowInfo(L["TaskEditor_GenerationStarted"].Value);`
  5. Keep the button's loading spinner active until the SignalR event fires.

## ✅ Verification
1. API immediately returns 202; UI is not blocked during LLM generation.
2. Background Worker successfully saves 1:N entities and notifies the specific user via SignalR.
3. Rapidly clicking between files correctly preserves edited code in Monaco.
4. New tasks strictly enforce a save (acquiring an ID) before generating AI tests.