using System.Net.Http.Json;
using System.Text.Json;
using QuizBattle.Models;

namespace QuizBattle.Services;

public class FirebaseAuthService
{
    private const string ApiKey =
        "AIzaSyCNdX6FMWwCMFWau6jrxv3MCwTWCcv7oRc";


    public async Task<User?> SignUp(
        string email,
        string password)
    {
        return await Authenticate(
            email,
            password,
            "signUp");
    }


    public async Task<User?> Login(
        string email,
        string password)
    {
        return await Authenticate(
            email,
            password,
            "signInWithPassword");
    }


    public async Task SendVerificationEmail(
        string idToken)
    {
        using HttpClient client =
            new HttpClient();

        string url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={ApiKey}";

        var body = new
        {
            requestType = "VERIFY_EMAIL",
            idToken
        };

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                url,
                body);

        string json =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }
    }


    public async Task<bool> CheckEmailVerified(
    string idToken)
    {
        using HttpClient client =
            new HttpClient();


        string url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={ApiKey}";


        var body = new
        {
            idToken
        };


        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                url,
                body);


        string json =
            await response.Content
                .ReadAsStringAsync();


        System.Diagnostics.Debug.WriteLine(
            "Firebase lookup response:");

        System.Diagnostics.Debug.WriteLine(
            json);


        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }


        JsonDocument document =
            JsonDocument.Parse(json);


        JsonElement user =
            document.RootElement
                .GetProperty("users")[0];


        return user.GetProperty(
                "emailVerified")
            .GetBoolean();
    }


    public async Task DeleteAccount(
    string idToken)
    {
        using HttpClient client =
            new HttpClient();

        string url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:delete?key={ApiKey}";

        var body = new
        {
            idToken
        };

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                url,
                body);

        string json =
            await response.Content
                .ReadAsStringAsync();

        Console.WriteLine(
            $"DeleteAccount Response: {json}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                json);
        }
    }


    private async Task<User?> Authenticate(
        string email,
        string password,
        string endpoint)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exception(
                "Email cannot be empty.");
        }


        if (string.IsNullOrWhiteSpace(password))
        {
            throw new Exception(
                "Password cannot be empty.");
        }


        if (password.Length < 6)
        {
            throw new Exception(
                "Password must be at least 6 characters.");
        }


        using HttpClient client =
            new HttpClient();


        var body = new
        {
            email,
            password,
            returnSecureToken = true
        };


        string url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:{endpoint}?key={ApiKey}";


        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                url,
                body);


        string json =
            await response.Content
                .ReadAsStringAsync();


        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }


        JsonDocument document =
            JsonDocument.Parse(json);


        return new User
        {
            Email =
                document.RootElement
                    .GetProperty("email")
                    .GetString()
                    ?? string.Empty,


            IdToken =
                document.RootElement
                    .GetProperty("idToken")
                    .GetString()
                    ?? string.Empty,


            LocalId =
                document.RootElement
                    .GetProperty("localId")
                    .GetString()
                    ?? string.Empty,


            EmailVerified = false
        };
    }

}