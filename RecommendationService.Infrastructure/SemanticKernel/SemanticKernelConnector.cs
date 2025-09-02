using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;

namespace RecommendationService.Infrastructure.SemanticKernel
{
    public class SemanticKernelConnector
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;

        public SemanticKernelConnector(string apiKey)
        {
            // Build the kernel directly
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-3.5-turbo", apiKey);
            _kernel = builder.Build();

            // Get the chat completion service
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> GetRecommendationsAsync(string prompt)
        {
            // Use GetChatMessageContentsAsync with ChatHistory
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var result = await _chatCompletionService.GetChatMessageContentsAsync(chatHistory);
            return result[0].Content;
        }
    }
}