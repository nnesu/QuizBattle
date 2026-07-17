using QuizBattle.Helpers;
using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void Login_Clicked(
        object sender,
        EventArgs e)
    {
        string email =
            EmailEntry.Text?.Trim()
            ?? string.Empty;

        string password =
            PasswordEntry.Text
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert(
                "Error",
                "Please enter an email.",
                "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert(
                "Error",
                "Please enter a password.",
                "OK");
            return;
        }

        FirebaseAuthService service =
            new FirebaseAuthService();

        try
        {
            User? user =
                await service.Login(
                    email,
                    password);

            if (user == null)
            {
                await DisplayAlert(
                    "Login Failed",
                    "Unable to retrieve user information.",
                    "OK");
                return;
            }

            bool verified =
                await service.CheckEmailVerified(
                    user.IdToken);

            if (!verified)
            {
                await DisplayAlert(
                    "Email Not Verified",
                    "Please verify your email before logging in.",
                    "OK");
                return;
            }

            FirestoreService firestore = new FirestoreService();

            User profile =
                await firestore.GetUserProfile(
                    user.LocalId);

            profile.IdToken =
                user.IdToken;

            SessionManager.Save(
                profile);

            // Redirect directly to the core game shell instead of AccountPage
            Application.Current!.Windows[0].Page = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Login Failed",
                ex.Message,
                "OK");
        }
    }

    private async void SignUp_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new SignUpPage());
    }
}