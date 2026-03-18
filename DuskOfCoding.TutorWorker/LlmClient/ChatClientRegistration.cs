using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using DuskOfCoding.TutorWorker.Configuration;

namespace DuskOfCoding.TutorWorker.LlmClient;

/// <summary>
/// Registers the correct IChatClient implementation based on configuration.
/// Supports switching between OpenAI and Azure OpenAI via appsettings.json.
/// </summary>
public static class ChatClientRegistration
{
    public static IServiceCollection AddConfigurableChatClient(this IServiceCollection services)
    {
        services.AddSingleton<IChatClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LlmProviderOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>();

            IChatClient innerClient = options.Provider.ToLowerInvariant() switch
            {
                "azureopenai" => CreateAzureOpenAIClient(options),
                "openai" => CreateOpenAIClient(options),
                _ => throw new InvalidOperationException(
                    $"Unknown LLM provider '{options.Provider}'. Supported: 'OpenAI', 'AzureOpenAI'.")
            };

            // Wrap with function-calling support for MCP tools
            // and logging for observability
            return new ChatClientBuilder(innerClient)
                .UseFunctionInvocation()
                .UseLogging(logger)
                .Build();
        });

        return services;
    }

    private static IChatClient CreateOpenAIClient(LlmProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OpenAIApiKey))
            throw new InvalidOperationException(
                "OpenAI API key is required. Set LlmProvider:OpenAIApiKey in configuration.");

        var client = new OpenAIClient(new ApiKeyCredential(options.OpenAIApiKey));
        return client.GetChatClient(options.ModelId).AsIChatClient();
    }

    private static IChatClient CreateAzureOpenAIClient(LlmProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AzureEndpoint))
            throw new InvalidOperationException(
                "Azure OpenAI endpoint is required. Set LlmProvider:AzureEndpoint in configuration.");

        AzureOpenAIClient azureClient;

        if (!string.IsNullOrWhiteSpace(options.AzureApiKey))
        {
            // API key auth
            azureClient = new AzureOpenAIClient(
                new Uri(options.AzureEndpoint),
                new ApiKeyCredential(options.AzureApiKey));
        }
        else
        {
            // DefaultAzureCredential (Managed Identity, etc.)
            azureClient = new AzureOpenAIClient(
                new Uri(options.AzureEndpoint),
                new Azure.Identity.DefaultAzureCredential());
        }

        return azureClient.GetChatClient(options.ModelId).AsIChatClient();
    }
}
