<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Mvc;
using HealthCheckAPI.Services;
using System.Threading.Tasks;

namespace HealthCheckAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatQueryService _chatQueryService;

        public ChatController(ChatQueryService chatQueryService)
        {
            _chatQueryService = chatQueryService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskQuestion([FromBody] QuestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Question))
            {
                return BadRequest("Το πεδίο 'Question' είναι υποχρεωτικό.");
            }

            var answer = await _chatQueryService.GetAnswerAsync(request.Question);
            return Ok(new { Answer = answer });
        }
    }

    public class QuestionRequest
    {
        public string Question { get; set; }
=======
﻿using HealthCheckAPI.Services;
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
>>>>>>> 1f513b0 (Done)
    }
}
