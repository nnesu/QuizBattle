namespace QuizBattle;

using QuizBattle.Models;

public partial class MainMenu : ContentPage
{
    public MainMenu()
    {
        InitializeComponent();
    }

    private async void PlayClicked(object sender, EventArgs e)
    {
        string? difficulty = await DisplayActionSheet(
            "Choose Difficulty",
            "Cancel",
            null,
            "Zen",
            "Easy",
            "Medium",
            "Hard");

        if (difficulty == null || difficulty == "Cancel")
        {
            return;
        }

        switch (difficulty)
        {
            case "Zen":
                GameSettings.IsZenMode = true;
                GameSettings.PlayerLives = int.MaxValue;
                GameSettings.CorrectAnswersRequired = 1;
                GameSettings.TimeLimitSeconds = -1;
                break;

            case "Easy":
                GameSettings.IsZenMode = false;
                GameSettings.PlayerLives = 5;
                GameSettings.CorrectAnswersRequired = 1;
                break;

            case "Medium":
                GameSettings.IsZenMode = false;
                GameSettings.PlayerLives = 4;
                GameSettings.CorrectAnswersRequired = 2;
                break;

            case "Hard":
                GameSettings.IsZenMode = false;
                GameSettings.PlayerLives = 3;
                GameSettings.CorrectAnswersRequired = 3;
                break;
        }

        if (!GameSettings.IsZenMode)
        {
            string? time = await DisplayActionSheet(
                "Choose Time",
                "Cancel",
                null,
                "Untimed",
                "Bullet (1 minute)",
                "Blitz (5 minutes)",
                "Rapid (10 minutes)");

            if (time == null || time == "Cancel")
            {
                return;
            }

            switch (time)
            {
                case "Untimed":
                    GameSettings.TimeLimitSeconds = -1;
                    break;

                case "Bullet (1 minute)":
                    GameSettings.TimeLimitSeconds = 60;
                    break;

                case "Blitz (5 minutes)":
                    GameSettings.TimeLimitSeconds = 300;
                    break;

                case "Rapid (10 minutes)":
                    GameSettings.TimeLimitSeconds = 600;
                    break;
            }
        }

        await Navigation.PushAsync(new SourceMaterial());
    }
}