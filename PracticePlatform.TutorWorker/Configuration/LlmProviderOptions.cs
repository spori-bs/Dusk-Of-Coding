namespace PracticePlatform.TutorWorker.Configuration;

/// <summary>
/// Configuration for the LLM provider. Bind from appsettings.json section "LlmProvider".
/// Switch between "OpenAI" and "AzureOpenAI" via the Provider property.
/// </summary>
public sealed class LlmProviderOptions
{
    public const string SectionName = "LlmProvider";

    /// <summary>
    /// "OpenAI" or "AzureOpenAI"
    /// </summary>
    public string Provider { get; set; } = "OpenAI";

    /// <summary>
    /// The model/deployment name to use (e.g. "gpt-4o", "gpt-4o-mini").
    /// For Azure OpenAI, this is the deployment name.
    /// </summary>
    public string ModelId { get; set; } = "gpt-4o-mini";

    // ── OpenAI-specific ────────────────────────────────
    /// <summary>API key for direct OpenAI access.</summary>
    public string? OpenAIApiKey { get; set; }

    // ── Azure OpenAI-specific ──────────────────────────
    /// <summary>Azure OpenAI resource endpoint (e.g. https://myresource.openai.azure.com/).</summary>
    public string? AzureEndpoint { get; set; }

    /// <summary>API key for Azure OpenAI. If empty, DefaultAzureCredential will be used.</summary>
    public string? AzureApiKey { get; set; }

    // ── Resilience ─────────────────────────────────────
    /// <summary>Max retry attempts for transient LLM failures (HTTP 5xx, 429).</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Overall timeout in seconds for the LLM "thought" process.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Fallback message sent when all retries are exhausted.</summary>
    public string FallbackMessage { get; set; } = "I'm having trouble processing your request right now. Please try again in a moment. In the meantime, here are the raw diagnostics from your code analysis.";
}
