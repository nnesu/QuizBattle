using System.Net.Http.Json;
using System.Text.Json;
using QuizBattle.Models;

namespace QuizBattle.Services;

public class FirestoreService
{
    private const string ProjectId = "quizbattle-bd5e7";

    // --- PROFILE METHODS ---
    public async Task CreateUserProfile(User user)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{user.LocalId}";
        var body = new { fields = new { email = new { stringValue = user.Email }, displayName = new { stringValue = "No Name" }, photoUrl = new { stringValue = string.Empty } } };
        await client.PatchAsJsonAsync(url, body);
    }

    public async Task<User> GetUserProfile(string localId)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}";
        HttpResponseMessage response = await client.GetAsync(url);
        JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement fields = document.RootElement.GetProperty("fields");
        return new User { Email = fields.GetProperty("email").GetProperty("stringValue").GetString() ?? "", DisplayName = fields.GetProperty("displayName").GetProperty("stringValue").GetString() ?? "No Name", LocalId = localId };
    }

    public async Task UpdateDisplayName(string localId, string displayName)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}?updateMask.fieldPaths=displayName";
        await client.PatchAsJsonAsync(url, new { fields = new { displayName = new { stringValue = displayName } } });
    }

    public async Task UpdatePhotoUrl(string localId, string photoUrl)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}?updateMask.fieldPaths=photoUrl";
        await client.PatchAsJsonAsync(url, new { fields = new { photoUrl = new { stringValue = photoUrl } } });
    }

    public async Task DeleteUserProfile(string localId)
    {
        using HttpClient client = new HttpClient();
        await client.DeleteAsync($"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}");
    }

    // --- LEADERBOARD METHODS ---
    public async Task SubmitLeaderboardScore(string localId, string displayName, string deckName, int score)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/leaderboards/{Uri.EscapeDataString(deckName)}_{localId}";
        await client.PatchAsJsonAsync(url, new { fields = new { localId = new { stringValue = localId }, displayName = new { stringValue = displayName }, deckName = new { stringValue = deckName }, score = new { integerValue = score } } });
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(string deckName)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
        var body = new { structuredQuery = new { from = new[] { new { collectionId = "leaderboards" } }, where = new { fieldFilter = new { field = new { fieldPath = "deckName" }, op = "EQUAL", value = new { stringValue = deckName } } }, orderBy = new[] { new { field = new { fieldPath = "score" }, direction = "DESCENDING" } } } };
        HttpResponseMessage response = await client.PostAsJsonAsync(url, body);
        var results = new List<LeaderboardEntry>();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("document", out JsonElement doc) && doc.TryGetProperty("fields", out JsonElement fields))
                results.Add(new LeaderboardEntry { DisplayName = fields.GetProperty("displayName").GetProperty("stringValue").GetString() ?? "", Score = int.Parse(fields.GetProperty("score").GetProperty("integerValue").GetString() ?? "0") });
        }
        return results;
    }

    // --- GLOBAL SEARCH & SYNC METHODS ---
    public async Task<List<DeckEntity>> SearchGlobalDecksAsync(string searchTerm)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
        var body = new { structuredQuery = new { from = new[] { new { collectionId = "global_decks" } } } };
        HttpResponseMessage response = await client.PostAsJsonAsync(url, body);
        var decks = new List<DeckEntity>();
        if (!response.IsSuccessStatusCode) return decks;
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("document", out JsonElement doc) && doc.TryGetProperty("fields", out JsonElement fields))
            {
                decks.Add(new DeckEntity
                {
                    Name = fields.TryGetProperty("deckName", out var n) ? n.GetProperty("stringValue").GetString() ?? "" : "",
                    Description = fields.TryGetProperty("content", out var c) ? c.GetProperty("stringValue").GetString() ?? "" : ""
                });
            }
        }
        return string.IsNullOrWhiteSpace(searchTerm) ? decks : decks.Where(d => d.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task UploadDeckToCloudAsync(string deckName, string deckContent)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/global_decks/{Uri.EscapeDataString(deckName)}";
        await client.PatchAsJsonAsync(url, new { fields = new { deckName = new { stringValue = deckName }, content = new { stringValue = deckContent } } });
    }
}