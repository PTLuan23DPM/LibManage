using Microsoft.AspNetCore.Mvc;
using LibWeb.Services;
using Microsoft.Extensions.Configuration; 

public class ChatbotController : Controller
{
    private readonly ChatbotService _chatbotService;
    private readonly DatabaseHelper _dbHelper;
    private readonly IConfiguration _configuration;

    public ChatbotController(ChatbotService chatbotService, DatabaseHelper dbHelper, IConfiguration configuration)
    {
        _chatbotService = chatbotService;
        _dbHelper = dbHelper;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return BadRequest("Message cannot be empty.");
        }

        Dictionary<string, List<Dictionary<string, object>>> allTableData = await _dbHelper.GetAllTableDataAsync();
        string chatbotResponse = await _chatbotService.GetChatbotResponse(userMessage, allTableData);

        if (chatbotResponse?.ToLower().StartsWith("select ") == true)
        {
            var queryResult = await _chatbotService.ExecuteQuery(chatbotResponse);
            return Json(new { userMessage = userMessage, chatbotResponse = "SQL Query Executed:", queryResult = queryResult });
        }

        return Json(new { userMessage = userMessage, chatbotResponse = chatbotResponse });
    }
}