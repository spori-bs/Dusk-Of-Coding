namespace DuskOfCoding.Application.DTOs;

public record CreateTaskDto(
    string Title,
    string Description,
    string DifficultyLevel,
    List<string> Tags,
    string TestBundleReference
);

public record UpdateTaskDto(
    string Title,
    string Description,
    string DifficultyLevel,
    List<string> Tags,
    string TestBundleReference
);

public record SubmitCodeDto(
    Guid TaskId,
    string SourceCode,
    Guid? UserId = null
);
