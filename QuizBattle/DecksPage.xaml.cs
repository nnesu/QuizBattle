using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class DecksPage : ContentPage
{
    private readonly AIService _aiService = new AIService();
    private readonly DatabaseService _dbService = new DatabaseService();
    private readonly FirestoreService _firestoreService = new FirestoreService();
    private DeckEntity? _currentDeck;

    public DecksPage()
    {
        InitializeComponent();
    }

    // Automatically refreshes the deck list whenever returning to this screen
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDecks();
    }

    private async void LoadDecks()
    {
        DecksGrid.Children.Clear();
        DecksGrid.RowDefinitions.Clear(); // Reset grid row definitions

        var decks = await _dbService.GetDecksAsync();
        int row = 0, col = 0;

        foreach (var deck in decks)
        {
            // Dynamically define a new row height when starting Column 0
            if (col == 0)
            {
                DecksGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Main item block container layout cell
            var cellContainer = new VerticalStackLayout
            {
                Spacing = 6,
                HorizontalOptions = LayoutOptions.Fill
            };

            // Main interaction card button (displays clean deck title)
            var btn = new Button
            {
                Text = deck.Name.ToUpper(),
                HeightRequest = 100,
                BackgroundColor = Color.FromArgb("#17274A"),
                TextColor = Colors.White,
                CornerRadius = 18,
                FontAttributes = FontAttributes.Bold
            };
            btn.Clicked += async (s, e) => {
                await AudioService.PlayButtonClickAsync();
                ViewDeck(deck);
            };
            cellContainer.Children.Add(btn);

            // Side-by-side layout deck configuration tags strip container
            var tagsLayout = new HorizontalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center
            };

            // TAG 1: Origin Flag Label (Always Visible)
            var originTag = new Label
            {
                Text = deck.IsReadOnly ? "☁️ CLOUD" : "💻 LOCAL",
                TextColor = deck.IsReadOnly ? Color.FromArgb("#A78BFA") : Color.FromArgb("#94A3B8"), // Purple for cloud, Slate for local
                FontSize = 11,
                FontAttributes = FontAttributes.Bold
            };
            tagsLayout.Children.Add(originTag);

            // TAG 2: Status Indicator Flag Label (Conditional: Beaten or Blank)
            var mastery = await _dbService.GetDeckMasteryAsync(deck.Id);
            if (mastery != null && mastery.HighScore > 0)
            {
                var beatenTag = new Label
                {
                    Text = "⚔️ BEATEN",
                    TextColor = Color.FromArgb("#5EEAD4"), // Neon teal accent contrast
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold
                };
                tagsLayout.Children.Add(beatenTag);
            }

            // Append the compiled tags strip container to the parent cell card stack block
            cellContainer.Children.Add(tagsLayout);

            // Route the cell item element out to the primary UI dashboard collection grid slots
            Grid.SetRow(cellContainer, row);
            Grid.SetColumn(cellContainer, col);
            DecksGrid.Children.Add(cellContainer);

            col++;
            if (col > 1)
            {
                col = 0;
                row++;
            }
        }
    }

    private void ViewDeck(DeckEntity deck)
    {
        _currentDeck = deck;
        DeckTitleLabel.Text = deck.Name.ToUpper();
        DecksView.IsVisible = false;
        CardsView.IsVisible = true;
        RenameDeckBtn.IsVisible = !deck.IsReadOnly;
        ModificationControlsFooter.IsVisible = !deck.IsReadOnly;
        LoadCards();
    }

    private async void CloseCardsView(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        CardsView.IsVisible = false;
        DecksView.IsVisible = true;
        LoadDecks();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        await Navigation.PopAsync();
    }

    private async void OnSearchGlobalClicked(object sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        await Navigation.PushAsync(new SearchDecksPage());
    }

    private async void OnUploadDeckClicked(object sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        if (_currentDeck == null) return;
        if (await DisplayAlert("Publish", $"Upload '{_currentDeck.Name}'?", "YES", "NO"))
        {
            string deckText = await _dbService.ExportDeckToTextAsync(_currentDeck.Id);
            await _firestoreService.FileUploadDeckToCloudAsync(_currentDeck.Name, deckText, _currentDeck.Uid);
            await DisplayAlert("Success", "Uploaded!", "OK");
        }
    }

    private async void LoadCards()
    {
        CardsGrid.Children.Clear();
        CardsGrid.RowDefinitions.Clear(); // FIX: Clear existing row definitions so overflow cards render

        var questions = await _dbService.GetQuestionsForDeckAsync(_currentDeck!.Id);
        int row = 0, col = 0;

        foreach (var q in questions)
        {
            // FIX: Dynamically add a row definition whenever a new grid row starts
            if (col == 0)
            {
                CardsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            var cardBtn = new Button
            {
                Text = q.Text,
                HeightRequest = 90,
                BackgroundColor = Color.FromArgb("#152440"),
                TextColor = Colors.White,
                CornerRadius = 16
            };
            cardBtn.Clicked += async (s, e) => {
                await AudioService.PlayButtonClickAsync();
                OpenEditCardPopup(q);
            };

            Grid.SetRow(cardBtn, row);
            Grid.SetColumn(cardBtn, col);
            CardsGrid.Children.Add(cardBtn);

            col++;
            if (col > 1)
            {
                col = 0;
                row++;
            }
        }
    }

    public async void OpenGenerateCardsPopup(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        if (_currentDeck?.IsReadOnly == true) return;
        var editor = new Editor { HeightRequest = 150, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "GENERATE", BackgroundColor = Color.FromArgb("#14B8A6") };
        btn.Clicked += async (s, ev) => {
            await AudioService.PlayButtonClickAsync();
            string generatedText = await _aiService.GenerateQuestionsAsync(editor.Text, 10);
            string[] lines = generatedText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                await SaveRawLineToCurrentDeckAsync(line);
            }
            DismissActivePopup(null, null!);
            LoadCards();
        };
        ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Paste Material:" }, editor, btn } });
    }

    public async void OpenCreateCardPopup(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        if (_currentDeck?.IsReadOnly == true) return;
        var editor = new Editor { HeightRequest = 80, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "CREATE", BackgroundColor = Color.FromArgb("#8B5CF6") };
        btn.Clicked += async (s, ev) => {
            await AudioService.PlayButtonClickAsync();
            if (string.IsNullOrWhiteSpace(editor.Text)) return;
            await SaveRawLineToCurrentDeckAsync(editor.Text);
            DismissActivePopup(null, null!);
            LoadCards();
        };
        ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Format: Type|Q|A" }, editor, btn } });
    }

    public async void OpenImportPopup(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        if (_currentDeck?.IsReadOnly == true) return;
        var editor = new Editor { HeightRequest = 150, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "IMPORT", BackgroundColor = Color.FromArgb("#A8DADC") };
        btn.Clicked += async (s, ev) => {
            await AudioService.PlayButtonClickAsync();
            if (string.IsNullOrWhiteSpace(editor.Text)) return;
            string[] lines = editor.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                await SaveRawLineToCurrentDeckAsync(line);
            }
            DismissActivePopup(null, null!);
            LoadCards();
        };
        ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Paste Data:" }, editor, btn } });
    }

    private async Task SaveRawLineToCurrentDeckAsync(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || _currentDeck == null) return;
        string[] parts = line.Split('|');
        if (parts.Length < 3) return;
        string type = parts[0].Trim();
        if (type.Equals("Identification", StringComparison.OrdinalIgnoreCase))
        {
            var q = new QuestionEntity
            {
                DeckId = _currentDeck.Id,
                Type = "Identification",
                Text = parts[1].Trim(),
                AnswersRaw = parts[2].Trim()
            };
            await _dbService.SaveQuestionAsync(q);
        }
        else if (type.Equals("MultipleChoice", StringComparison.OrdinalIgnoreCase) && parts.Length >= 8)
        {
            var q = new QuestionEntity
            {
                DeckId = _currentDeck.Id,
                Type = "MultipleChoice",
                Text = parts[1].Trim(),
                OptionsRaw = $"{parts[2].Trim()}|{parts[3].Trim()}|{parts[4].Trim()}|{parts[5].Trim()}",
                AnswersRaw = parts[7].Trim()
            };
            await _dbService.SaveQuestionAsync(q);
        }
    }

    public async void OpenExportPopup(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        Task.Run(async () => {
            string contents = await _dbService.ExportDeckToTextAsync(_currentDeck!.Id);
            MainThread.BeginInvokeOnMainThread(() => ShowPopup(new VerticalStackLayout { Children = { new Label { Text = "Export Data:" }, new Editor { Text = contents, IsReadOnly = true, HeightRequest = 150 } } }));
        });
    }

    private void OpenEditCardPopup(QuestionEntity question)
    {
        bool isReadOnly = _currentDeck?.IsReadOnly ?? false;
        var editor = new Editor { Text = question.Text, HeightRequest = 100, BackgroundColor = Colors.Black, TextColor = Colors.White, IsReadOnly = isReadOnly };
        var saveBtn = new Button { Text = "SAVE", BackgroundColor = Color.FromArgb("#2A9D8F"), IsVisible = !isReadOnly };
        saveBtn.Clicked += async (s, ev) => {
            await AudioService.PlayButtonClickAsync();
            question.Text = editor.Text;
            await _dbService.SaveQuestionAsync(question);
            DismissActivePopup(null, null!);
            LoadCards();
        };
        var layout = new VerticalStackLayout { Children = { new Label { Text = isReadOnly ? "View Card" : "Edit Card" }, editor } };
        if (!isReadOnly) layout.Children.Add(saveBtn);
        ShowPopup(layout);
    }

    public async void OnAddNewDeckClicked(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        await _dbService.CreateDeckAsync(await DisplayPromptAsync("NEW DECK", "Name:"));
        LoadDecks();
    }

    private async void OnRenameDeckClicked(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        if (_currentDeck == null || _currentDeck.IsReadOnly) return;
        await _dbService.RenameDeckAsync(_currentDeck.Id, await DisplayPromptAsync("RENAME", "New name:", initialValue: _currentDeck.Name) ?? _currentDeck.Name);
        DeckTitleLabel.Text = _currentDeck.Name.ToUpper();
    }

    private async void OnDeleteDeckClicked(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        await _dbService.DeleteDeckAsync(_currentDeck!.Id);
        CloseCardsView(null, null!);
    }

    private async void DismissActivePopup(object? sender, EventArgs e)
    {
        await AudioService.PlayButtonClickAsync();
        PopupBackground.IsVisible = false;
    }

    private void ShowPopup(View view) { PopupContentStack.Children.Clear(); PopupContentStack.Children.Add(view); PopupBackground.IsVisible = true; }
}