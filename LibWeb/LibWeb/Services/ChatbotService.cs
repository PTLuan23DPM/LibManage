using System.Net.Http.Headers;
using System.Net.Http.Json;

public class ChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly string _openRouterApiKey;
    private readonly string _openRouterBaseUrl;
    private readonly string _openRouterModel;

    public ChatbotService(IConfiguration configuration, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _openRouterApiKey = configuration["OpenRouter:ApiKey"];
        _openRouterBaseUrl = configuration["OpenRouter:BaseUrl"];
        _openRouterModel = configuration["OpenRouter:Model"];

        _httpClient.BaseAddress = new Uri(_openRouterBaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openRouterApiKey);
    }

    public async Task<string> GetChatbotResponse(string userMessage)
    {
        var requestBody = new
        {
            model = _openRouterModel,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("", requestBody);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadFromJsonAsync<OpenRouterResponse>();
            return responseContent?.choices?.FirstOrDefault()?.message?.content;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"OpenRouter API Error: {ex.Message}");
            return "Error communicating with the chatbot.";
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"JSON Deserialization Error: {ex.Message}");
            return "Error processing chatbot response.";
        }
    }
}

public class OpenRouterResponse
{
    public List<Choice> choices { get; set; }
}

public class Choice
{
    public Message message { get; set; }
}

public class Message
{
    public string content { get; set; }
}