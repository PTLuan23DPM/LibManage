using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibWeb.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public class ChatbotService
{
    private readonly AppDbContext _dbContext;
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

    public async Task<string> GetChatbotResponse(string userMessage, Dictionary<string, List<Dictionary<string, object>>> allTableData)
    {
        string prompt = "You are a helpful chatbot with access to the following database tables and their data:\n\n";

        foreach (var tableEntry in allTableData)
        {
            string tableName = tableEntry.Key;
            List<Dictionary<string, object>> rows = tableEntry.Value;

            prompt += $"Table: {tableName}\n";
            if (rows.Any())
            {
                prompt += $"Columns: {string.Join(", ", rows.First().Keys)}\n";
                prompt += "Data:\n";
                foreach (var row in rows)
                {
                    prompt += JsonSerializer.Serialize(row) + "\n"; // Serialize each row as JSON
                }
            }
            else
            {
                prompt += "No data in this table.\n";
            }
            prompt += "\n";
        }

        prompt += $"Based on this information, answer the following question: '{userMessage}'\n\n";
        prompt += "If the question requires analyzing the provided data, do so directly. If the question is about the database schema, answer directly.\n\n";
        prompt += "Provide a concise and informative answer based on the data provided.\n";

        var requestBody = new
        {
            model = _openRouterModel,
            messages = new[]
            {
                new { role = "user", content = prompt }
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
    public async Task<List<Dictionary<string, object>>> ExecuteQuery(string sqlQuery)
    {
        try
        {
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    var reader = await command.ExecuteReaderAsync();
                    var results = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        results.Add(row);
                    }
                    return results;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database Error: {ex.Message}");
            return null;
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