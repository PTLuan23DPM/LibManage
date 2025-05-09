using Microsoft.AspNetCore.Mvc;
using LibWeb.Services; 

public class ChatbotController : Controller
{
    private readonly ChatbotService _chatbotService;

    public ChatbotController(ChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
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

        string chatbotResponse = await _chatbotService.GetChatbotResponse(userMessage);

        return Json(new { userMessage = userMessage, chatbotResponse = chatbotResponse });
    }
}