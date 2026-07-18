using System.Net.Http.Json;
using System.Text.Json;
using QuizBattle.Models;

namespace QuizBattle.Services;

public class FirestoreService
{
    private const string ProjectId = "quizbattle-bd5e7";

    public async Task CreateUserProfile(User user)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{user.LocalId}";

        var body = new
        {
            fields = new
            {
                email = new { stringValue = user.Email },
                displayName = new { stringValue = "No Name" },
                photoUrl = new { stringValue = string.Empty }
            }
        };

        HttpResponseMessage response = await client.PatchAsJsonAsync(url, body);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }
    }

    public async Task<User> GetUserProfile(string localId)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}";

        HttpResponseMessage response = await client.GetAsync(url);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }

        JsonDocument document = JsonDocument.Parse(json);
        JsonElement fields = document.RootElement.GetProperty("fields");

        return new User
        {
            Email = fields.GetProperty("email").GetProperty("stringValue").GetString() ?? string.Empty,
            DisplayName = fields.GetProperty("displayName").GetProperty("stringValue").GetString() ?? "No Name",
            PhotoUrl = fields.TryGetProperty("photoUrl", out JsonElement photo)
                ? photo.GetProperty("stringValue").GetString() ?? string.Empty
                : string.Empty,
            LocalId = localId
        };
    }

    public async Task UpdateDisplayName(string localId, string displayName)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}?updateMask.fieldPaths=displayName";

        var body = new
        {
            fields = new
            {
                displayName = new { stringValue = displayName }
            }
        };

        HttpResponseMessage response = await client.PatchAsJsonAsync(url, body);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }
    }

    public async Task UpdatePhotoUrl(string localId, string photoUrl)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}?updateMask.fieldPaths=photoUrl";

        var body = new
        {
            fields = new
            {
                photoUrl = new { stringValue = photoUrl }
            }
        };

        HttpResponseMessage response = await client.PatchAsJsonAsync(url, body);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }
    }

    public async Task DeleteUserProfile(string localId)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}";

        HttpResponseMessage response = await client.DeleteAsync(url);
        string json = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"DeleteAccount Response: {json}");
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }
    }

    public async Task SubmitLeaderboardScore(string localId, string displayName, string deckName, int score)
    {
        using HttpClient client = new HttpClient();

        string safeDeckName = Uri.EscapeDataString(deckName);
        string docId = $"{safeDeckName}_{localId}";
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/leaderboards/{docId}";

        var body = new
        {
            fields = new
            {
                localId = new { stringValue = localId },
                displayName = new { stringValue = displayName },
                deckName = new { stringValue = deckName },
                score = new { integerValue = score }
            }
        };

        HttpResponseMessage response = await client.PatchAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            throw new Exception(json);
        }
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(string deckName)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";

        var body = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "leaderboards" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "deckName" },
                        op = "EQUAL",
                        value = new { stringValue = deckName }
                    }
                },
                orderBy = new[]
                {
                    new { field = new { fieldPath = "score" }, direction = "DESCENDING" }
                },
                limit = 10
            }
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(url, body);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) return new List<LeaderboardEntry>();

        List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();
        using JsonDocument document = JsonDocument.Parse(json);

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("document", out JsonElement doc) && doc.TryGetProperty("fields", out JsonElement fields))
            {
                leaderboard.Add(new LeaderboardEntry
                {
                    LocalId = fields.GetProperty("localId").GetProperty("stringValue").GetString() ?? "",
                    DisplayName = fields.GetProperty("displayName").GetProperty("stringValue").GetString() ?? "Unknown",
                    DeckName = fields.GetProperty("deckName").GetProperty("stringValue").GetString() ?? "",
                    Score = int.Parse(fields.GetProperty("score").GetProperty("integerValue").GetString() ?? "0")
                });
            }
        }
        return leaderboard;
    }
}