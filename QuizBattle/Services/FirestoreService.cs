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
    public async Task SubmitLeaderboardScore(string localId, string displayName, string deckUid, int score, string idToken)
    {
        using HttpClient client = new HttpClient();

        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/leaderboards/{Uri.EscapeDataString(deckUid)}_{localId}";

        // Payload configured to use clean deckUid keys natively
        var body = new { fields = new { localId = new { stringValue = localId }, displayName = new { stringValue = displayName }, deckUid = new { stringValue = deckUid }, score = new { integerValue = score.ToString() } } };

        var uploadResponse = await client.PatchAsJsonAsync(url, body);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            string errorPayload = await uploadResponse.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[Leaderboard Upload Rejection]: {uploadResponse.StatusCode} - {errorPayload}");
            return;
        }

        List<string> documentPathsToDelete = new List<string>();
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
        var queryBody = new { structuredQuery = new { from = new[] { new { collectionId = "leaderboards" } }, where = new { fieldFilter = new { field = new { fieldPath = "deckUid" }, op = "EQUAL", value = new { stringValue = deckUid } } } } };

        HttpResponseMessage response = await client.PostAsJsonAsync(queryUrl, queryBody);
        if (response.IsSuccessStatusCode)
        {
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var temporaryList = new List<(string Path, int Score)>();

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("document", out JsonElement doc) && doc.TryGetProperty("fields", out JsonElement fields))
                {
                    string rawScore = "0";
                    if (fields.TryGetProperty("score", out var scoreProp))
                    {
                        if (scoreProp.TryGetProperty("integerValue", out var iv))
                            rawScore = iv.GetString() ?? "0";
                        else if (scoreProp.TryGetProperty("doubleValue", out var dv))
                            rawScore = dv.ValueKind == JsonValueKind.Number ? dv.GetRawText() : (dv.GetString() ?? "0");
                    }

                    if (doc.TryGetProperty("name", out JsonElement nameProp))
                    {
                        temporaryList.Add((nameProp.GetString() ?? "", int.Parse(rawScore)));
                    }
                }
            }

            var losers = temporaryList.OrderByDescending(x => x.Score).Skip(10).ToList();
            foreach (var loser in losers)
            {
                documentPathsToDelete.Add(loser.Path);
            }
        }

        foreach (string docPath in documentPathsToDelete)
        {
            if (!string.IsNullOrWhiteSpace(docPath))
            {
                string deleteUrl = $"https://firestore.googleapis.com/v1/{docPath}";
                await client.DeleteAsync(deleteUrl);
            }
        }
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(string deckUid, string idToken)
    {
        using HttpClient client = new HttpClient();

        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
        var body = new { structuredQuery = new { from = new[] { new { collectionId = "leaderboards" } }, where = new { fieldFilter = new { field = new { fieldPath = "deckUid" }, op = "EQUAL", value = new { stringValue = deckUid } } } } };
        HttpResponseMessage response = await client.PostAsJsonAsync(url, body);
        var results = new List<LeaderboardEntry>();

        if (!response.IsSuccessStatusCode) return results;

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            if (element.TryGetProperty("document", out JsonElement doc) && doc.TryGetProperty("fields", out JsonElement fields))
            {
                string rawScore = "0";
                if (fields.TryGetProperty("score", out var scoreProp))
                {
                    if (scoreProp.TryGetProperty("integerValue", out var iv))
                        rawScore = iv.GetString() ?? "0";
                    else if (scoreProp.TryGetProperty("doubleValue", out var dv))
                        rawScore = dv.ValueKind == JsonValueKind.Number ? dv.GetRawText() : (dv.GetString() ?? "0");
                }

                results.Add(new LeaderboardEntry
                {
                    DisplayName = fields.TryGetProperty("displayName", out var dn) ? dn.GetProperty("stringValue").GetString() ?? "Anonymous" : "Anonymous",
                    Score = int.Parse(rawScore)
                });
            }
        }

        return results.OrderByDescending(r => r.Score).Take(10).ToList();
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
                string fullPathName = doc.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
                string parsedCloudUid = Path.GetFileName(fullPathName);

                decks.Add(new DeckEntity
                {
                    Name = fields.TryGetProperty("deckName", out var n) ? n.GetProperty("stringValue").GetString() ?? "" : "",
                    Description = fields.TryGetProperty("content", out var c) ? c.GetProperty("stringValue").GetString() ?? "" : "",
                    Uid = parsedCloudUid
                });
            }
        }
        return string.IsNullOrWhiteSpace(searchTerm) ? decks : decks.Where(d => d.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task FileUploadDeckToCloudAsync(string deckName, string deckContent, string deckUid)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/global_decks/{Uri.EscapeDataString(deckUid)}";
        await client.PatchAsJsonAsync(url, new { fields = new { deckName = new { stringValue = deckName }, content = new { stringValue = deckContent } } });
    }
}