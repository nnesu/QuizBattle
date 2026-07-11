namespace QuizBattle;

public partial class MainMenu : ContentPage
{
    public MainMenu()
    {
        InitializeComponent();
    }

    private async void PlayClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SourceMaterial());
    }
}