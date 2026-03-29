namespace DuskOfCoding.Infrastructure.Configuration;

/// <summary>
/// Configuration for the LLM provider. Bind from appsettings.json section "LlmProvider".
/// Includes Hungarian specific prompt requirements.
/// </summary>
public sealed class LlmProviderOptions
{
    public const string SectionName = "LlmProvider";

    public string Provider { get; set; } = "OpenAI";
    public string ModelId { get; set; } = "gpt-4o-mini";

    // OpenAI-specific
    public string? OpenAIApiKey { get; set; }

    // Gemini-specific
    public string? GeminiApiKey { get; set; }

    // Azure OpenAI-specific
    public string? AzureEndpoint { get; set; }
    public string? AzureApiKey { get; set; }

    // Resilience
    public int MaxRetryAttempts { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;

    public string FallbackMessage { get; set; } = "I'm having trouble processing your request right now. Please try again in a moment. / Jelenleg nehézségeim adódtak a kérés feldolgozásával. Kérem, próbálja újra később.";
}
