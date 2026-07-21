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
                    Padding = new Thickness(12, 10)
                };

                // 4 Columns Layout: [Rank (#1)] [Avatar Picture] [Player Name] [Score]
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Auto }, // Column 0: Rank (#1)
                        new ColumnDefinition { Width = GridLength.Auto }, // Column 1: Profile Image
                        new ColumnDefinition { Width = GridLength.Star }, // Column 2: Name
                        new ColumnDefinition { Width = GridLength.Auto }  // Column 3: Score
                    },
                    ColumnSpacing = 12
                };

                // 1. Rank Label
                var rankLabel = new Label
                {
                    Text = $"#{rank}",
                    TextColor = Color.FromArgb("#C4B5FD"),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(rankLabel, 0);
                grid.Children.Add(rankLabel);

                // 2. Circular Profile Image
                var avatarImg = new Image
                {
                    Source = !string.IsNullOrWhiteSpace(entry.PhotoUrl)
                        ? new UriImageSource { Uri = new Uri(entry.PhotoUrl), CachingEnabled = true }
                        : "avatar1.png",
                    WidthRequest = 36,
                    HeightRequest = 36,
                    Aspect = Aspect.AspectFill,
                    VerticalOptions = LayoutOptions.Center
                };

                var avatarBorder = new Border
                {
                    WidthRequest = 36,
                    HeightRequest = 36,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                    StrokeThickness = 1,
                    Stroke = Color.FromArgb("#31405F"),
                    BackgroundColor = Color.FromArgb("#18213C"),
                    Content = avatarImg,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(avatarBorder, 1);
                grid.Children.Add(avatarBorder);

                // 3. Name Label
                var nameLabel = new Label
                {
                    Text = entry.DisplayName,
                    TextColor = Colors.White,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                Grid.SetColumn(nameLabel, 2);
                grid.Children.Add(nameLabel);

                // 4. Score Label
                var scoreLabel = new Label
                {
                    Text = $"{entry.Score} pts",
                    TextColor = Color.FromArgb("#5EEAD4"),
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(scoreLabel, 3);
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