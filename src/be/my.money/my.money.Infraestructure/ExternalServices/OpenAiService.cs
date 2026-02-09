using System.Text;
using System.Text.Json;
using my.money.application.Ports.ExternalServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace my.money.Infraestructure.ExternalServices;

public sealed class OpenAiSettings
{
    public const string SectionName = "OpenAi";
    public string ApiKey { get; set; } = default!;
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

public sealed class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(HttpClient httpClient, IOptions<OpenAiSettings> settings, ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AnalyzeNewsResponse> AnalyzeNewsAsync(
        string articleText,
        IEnumerable<AssetCandidateDto> candidates,
        CancellationToken ct)
    {
        if (!candidates.Any())
            return new AnalyzeNewsResponse();

        var candidatesList = candidates.ToList();
        var candidatesJson = JsonSerializer.Serialize(candidatesList.Select(c => new { c.Ticker, c.Name }));

        var systemPrompt = @"You are an expert financial analyst. Analyze news articles to identify mentions of specific financial assets.
Return ONLY a JSON response with the exact structure specified, no other text.";

        var userPrompt = $@"Analyze this news article and determine if any of the following assets are mentioned economically (i.e., affected by the news):

Assets to check:
{candidatesJson}

Article:
{articleText}

For each asset that is relevant to the article, return a JSON response with this exact structure:
{{
  ""mentions"": [
    {{
      ""ticker"": ""string (matching the asset ticker from the list)"",
      ""confidence"": number (0.0 to 1.0, where 1.0 means definitely relevant),
      ""explanation"": ""string (brief explanation of how the asset is impacted)"",
      ""matched_text"": ""string (optional: the specific text that triggered the mention)""
    }}
  ]
}}

Return ONLY the JSON, no additional text.
If no assets are relevant, return: {{ ""mentions"": [] }}";

        var requestBody = new
        {
            model = _settings.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            max_tokens = 1000
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("Authorization", $"Bearer {_settings.ApiKey}");

        try
        {
            _logger.LogInformation("Calling OpenAI API for news analysis with {CandidateCount} candidates", candidatesList.Count);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            // Step 1: Deserialize OpenAI wrapper
            var openAiResponse = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(responseContent, options);
            if (openAiResponse == null || openAiResponse.Choices == null || openAiResponse.Choices.Count == 0)
            {
                _logger.LogWarning("No choices in OpenAI response");
                return new AnalyzeNewsResponse();
            }

            var message = openAiResponse.Choices[0].Message;
            if (message == null || string.IsNullOrWhiteSpace(message.Content))
            {
                _logger.LogWarning("Empty message content from OpenAI");
                return new AnalyzeNewsResponse();
            }

            // Step 2: Extract and clean JSON from content
            var jsonMatch = ExtractJson(message.Content);
            if (string.IsNullOrWhiteSpace(jsonMatch))
            {
                _logger.LogWarning("Could not extract JSON from OpenAI response: {Response}", message.Content);
                return new AnalyzeNewsResponse();
            }

            // Step 3: Deserialize to domain model (AssetMentionsResponse)
            var assetMentions = JsonSerializer.Deserialize<AssetMentionsResponse>(jsonMatch, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            if (assetMentions == null)
            {
                _logger.LogWarning("Failed to deserialize asset mentions from OpenAI content");
                return new AnalyzeNewsResponse();
            }

            // Map AssetMentionsResponse to AnalyzeNewsResponse
            var result = new AnalyzeNewsResponse();
            if (assetMentions.Mentions != null)
            {
                foreach (var mention in assetMentions.Mentions)
                {
                    result.Mentions.Add(new MentionResult
                    {
                        Ticker = mention.Ticker,
                        Confidence = mention.Confidence,
                        Explanation = mention.Explanation,
                        MatchedText = mention.MatchedText
                    });
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            throw;
        }
    }

    private static string ExtractJson(string text)
    {
        // Remove markdown code blocks if present
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return text.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        return text;
    }

    // DTOs for OpenAI Chat Completion
    private sealed class OpenAiChatCompletionResponse
    {
        public List<Choice> Choices { get; set; } = new();
        public sealed class Choice
        {
            public Message Message { get; set; } = null!;
        }
        public sealed class Message
        {
            public string Role { get; set; } = null!;
            public string Content { get; set; } = null!;
        }
    }

    // DTO for your domain response
    public sealed class AssetMentionsResponse
    {
        public List<Mention> Mentions { get; set; } = new();
        public sealed class Mention
        {
            public string Ticker { get; set; } = null!;
            public decimal Confidence { get; set; }
            public string Explanation { get; set; } = null!;
            public string? MatchedText { get; set; }
        }
    }
}
