using QuizBattle.Helpers;
using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class SignUpPage : ContentPage
{
    public SignUpPage()
    {
        InitializeComponent();
    }

    private async void Create_Clicked(
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

        if (!email.Contains("@"))
        {
            await DisplayAlert(
                "Error",
                "Please enter a valid email.",
                "OK");
            return;
        }

        if (password.Length < 6)
        {
            await DisplayAlert(
                "Error",
                "Password must be at least 6 characters.",
                "OK");
            return;
        }

        FirebaseAuthService service =
            new FirebaseAuthService();

        try
        {
            User? user = await service.SignUp(email, password);

            // MOVE THE NULL CHECK UP HERE to satisfy .NET 9's nullable checks
            if (user == null)
            {
                await DisplayAlert("Sign Up Failed", "Could not create user account profile.", "OK");
                return;
            }

            FirestoreService firestore = new FirestoreService();
            await firestore.CreateUserProfile(user);

            await service.SendVerificationEmail(user.IdToken);

            await DisplayAlert(
                "Verify Email",
                "A verification email has been sent. Please verify your email before logging in.",
                "OK");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Sign Up Failed",
                ex.Message,
                "OK");
        }
    }

    private async void Login_Clicked(
        object sender,
        EventArgs e)
    {
        // Simple rollback mechanism to send them back to the login screen wrapper
        await Navigation.PopAsync();
    }
}