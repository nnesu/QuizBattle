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

    private async void Create_Clicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text ?? string.Empty;
        string confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty; // New reference

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Error", "Please enter an email.", "OK");
            return;
        }

        if (!email.Contains("@"))
        {
            await DisplayAlert("Error", "Please enter a valid email.", "OK");
            return;
        }

        if (password.Length < 6)
        {
            await DisplayAlert("Error", "Password must be at least 6 characters.", "OK");
            return;
        }

        // NEW VALIDATION: Check if confirmation matches
        if (password != confirmPassword)
        {
            await DisplayAlert("Error", "Passwords do not match. Please verify both fields.", "OK");
            return;
        }

        FirebaseAuthService service = new FirebaseAuthService();

        try
        {
            User? user = await service.SignUp(email, password);

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
            await DisplayAlert("Sign Up Failed", ex.Message, "OK");
        }
    }

    private async void Login_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // Password Eye Toggles
    private void TogglePassword_Clicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        TogglePasswordBtn.Text = PasswordEntry.IsPassword ? "👁️" : "🙈";
    }

    private void ToggleConfirmPassword_Clicked(object sender, EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        ToggleConfirmPasswordBtn.Text = ConfirmPasswordEntry.IsPassword ? "👁️" : "🙈";
    }
}