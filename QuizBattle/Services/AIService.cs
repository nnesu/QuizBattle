using System.Text;
using System.Text.Json;

namespace QuizBattle.Services;

public class AIService
{
    private static readonly HttpClient httpClient = new HttpClient();

    private const string ApiKey = "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";
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    public async Task<string> GenerateQuestionsAsync(string material, int maxQuestions)
    {
        string prompt = $@"You are a strict, factual quiz question generator. Your sole job is to create quiz questions derived EXCLUSIVELY from the provided Learning Material.

STRICT ACCURACY & ANTI-HALLUCINATION RULES:
1. EXCLUSIVE SOURCE GROUNDING: Use ONLY facts explicitly stated in the Learning Material. Do NOT use outside knowledge, prior training data, or assumptions.
2. NO INVENTED FACTS: If a statement or detail is not directly written in the text, DO NOT create a question about it.
3. QUANTITY FLEXIBILITY: Generate UP TO {maxQuestions} questions. If the material does not contain enough distinct facts, generate fewer questions rather than fabricating information.

FORMAT RULES:
1. Each line must be a single question.
2. For Identification questions, use this exact format:
   Identification|Question text|Correct Answer
3. For Multiple Choice questions, use this exact format:
   MultipleChoice|Question text|Option1|Option2|Option3|Option4|ANS|CorrectAnswer
   * CRITICAL: The ""CorrectAnswer"" string MUST be an EXACT word-for-word match to one of Option1, Option2, Option3, or Option4.
4. Mix Identification and MultipleChoice question types evenly.
5. OUTPUT ONLY raw text lines separated by newlines. Do NOT output markdown code blocks (```), bullet points, introductory headers, or explanations.

Learning Material:
{material}";

        var requestBody = new
        {
            model = "llama-3.1-8b-instant",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.0
        };

        string jsonContent = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("Authorization", $"Bearer {ApiKey}");
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new Exception("API Key is invalid or was revoked. Please paste a fresh key into AIService.cs.");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Exception("Groq Token Rate Limit Exceeded (6,000 TPM limit). Please wait 45-60 seconds before generating again.");
            }
            throw new Exception($"API Error ({response.StatusCode}): {responseBody}");
        }

        using JsonDocument doc = JsonDocument.Parse(responseBody);
        string content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return content
            .Replace("```text", "")
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();
    }
}