using QuizBattle.Helpers;
using QuizBattle.Models;

namespace QuizBattle;

public partial class MainMenu : ContentPage
{
    public MainMenu()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserProfileImage();
    }

    private void LoadUserProfileImage()
    {
        try
        {
            if (SessionManager.IsLoggedIn())
            {
                User user = SessionManager.GetUser();

                if (!string.IsNullOrWhiteSpace(user.PhotoUrl))
                {
                    ProfileImage.Source = new UriImageSource
                    {
                        Uri = new Uri(user.PhotoUrl),
                        CachingEnabled = false
                    };
                }
                else if (!string.IsNullOrWhiteSpace(user.LocalId))
                {
                    ProfileImage.Source = new UriImageSource
                    {
                        Uri = new Uri($"https://res.cloudinary.com/j3fal3hz/image/upload/profiles/{user.LocalId}.jpg"),
                        CachingEnabled = false
                    };
                }
                else
                {
                    ProfileImage.Source = "avatar1.png";
                }
            }
            else
            {
                ProfileImage.Source = "avatar1.png";
            }
        }
        catch
        {
            ProfileImage.Source = "avatar1.png";
        }
    }

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SourceMaterial());
    }

    private async void OnDecksClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new DecksPage());
    }

    private async void OnAccountSettingsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AccountPage());
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }

    private async void OnLeaderboardClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LeaderboardPage());
    }
}