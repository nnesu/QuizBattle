using QuizBattle.Helpers;
using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class AccountPage : ContentPage
{
    public AccountPage()
    {
        InitializeComponent();

        LoadUser();
    }

    private void LoadUser()
    {
        User user =
            SessionManager.GetUser();

        DisplayNameLabel.Text =
            string.IsNullOrWhiteSpace(
                user.DisplayName)
            ? "No Name"
            : user.DisplayName;

        EmailLabel.Text =
            $"Email: {user.Email}";

        try
        {
            if (!string.IsNullOrWhiteSpace(
                user.PhotoUrl))
            {
                ProfilePicture.Source =
                    ImageSource.FromUri(
                        new Uri(
                            user.PhotoUrl));
            }
            else
            {
                ProfilePicture.Source =
                    "avatar1.png";
            }
        }
        catch
        {
            ProfilePicture.Source =
                "avatar1.png";
        }
    }

    private async void EditPicture_Clicked(object sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        try
        {
            FileResult? file = await MediaPicker.PickPhotoAsync();
            if (file == null) return;

            User user = SessionManager.GetUser();
            CloudinaryService storage = new CloudinaryService();

            // 1. Upload the image to Cloudinary
            string photoUrl = await storage.UploadProfilePicture(user.LocalId, file);

            // 2. Update database and local session memory
            user.PhotoUrl = photoUrl;
            FirestoreService firestore = new FirestoreService();
            await firestore.UpdatePhotoUrl(user.LocalId, photoUrl);
            SessionManager.Save(user);

            // 3. SAFE UI UPDATE: Offload image stream generation from blocking the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(photoUrl))
                    {
                        // Using a fresh Uri ImageSource safely
                        ProfilePicture.Source = new UriImageSource
                        {
                            Uri = new Uri(photoUrl),
                            CachingEnabled = false // Prevents cache-locking during a rapid update
                        };
                    }
                    else
                    {
                        ProfilePicture.Source = "avatar1.png";
                    }
                }
                catch
                {
                    // Soft fallback if the UI engine trips on the incoming URL string
                    ProfilePicture.Source = "avatar1.png";
                }
            });

            await DisplayAlert("Success", "Profile picture updated.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void EditDetails_Clicked(
        object sender,
        EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        string? result =
            await DisplayPromptAsync(
                "Display Name",
                "Enter a display name:");

        if (string.IsNullOrWhiteSpace(
            result))
        {
            return;
        }

        User user =
            SessionManager.GetUser();

        user.DisplayName =
            result.Trim();

        FirestoreService firestore =
            new FirestoreService();

        await firestore.UpdateDisplayName(
            user.LocalId,
            user.DisplayName);

        SessionManager.Save(
            user);

        DisplayNameLabel.Text =
            user.DisplayName;
    }

    private async void Logout_Clicked(
        object sender,
        EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        SessionManager.Logout();

        // Redirect to a clean login navigation stack upon logout
        Application.Current!.Windows[0].Page =
            new NavigationPage(
                new LoginPage());
    }

    private async void Delete_Clicked(
        object sender,
        EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        bool answer =
            await DisplayAlert(
                "Delete Account",
                "You cannot recover your data after account deletion. Continue?",
                "Yes",
                "No");

        if (!answer)
        {
            return;
        }

        try
        {
            User user =
                SessionManager.GetUser();

            FirebaseAuthService auth =
                new FirebaseAuthService();

            await auth.DeleteAccount(
                user.IdToken);

            FirestoreService firestore =
                new FirestoreService();

            await firestore.DeleteUserProfile(
                user.LocalId);

            SessionManager.Logout();

            await DisplayAlert(
                "Success",
                "Your account has been deleted.",
                "OK");

            // Redirect to a clean login navigation stack upon account deletion
            Application.Current!.Windows[0].Page =
                new NavigationPage(
                    new LoginPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Delete Failed",
                ex.Message,
                "OK");
        }
    }
}