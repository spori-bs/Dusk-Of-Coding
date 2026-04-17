namespace DuskOfCoding.Application.DTOs;

public record CreateTaskDto(
    string Title,
    string Description,
    string DifficultyLevel,
    List<string> Tags,
    List<TaskTestDto> Tests,
    string ExpectedClassName = "Solution"
);

public record UpdateTaskDto(
    string Title,
    string Description,
    string DifficultyLevel,
    List<string> Tags,
    List<TaskTestDto> Tests,
    string ExpectedClassName = "Solution"
);

public record TaskTestDto(
    Guid? Id,
    string Name,
    string Code
);

public record SubmitCodeDto(
    Guid TaskId,
    string SourceCode,
    Guid? UserId = null,
    string? PreferredLanguage = null
);
