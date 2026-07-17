using QuizBattle.Models;

namespace QuizBattle.Helpers;

public static class SessionManager
{
    public static void Save(User user)
    {
        Preferences.Set(
            "Email",
            user.Email);

        Preferences.Set(
            "Token",
            user.IdToken);

        Preferences.Set(
            "LocalId",
            user.LocalId);

        Preferences.Set(
            "DisplayName",
            user.DisplayName);

        Preferences.Set(
            "PhotoUrl",
            user.PhotoUrl);
    }   

    public static User GetUser()
    {
        return new User
        {
            Email = Preferences.Get(
                "Email",
                string.Empty),

            IdToken = Preferences.Get(
                "Token",
                string.Empty),

            LocalId = Preferences.Get(
                "LocalId",
                string.Empty),

            DisplayName = Preferences.Get(
                "DisplayName",
                "No Name"),

            PhotoUrl = Preferences.Get(
                "PhotoUrl",
                string.Empty)
        };
    }

    public static bool IsLoggedIn()
    {
        return !string.IsNullOrWhiteSpace(
            Preferences.Get(
                "Token",
                string.Empty));
    }

    public static void Logout()
    {
        Preferences.Clear();
    }
}