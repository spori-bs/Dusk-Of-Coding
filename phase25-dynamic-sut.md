# 🛠️ Phase 25: Dynamic SUT (System Under Test) Configuration

**Role:** Senior .NET Backend Engineer
**Task:** Implement a new property `ExpectedClassName` across the stack to allow dynamic class name targeting for AI test generation.

**Execution Steps:**
1. **Domain & Database:** Add `public string ExpectedClassName { get; set; } = "Solution";` to the `TaskDefinition` (or `Task`) domain entity. Create an EF Core migration (`dotnet ef migrations add AddExpectedClassName`).
2. **Application (DTOs):** Add `ExpectedClassName` to `CreateTaskDto`, `UpdateTaskDto`, and `TaskDto`. Ensure it defaults to `"Solution"`.
3. **Messaging:** Add `ExpectedClassName` to the `GenerateTestSuiteCommand` record so the Worker receives it.
4. **WebUI (TaskEditor.razor):** In the edit form, add a new text input for `ExpectedClassName` right below the Tags input. Use the Obsidian Foundry design (e.g., `ck-input`). Give it a label "Expected Class Name (SUT)" and a placeholder "e.g. Calculator, default: Solution".
5. **Worker (TutorWorker):** Update the AI prompt string to dynamically inject `command.ExpectedClassName` into the LLM system prompt, instructing it to test that specific class instead of a hardcoded "Solution".

Output only the modified code blocks.