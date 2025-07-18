using HealthCheckAPI.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AIService _aiService;

    public ChatController()
    {
        // Βάλε εδώ το πραγματικό σου API key (ή καλύτερα από config)
        string openAiApiKey = "your_openai_api_key_here";
        _aiService = new AIService(openAiApiKey);
    }

    [HttpGet("ask")]
    public async Task<IActionResult> Ask(string question)
    {
        var answer = await _aiService.AskChatbot(question);
        return Ok(new { answer });
    }
}
