using System.Text;
using System.Text.Json;

namespace QuizBattle.Services
{
    public class AIService
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // your groq api key
        private const string ApiKey = "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";

        // groq chat completions endpoint
        private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

        public async Task<string> GenerateQuestionsAsync(string material, int maxQuestions)
        {
            string prompt = $@"You are a quiz question generator. Read the learning material below and create exactly {maxQuestions} questions based on it.

FORMAT RULES:
1. Each line must be a single question.
2. For Identification questions, use this format:
   Identification|Question text|Correct Answer
3. For Multiple Choice questions, use this format:
   MultipleChoice|Question text|Option1|Option2|Option3|Option4|ANS|CorrectAnswer
4. Mix Identification and MultipleChoice questions.
5. Output ONLY raw text lines separated by newlines. Do not output markdown, code blocks, bullet points, or extra text.

Learning Material:
{material}";

            var requestBody = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.5
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("Authorization", $"Bearer {ApiKey}");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API Error ({response.StatusCode}): {responseBody}");
            }

            using JsonDocument doc = JsonDocument.Parse(responseBody);

            string generatedContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            // clean markdown tags if present
            generatedContent = generatedContent
                .Replace("```text", "")
                .Replace("```", "")
                .Trim();

            return generatedContent;
        }
    }
}