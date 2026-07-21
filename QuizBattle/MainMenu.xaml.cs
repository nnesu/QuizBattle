using QuizBattle.Services;

namespace QuizBattle;

public partial class MainMenu : ContentPage
{
    public MainMenu()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AudioService.Instance.PlayBgmAsync("bgm_lobby.mp3");
    }

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        _ = AudioService.Instance.PlaySfxAsync("sfx_click.mp3");
        await Navigation.PushAsync(new SourceMaterial());
    }

    private async void OnDecksClicked(object sender, EventArgs e)
    {
        _ = AudioService.Instance.PlaySfxAsync("sfx_click.mp3");
        await Navigation.PushAsync(new DecksPage());
    }

    private async void OnAccountSettingsClicked(object sender, EventArgs e)
    {
        _ = AudioService.Instance.PlaySfxAsync("sfx_click.mp3");
        await Navigation.PushAsync(new AccountPage());
    }

    private async void OnLeaderboardClicked(object sender, EventArgs e)
    {
        _ = AudioService.Instance.PlaySfxAsync("sfx_click.mp3");
        await Navigation.PushAsync(new LeaderboardPage());
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        AudioService.Instance.StopBgm();
        Application.Current?.Quit();
    }
}