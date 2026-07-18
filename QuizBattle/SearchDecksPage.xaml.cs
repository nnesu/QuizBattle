using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class SearchDecksPage : ContentPage
{
    private readonly FirestoreService _firestoreService = new FirestoreService();
    private readonly DatabaseService _dbService = new DatabaseService();

    public SearchDecksPage() { InitializeComponent(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SearchResultsView.ItemsSource = await _firestoreService.SearchGlobalDecksAsync("");
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        SearchResultsView.ItemsSource = await _firestoreService.SearchGlobalDecksAsync(e.NewTextValue ?? "");
    }

    private async void OnDownloadClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is DeckEntity deck)
        {
            var editor = new Editor { Text = deck.Description, IsReadOnly = true, HeightRequest = 200, BackgroundColor = Colors.Black, TextColor = Colors.White };
            var saveBtn = new Button { Text = "SAVE TO MY DECKS", BackgroundColor = Color.FromArgb("#8B5CF6") };
            var popupLayout = new VerticalStackLayout { Padding = 20, Spacing = 10, BackgroundColor = Color.FromArgb("#091121") };
            popupLayout.Children.Add(new Label { Text = $"PREVIEW: {deck.Name}", FontSize = 18, TextColor = Colors.White, FontAttributes = FontAttributes.Bold });
            popupLayout.Children.Add(editor);
            popupLayout.Children.Add(saveBtn);

            var previewPage = new ContentPage { Content = popupLayout };

            saveBtn.Clicked += async (s, ev) => {
                await _dbService.ImportDeckFromTextAsync(deck.Name, deck.Description, false, true, deck.Uid);
                await DisplayAlert("Success", "Deck added to your local library!", "OK");
                await Navigation.PopModalAsync();
                await Navigation.PopAsync();
            };

            await Navigation.PushModalAsync(previewPage);
        }
    }
}