# 🧪 Phase 21: Task Editor "Cockpit" Upgrade — Multi-File Test Engineering & AI Generation

## 🎯 Role & Objective

**Role:** Senior Full-Stack Architect (.NET 10, Blazor Server, EF Core).

**Objective:** Upgrade the Task domain and `Components/Pages/TaskEditor.razor` UI. Move from a single `TestBundleReference` string to a **1-to-N relationship** where one Task has multiple `TaskTest` entities (raw xUnit test files). Introduce an **"AI Test Suite Generator"** that creates categorized xUnit tests based on the task description. Maintain the strict "Obsidian Foundry" design language (hard edges, industrial amber, dark surfaces).

---

## 🗄️ Part 1: Backend Architecture Updates

### Step 1: Domain Entities (`DuskOfCoding.Domain.Entities`)
1. **Modify `TaskDefinition`:** 
   - Remove `TestBundleReference`.
   - Add: `public ICollection<TaskTest> Tests { get; set; } = new List<TaskTest>();`
2. **Create `TaskTest`:**
   ```csharp
   public class TaskTest
   {
       public Guid Id { get; init; } = Guid.NewGuid();
       public Guid TaskDefinitionId { get; set; }
       public string Name { get; set; } = string.Empty; // e.g., "BasicMathTests.cs"
       public string Code { get; set; } = string.Empty; // Raw C# xUnit code
       public TaskDefinition? TaskDefinition { get; set; }
   }
   ```

### Step 2: DTOs & API Contracts
Create `TaskTestDto`:
```csharp
public class TaskTestDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
```

Update Task DTOs (`TaskDto`, `CreateTaskDto`, `UpdateTaskDto`):
- Remove `TestBundleReference`.
- Add: `public List<TaskTestDto> Tests { get; set; } = new();`

### Step 3: EF Core & Services
- Update `ApplicationDbContext` to configure the 1:N relationship with Cascade Delete.
- Update `TaskService` (or equivalent backend repository logic) so that Create/Update operations correctly map and save the list of `TaskTest` entities. (Ensure you handle adding new tests, updating existing ones, and deleting removed ones during an Update operation).

### Step 4: AI Generation Endpoint
Create a new endpoint (e.g., in `TaskController` or `AiToolsController`):
- **Route:** `POST /api/tasks/generate-tests`
- **Request:** `{ "title": "string", "description": "string" }`
- **Logic:** Use `Microsoft.Extensions.AI` (`IChatClient`) to generate the tests.
- **System Prompt for the LLM:**
  > "You are a Senior .NET QA Engineer. Generate rigorous, compilable xUnit test classes for the provided coding task. Split them logically into multiple files. For example, if it's a Calculator task, generate 'BasicMathTests.cs', 'ParsingAndCultureTests.cs', and 'OverflowAndExceptionTests.cs'. Return the response strictly as a JSON array of objects with 'Name' (string, filename) and 'Code' (string, raw C# code). Do not use markdown blocks outside the JSON."
- **Return:** A mapped list of `TaskTestDto`.

### Step 5: WebUI ApiClient
Update `ApiClient.cs` in the Blazor project:
```csharp
public async Task<Result<List<TaskTestDto>>> GenerateTestSuiteAsync(string title, string description)
```

---

## 🎨 Part 2: Frontend UI/UX (The Test Engineering Bay)

Modify `Components/Pages/TaskEditor.razor`.

### Step 1: UI Layout (HTML/CSS)
Replace the old `TestBundleReference` input with a new Cockpit-style section:
- **Header:** `.ck-section-header` with `// 02` and `TEST ENGINEERING`.
- **Action Bar:** A flex container aligning the section title and an amber button: `[ <i class="bi bi-magic"></i> GENERATE TEST SUITE VIA AI ]`. Disable it if Title or Description is empty, or while loading.
- **Master-Detail Layout (Grid):**
  - **Sidebar (`col-md-3`):** A list of test files.
    - Active item gets an amber left-border (`border-left: 2px solid var(--ck-amber)`) and lighter background (`var(--ck-surface)`).
    - Inactive items have transparent borders.
    - Add a `[ + NEW FILE ]` button at the bottom.
    - Each item should have a small trash icon (`bi-trash`) to delete it.
  - **Main View (`col-md-9`):** 
    - If a file is selected, show an input for the file Name (`.ck-input`) at the top, and below it, the Monaco `<StandaloneCodeEditor>` inside an `.editor-container` (`height: 500px, border: 1px solid var(--ck-border-dim)`).
    - If no file is selected, show a muted text: `// NO TEST FILE SELECTED.`

### Step 2: Blazor Logic (`@code`)
- **State:**
  ```csharp
  private TaskTestDto? _selectedTest;
  private StandaloneCodeEditor? _testEditor;
  private bool _isGeneratingTests = false;
  ```

- **Editor Synchronization (CRITICAL):**
  - When the user clicks a different test file in the sidebar (`SelectTest(TaskTestDto test)`), you MUST first save the current editor's content into the previously selected DTO:
    ```csharp
    if (_selectedTest != null && _testEditor != null) { 
        _selectedTest.Code = await _testEditor.GetValue(); 
    }
    ```
  - Then update `_selectedTest = test;` and load the new code into the editor:
    ```csharp
    if (_testEditor != null) { 
        await _testEditor.SetValue(test.Code); 
    }
    ```

- **Save Sync:**
  - In `HandleValidSubmit`, before calling `Api.CreateTaskAsync` or `Api.UpdateTaskAsync`, do a final sync of the active editor:
    ```csharp
    if (_selectedTest != null && _testEditor != null) { 
        _selectedTest.Code = await _testEditor.GetValue(); 
    }
    ```

- **AI Generation Logic:**
  - On click, set `_isGeneratingTests = true`.
  - Call `Api.GenerateTestSuiteAsync()`.
  - On success, replace or append to `_model.Tests` and call `SelectTest(_model.Tests.First())`.

### Step 3: Styling Rules
- Use pure Tailwind CSS grid utilities + the globally defined Obsidian Foundry variables (`--ck-amber`, `--ck-bg`, `--ck-surface`, `--ck-border`).
- No `border-radius` > 4px.
- No purple or cyan. Use only the global tokens.
- Buttons must use `.dusk-btn` or `.dusk-btn-outline` (or similar Cockpit button classes). Inputs must use `.ck-input`.

---

## ✅ Verification
- EF Core migration compiles and updates correctly.
- The UI renders the master-detail view flawlessly.
- Switching between test files in the sidebar retains the edits made in the Monaco editor (state isn't lost).
- The AI generation successfully populates multiple files into the sidebar.
