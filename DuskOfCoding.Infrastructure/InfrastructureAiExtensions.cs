using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAI;
using DuskOfCoding.Infrastructure.Configuration;

namespace DuskOfCoding.Infrastructure;

public static class InfrastructureAiExtensions
{
    public static IServiceCollection AddConfigurableKernel(this IServiceCollection services)
    {
        services.AddTransient<Microsoft.SemanticKernel.Kernel>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LlmProviderOptions>>().Value;
            var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();

            switch (options.Provider.ToLowerInvariant())
            {
                case "azureopenai":
                    if (!string.IsNullOrWhiteSpace(options.AzureApiKey))
                    {
                        builder.AddAzureOpenAIChatCompletion(options.ModelId, options.AzureEndpoint!, options.AzureApiKey);
                    }
                    else
                    {
                        builder.AddAzureOpenAIChatCompletion(options.ModelId, options.AzureEndpoint!, new Azure.Identity.DefaultAzureCredential());
                    }
                    break;

                case "openai":
                    builder.AddOpenAIChatCompletion(options.ModelId, options.OpenAIApiKey ?? options.ApiKey ?? "dummy-key");
                    break;

                case "gemini":
                    var geminiClient = new HttpClient { BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/") };
                    builder.AddOpenAIChatCompletion(options.ModelId, options.GeminiApiKey ?? "dummy-key", httpClient: geminiClient);
                    break;

                case "local":
                case "custom":
                    var endpoint = options.CustomEndpoint ?? "http://localhost:11434/v1";
                    var endpointUri = endpoint.EndsWith("/") ? endpoint : endpoint + "/";
                    var customClient = new HttpClient { BaseAddress = new Uri(endpointUri) };
                    builder.AddOpenAIChatCompletion(options.ModelId, options.ApiKey ?? "local-key", httpClient: customClient);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown LLM provider '{options.Provider}'. Supported: 'OpenAI', 'AzureOpenAI', 'Gemini', 'Local', 'Custom'.");
            }

            return builder.Build();
        });

        return services;
    }

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
                "gemini" => CreateGeminiClient(options),
                "local" or "custom" => CreateOpenAIClient(options),
                _ => throw new InvalidOperationException(
                    $"Unknown LLM provider '{options.Provider}'. Supported: 'OpenAI', 'AzureOpenAI', 'Gemini', 'Local', 'Custom'.")
            };

            return new ChatClientBuilder(innerClient)
                .UseLogging(logger)
                .Build();
        });

        return services;
    }

    private static IChatClient CreateOpenAIClient(LlmProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OpenAIApiKey))
            throw new InvalidOperationException($"OpenAI API key is required when provider is set to '{options.Provider}'.");

        // Use the official OpenAI library explicitly
        var client = new global::OpenAI.OpenAIClient(new ApiKeyCredential(options.OpenAIApiKey));
        return client.GetChatClient(options.ModelId).AsIChatClient();
    }

    private static IChatClient CreateGeminiClient(LlmProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.GeminiApiKey))
            throw new InvalidOperationException($"Gemini API key is required when provider is set to '{options.Provider}'.");

        // Gemini's OpenAI-compatible endpoint. 
        // Note: Some clients are sensitive to the trailing slash or the way the model is appended.
        var clientOptions = new global::OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/")
        };
        
        var client = new global::OpenAI.OpenAIClient(new ApiKeyCredential(options.GeminiApiKey), clientOptions);
        
        // Return the client directly using the extension method
        return client.GetChatClient(options.ModelId).AsIChatClient();
    }

    private static IChatClient CreateAzureOpenAIClient(LlmProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AzureEndpoint))
            throw new InvalidOperationException($"Azure OpenAI endpoint is required when provider is set to '{options.Provider}'.");

        // Use Azure.AI.OpenAI explicitly
        global::Azure.AI.OpenAI.AzureOpenAIClient azureClient;
        if (!string.IsNullOrWhiteSpace(options.AzureApiKey))
        {
            azureClient = new global::Azure.AI.OpenAI.AzureOpenAIClient(new Uri(options.AzureEndpoint), new ApiKeyCredential(options.AzureApiKey));
        }
        else
        {
            azureClient = new global::Azure.AI.OpenAI.AzureOpenAIClient(new Uri(options.AzureEndpoint), new global::Azure.Identity.DefaultAzureCredential());
        }

        return azureClient.GetChatClient(options.ModelId).AsIChatClient();
    }
}
