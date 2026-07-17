// MainMenu.xaml.cs
namespace QuizBattle;

public partial class MainMenu : ContentPage
{
    public MainMenu()
    {
        InitializeComponent();
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
}