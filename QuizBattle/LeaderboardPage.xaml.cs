using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class LeaderboardPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private readonly FirestoreService _firestoreService = new FirestoreService();
    private List<DeckEntity> _availableDecks = new();

    public LeaderboardPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDecks();
    }

    private async Task LoadDecks()
    {
        _availableDecks = await _dbService.GetDecksAsync();
        DeckPicker.ItemsSource = _availableDecks.Select(d => $"{d.Name.ToUpper()} [{(d.Uid.Length > 5 ? d.Uid.Substring(0, 5) : d.Uid)}]").ToList();
    }

    private async void OnDeckSelected(object sender, EventArgs e)
    {
        if (DeckPicker.SelectedIndex == -1) return;
        await AudioService.PlayButtonClickAsync();
        var selectedDeck = _availableDecks[DeckPicker.SelectedIndex];

        var localMastery = await _dbService.GetDeckMasteryAsync(selectedDeck.Id);
        LocalHighScoreLabel.Text = localMastery != null ? $"High Score: {localMastery.HighScore} pts" : "High Score: 0 pts";

        LeaderboardStack.Children.Clear();
        LeaderboardStack.Children.Add(new Label { Text = "Loading global scores...", TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center });

        try
        {
            // ROUTING FIX: Pull session data and pass validation credentials downstream safely
            string userToken = "";
            if (QuizBattle.Helpers.SessionManager.IsLoggedIn())
            {
                userToken = QuizBattle.Helpers.SessionManager.GetUser().IdToken;
            }

            var topScores = await _firestoreService.GetLeaderboardAsync(selectedDeck.Uid, userToken);
            LeaderboardStack.Children.Clear();

            if (topScores.Count == 0)
            {
                LeaderboardStack.Children.Add(new Label { Text = "No global scores yet. Be the first!", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center });
                return;
            }

            int rank = 1;
            foreach (var entry in topScores)
            {
                var row = new Border
                {
                    BackgroundColor = Color.FromArgb("#161F38"),
                    Stroke = Color.FromArgb("#2C3A5C"),
                    StrokeThickness = 1,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(14)
                };

                var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } }, ColumnSpacing = 15 };

                grid.Children.Add(new Label { Text = $"#{rank}", TextColor = Color.FromArgb("#C4B5FD"), FontSize = 18, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center });

                var nameLabel = new Label { Text = entry.DisplayName, TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
                Grid.SetColumn(nameLabel, 1);
                grid.Children.Add(nameLabel);

                var scoreLabel = new Label { Text = $"{entry.Score} pts", TextColor = Color.FromArgb("#5EEAD4"), FontSize = 16, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
                Grid.SetColumn(scoreLabel, 2);
                grid.Children.Add(scoreLabel);

                row.Content = grid;
                LeaderboardStack.Children.Add(row);
                rank++;
            }
        }
        catch (Exception ex)
        {
            LeaderboardStack.Children.Clear();
            LeaderboardStack.Children.Add(new Label { Text = "Failed to load global scores.", TextColor = Colors.Red, HorizontalOptions = LayoutOptions.Center });
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        await Navigation.PopAsync();
    }
}