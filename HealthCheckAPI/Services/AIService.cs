using OpenAI;
using OpenAI.Chat;

namespace HealthCheckAPI.Services
{
    public class AIService
    {
        private readonly OpenAIClient _client;

        public AIService(string apiKey)
        {
            _client = new OpenAIClient(apiKey);
        }

        public async Task<string> AskChatbot(string question)
        {
            var chatMessages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, question)
            };

            var chatRequest = new ChatCompletionCreateRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = chatMessages,
                Temperature = 0.7f,
                MaxTokens = 150
            };

            var response = await _client.ChatCompletion.CreateAsync(chatRequest);

            return response.Choices[0].Message.Content;
        }
    }
}
