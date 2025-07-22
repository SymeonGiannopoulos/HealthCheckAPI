using Microsoft.AspNetCore.Mvc;
using HealthCheckAPI.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HealthCheckAPI.Controllers
{
    //[Authorize]
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
    }
}

﻿


