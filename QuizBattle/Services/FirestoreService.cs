using System.Net.Http.Json;
using System.Text.Json;
using QuizBattle.Models;

namespace QuizBattle.Services;

public class FirestoreService
{
    private const string ProjectId =
        "quizbattle-bd5e7";

    public async Task CreateUserProfile(
        User user)
    {
        using HttpClient client =
            new HttpClient();

        string url =
            $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{user.LocalId}";

        var body = new
        {
            fields = new
            {
                email = new
                {
                    stringValue =
                        user.Email
                },

                displayName = new
                {
                    stringValue =
                        "No Name"
                },

                photoUrl = new
                {
                    stringValue =
                        string.Empty
                }
            }
        };

        HttpResponseMessage response =
            await client.PatchAsJsonAsync(
                url,
                body);

        string json =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                json);
        }
    }

    public async Task<User> GetUserProfile(
        string localId)
    {
        using HttpClient client =
            new HttpClient();

        string url =
            $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}";

        HttpResponseMessage response =
            await client.GetAsync(
                url);

        string json =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                json);
        }

        JsonDocument document =
            JsonDocument.Parse(
                json);

        JsonElement fields =
            document.RootElement
                .GetProperty(
                    "fields");

        return new User
        {
            Email =
                fields
                    .GetProperty(
                        "email")
                    .GetProperty(
                        "stringValue")
                    .GetString()
                ?? string.Empty,

            DisplayName =
                fields
                    .GetProperty(
                        "displayName")
                    .GetProperty(
                        "stringValue")
                    .GetString()
                ?? "No Name",

            PhotoUrl =
                fields.TryGetProperty(
                    "photoUrl",
                    out JsonElement photo)
                ? photo
                    .GetProperty(
                        "stringValue")
                    .GetString()
                    ?? string.Empty
                : string.Empty,

            LocalId =
                localId
        };
    }

    public async Task UpdateDisplayName(
        string localId,
        string displayName)
    {
        using HttpClient client =
            new HttpClient();

        string url =
            $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}?updateMask.fieldPaths=displayName";

        var body = new
        {
            fields = new
            {
                displayName = new
                {
                    stringValue =
                        displayName
                }
            }
        };

        HttpResponseMessage response =
            await client.PatchAsJsonAsync(
                url,
                body);

        string json =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                json);
        }
    }

    public async Task UpdatePhotoUrl(
        string localId,
        string photoUrl)
    {
        using HttpClient client =
            new HttpClient();

        string url =
            $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}?updateMask.fieldPaths=photoUrl";

        var body = new
        {
            fields = new
            {
                photoUrl = new
                {
                    stringValue =
                        photoUrl
                }
            }
        };

        HttpResponseMessage response =
            await client.PatchAsJsonAsync(
                url,
                body);

        string json =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                json);
        }
    }

    public async Task DeleteUserProfile(
        string localId)
    {
        using HttpClient client =
            new HttpClient();

        string url =
            $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{localId}";

        HttpResponseMessage response =
            await client.DeleteAsync(
                url);

        string json =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                json);
        }
    }
}