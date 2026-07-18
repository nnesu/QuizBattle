using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class DecksPage : ContentPage
{
    private readonly AIService _aiService = new AIService();
    private readonly DatabaseService _dbService = new DatabaseService();
    private readonly FirestoreService _firestoreService = new FirestoreService();
    private DeckEntity? _currentDeck;

    public DecksPage() { InitializeComponent(); LoadDecks(); }

    private async void LoadDecks()
    {
        DecksGrid.Children.Clear();
        var decks = await _dbService.GetDecksAsync();
        int row = 0, col = 0;
        foreach (var deck in decks)
        {
            var btn = new Button { Text = deck.Name.ToUpper(), HeightRequest = 100, BackgroundColor = Color.FromArgb("#17274A"), TextColor = Colors.White, CornerRadius = 18, FontAttributes = FontAttributes.Bold };
            btn.Clicked += (s, e) => ViewDeck(deck);
            Grid.SetRow(btn, row); Grid.SetColumn(btn, col);
            DecksGrid.Children.Add(btn);
            col++; if (col > 1) { col = 0; row++; }
        }
    }

    private void ViewDeck(DeckEntity deck) { _currentDeck = deck; DeckTitleLabel.Text = deck.Name.ToUpper(); DecksView.IsVisible = false; CardsView.IsVisible = true; LoadCards(); }
    private void CloseCardsView(object? sender, EventArgs e) { CardsView.IsVisible = false; DecksView.IsVisible = true; LoadDecks(); }
    private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

    private async void OnSearchGlobalClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SearchDecksPage());
    }

    private async void OnUploadDeckClicked(object sender, EventArgs e)
    {
        if (_currentDeck == null) return;
        if (await DisplayAlert("Publish", $"Upload '{_currentDeck.Name}'?", "YES", "NO"))
        {
            string deckText = await _dbService.ExportDeckToTextAsync(_currentDeck.Id);
            await _firestoreService.UploadDeckToCloudAsync(_currentDeck.Name, deckText);
            await DisplayAlert("Success", "Uploaded!", "OK");
        }
    }

    private async void LoadCards()
    {
        CardsGrid.Children.Clear();
        var questions = await _dbService.GetQuestionsForDeckAsync(_currentDeck!.Id);
        int row = 0, col = 0;
        foreach (var q in questions)
        {
            var cardBtn = new Button { Text = q.Text, HeightRequest = 90, BackgroundColor = Color.FromArgb("#152440"), TextColor = Colors.White, CornerRadius = 16 };
            cardBtn.Clicked += (s, e) => OpenEditCardPopup(q);
            Grid.SetRow(cardBtn, row); Grid.SetColumn(cardBtn, col);
            CardsGrid.Children.Add(cardBtn);
            col++; if (col > 1) { col = 0; row++; }
        }
    }

    public void OpenGenerateCardsPopup(object? sender, EventArgs e)
    {
        var editor = new Editor { HeightRequest = 150, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "GENERATE", BackgroundColor = Color.FromArgb("#14B8A6") };
        btn.Clicked += async (s, ev) => { await _dbService.ImportDeckFromTextAsync(_currentDeck!.Name, await _aiService.GenerateQuestionsAsync(editor.Text, 10)); DismissActivePopup(null, null!); LoadCards(); };
        ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Paste Material:" }, editor, btn } });
    }

    public void OpenCreateCardPopup(object? sender, EventArgs e)
    {
        var editor = new Editor { HeightRequest = 80, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "CREATE", BackgroundColor = Color.FromArgb("#8B5CF6") };
        btn.Clicked += async (s, ev) => { await _dbService.ImportDeckFromTextAsync(_currentDeck!.Name, editor.Text); DismissActivePopup(null, null!); LoadCards(); };
        ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Format: Type|Q|A" }, editor, btn } });
    }

    public void OpenImportPopup(object? sender, EventArgs e)
    {
        var editor = new Editor { HeightRequest = 150, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "IMPORT", BackgroundColor = Color.FromArgb("#A8DADC") };
        btn.Clicked += async (s, ev) => { await _dbService.ImportDeckFromTextAsync(_currentDeck!.Name, editor.Text); DismissActivePopup(null, null!); LoadCards(); };
        ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Paste Data:" }, editor, btn } });
    }

    public void OpenExportPopup(object? sender, EventArgs e)
    {
        Task.Run(async () => {
            string contents = await _dbService.ExportDeckToTextAsync(_currentDeck!.Id);
            MainThread.BeginInvokeOnMainThread(() => ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Export Data:" }, new Editor { Text = contents, IsReadOnly = true, HeightRequest = 150 } } }));
        });
    }

    private void OpenEditCardPopup(QuestionEntity question)
    {
        var editor = new Editor { Text = question.Text, HeightRequest = 100, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var saveBtn = new Button { Text = "SAVE", BackgroundColor = Color.FromArgb("#2A9D8F") };
        saveBtn.Clicked += async (s, ev) => { question.Text = editor.Text; await _dbService.SaveQuestionAsync(question); DismissActivePopup(null, null!); LoadCards(); };
        ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Edit Card" }, editor, saveBtn } });
    }

    public async void OnAddNewDeckClicked(object? sender, EventArgs e) { await _dbService.CreateDeckAsync(await DisplayPromptAsync("NEW DECK", "Name:")); LoadDecks(); }
    private async void OnRenameDeckClicked(object? sender, EventArgs e) { await _dbService.RenameDeckAsync(_currentDeck!.Id, await DisplayPromptAsync("RENAME", "New name:", initialValue: _currentDeck.Name) ?? _currentDeck.Name); DeckTitleLabel.Text = _currentDeck.Name.ToUpper(); }
    private async void OnDeleteDeckClicked(object? sender, EventArgs e) { await _dbService.DeleteDeckAsync(_currentDeck!.Id); CloseCardsView(null, null!); }
    private void DismissActivePopup(object? sender, EventArgs e) => PopupBackground.IsVisible = false;
    private void ShowPopup(View view) { PopupContentStack.Children.Clear(); PopupContentStack.Children.Add(view); PopupBackground.IsVisible = true; }
}