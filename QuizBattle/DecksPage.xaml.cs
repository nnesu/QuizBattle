using System.Text;
using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class DecksPage : ContentPage
{
    private readonly AIService _aiService = new AIService();
    private readonly DatabaseService _dbService = new DatabaseService();
    private DeckEntity? _currentDeck;

    public DecksPage()
    {
        InitializeComponent();
        LoadDecks();
    }

    private async void LoadDecks()
    {
        DecksGrid.Children.Clear();
        DecksGrid.RowDefinitions.Clear();

        var decks = await _dbService.GetDecksAsync();
        int row = 0, col = 0;

        foreach (var deck in decks)
        {
            if (col == 0) DecksGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var btn = new Button
            {
                Text = deck.Name.ToUpper(),
                HeightRequest = 100,
                BackgroundColor = Color.FromArgb("#264653"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold
            };
            btn.Clicked += (s, e) => ViewDeck(deck);

            Grid.SetRow(btn, row);
            Grid.SetColumn(btn, col);
            DecksGrid.Children.Add(btn);

            col++;
            if (col > 1) { col = 0; row++; }
        }

        if (col == 0) DecksGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var addBtn = new Button
        {
            Text = "+ NEW DECK",
            HeightRequest = 100,
            BackgroundColor = Color.FromArgb("#444"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };
        addBtn.Clicked += OnAddNewDeckClicked;
        Grid.SetRow(addBtn, row);
        Grid.SetColumn(addBtn, col);
        DecksGrid.Children.Add(addBtn);
    }

    private void ViewDeck(DeckEntity deck)
    {
        _currentDeck = deck;
        DeckTitleLabel.Text = deck.Name.ToUpper();
        DecksView.IsVisible = false;
        CardsView.IsVisible = true;
        LoadCards();
    }

    private void CloseCardsView(object? sender, EventArgs e)
    {
        CardsView.IsVisible = false;
        DecksView.IsVisible = true;
        LoadDecks();
    }

    private async void LoadCards()
    {
        if (_currentDeck == null) return;
        CardsGrid.Children.Clear();
        CardsGrid.RowDefinitions.Clear();

        var questions = await _dbService.GetQuestionsForDeckAsync(_currentDeck.Id);
        int row = 0, col = 0;

        foreach (var q in questions)
        {
            if (col == 0) CardsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var cardBtn = new Button
            {
                Text = $"[{q.Type.ToUpper()}]\n{q.Text}",
                HeightRequest = 90,
                FontSize = 12,
                BackgroundColor = Color.FromArgb("#1D3557"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold
            };

            cardBtn.Clicked += (s, e) => OpenEditCardPopup(q);

            Grid.SetRow(cardBtn, row);
            Grid.SetColumn(cardBtn, col);
            CardsGrid.Children.Add(cardBtn);

            col++;
            if (col > 1) { col = 0; row++; }
        }
    }

    private void OpenEditCardPopup(QuestionEntity question)
    {
        string currentRawText = question.Type.Equals("Identification", StringComparison.OrdinalIgnoreCase)
            ? $"Identification|{question.Text}|{question.AnswersRaw}"
            : $"MultipleChoice|{question.Text}|{question.OptionsRaw.Replace('|', '|')}|ANS|{question.AnswersRaw}";

        var label = new Label { Text = "EDIT CARD FORMAT:", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var editor = new Editor { Text = currentRawText, HeightRequest = 100, BackgroundColor = Colors.Black, TextColor = Colors.White };

        var saveBtn = new Button { Text = "SAVE CHANGES", BackgroundColor = Color.FromArgb("#2A9D8F"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var deleteBtn = new Button { Text = "DELETE", BackgroundColor = Color.FromArgb("#E63946"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold };

        saveBtn.Clicked += async (s, ev) => {
            string updatedText = editor.Text?.Trim() ?? "";

            if (!ValidateCardFormat(updatedText, out string errorMsg))
            {
                await DisplayAlert("INVALID FORMAT", errorMsg, "OK");
                return;
            }

            string[] parts = updatedText.Split('|');
            question.Type = parts[0].Trim();
            question.Text = parts[1].Trim();

            if (question.Type.Equals("Identification", StringComparison.OrdinalIgnoreCase))
            {
                question.AnswersRaw = parts[2].Trim();
            }
            else
            {
                question.OptionsRaw = $"{parts[2].Trim()}|{parts[3].Trim()}|{parts[4].Trim()}|{parts[5].Trim()}";
                question.AnswersRaw = parts[7].Trim();
            }

            await _dbService.SaveQuestionAsync(question);
            PopupBackground.IsVisible = false;
            LoadCards();
        };

        deleteBtn.Clicked += async (s, ev) => {
            await _dbService.DeleteQuestionAsync(question.Id);
            PopupBackground.IsVisible = false;
            LoadCards();
        };

        var btnGrid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } }, ColumnSpacing = 10 };
        btnGrid.Children.Add(deleteBtn); Grid.SetColumn(deleteBtn, 0);
        btnGrid.Children.Add(saveBtn); Grid.SetColumn(saveBtn, 1);

        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(label);
        layout.Children.Add(editor);
        layout.Children.Add(btnGrid);

        ShowPopup(layout);
    }

    private bool ValidateCardFormat(string rawText, out string errorMessage)
    {
        errorMessage = "";
        if (string.IsNullOrWhiteSpace(rawText))
        {
            errorMessage = "Card text cannot be empty.";
            return false;
        }

        string[] parts = rawText.Split('|');
        if (parts.Length < 3)
        {
            errorMessage = "Invalid card syntax! At least 3 pipe-separated parameters required.\n\nExample:\nIdentification|Question|Answer";
            return false;
        }

        string type = parts[0].Trim();
        if (!type.Equals("Identification", StringComparison.OrdinalIgnoreCase) &&
            !type.Equals("MultipleChoice", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Invalid question type! Must start with 'Identification' or 'MultipleChoice'.";
            return false;
        }

        if (type.Equals("MultipleChoice", StringComparison.OrdinalIgnoreCase))
        {
            int ansIndex = Array.IndexOf(parts, "ANS");
            if (ansIndex == -1)
            {
                errorMessage = "Multiple Choice format error! Must contain '|ANS|' delimiter before correct answer.\n\nExample:\nMultipleChoice|Q|Opt1|Opt2|Opt3|Opt4|ANS|CorrectAnswer";
                return false;
            }
            if (parts.Length < 8)
            {
                errorMessage = "Multiple Choice format error! Missing options or answer parameter.\n\nExpected:\nMultipleChoice|Q|Opt1|Opt2|Opt3|Opt4|ANS|CorrectAnswer";
                return false;
            }
        }

        return true;
    }

    public async void OnAddNewDeckClicked(object? sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("NEW DECK", "Enter deck filename title:", "CREATE", "CANCEL");
        if (!string.IsNullOrWhiteSpace(name))
        {
            await _dbService.CreateDeckAsync(name.Trim());
            LoadDecks();
        }
    }

    private async void OnRenameDeckClicked(object? sender, EventArgs e)
    {
        if (_currentDeck == null) return;
        string newName = await DisplayPromptAsync("RENAME DECK", "Enter a new name:", "SAVE", "CANCEL", initialValue: _currentDeck.Name);
        if (!string.IsNullOrWhiteSpace(newName) && newName != _currentDeck.Name)
        {
            await _dbService.RenameDeckAsync(_currentDeck.Id, newName.Trim());
            _currentDeck.Name = newName.Trim();
            DeckTitleLabel.Text = _currentDeck.Name.ToUpper();
            LoadCards();
        }
    }

    private async void OnDeleteDeckClicked(object? sender, EventArgs e)
    {
        if (_currentDeck == null) return;
        bool confirm = await DisplayAlert("DELETE DECK", $"Permanently delete '{_currentDeck.Name}'?", "YES", "NO");
        if (confirm)
        {
            await _dbService.DeleteDeckAsync(_currentDeck.Id);
            CloseCardsView(null, null!);
        }
    }

    private void DismissActivePopup(object? sender, EventArgs e) => PopupBackground.IsVisible = false;

    private void ShowPopup(View view)
    {
        PopupContentStack.Children.Clear();
        PopupContentStack.Children.Add(view);
        PopupBackground.IsVisible = true;
    }

    private void OpenGenerateCardsPopup(object? sender, EventArgs e)
    {
        var label = new Label { Text = "PASTE MATERIAL NOTES:", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var editor = new Editor { HeightRequest = 180, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "GENERATE API", BackgroundColor = Color.FromArgb("#2A9D8F"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold };

        btn.Clicked += async (s, ev) => {
            if (string.IsNullOrWhiteSpace(editor.Text) || _currentDeck == null) return;
            btn.IsEnabled = false; btn.Text = "RUNNING...";
            try
            {
                string res = await _aiService.GenerateQuestionsAsync(editor.Text, 10);
                await _dbService.ImportDeckFromTextAsync(_currentDeck.Name, res, clearExisting: false);
                PopupBackground.IsVisible = false;
                LoadCards();
            }
            catch (Exception ex) { await DisplayAlert("ERROR", ex.Message, "OK"); }
        };

        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(label); layout.Children.Add(editor); layout.Children.Add(btn);
        ShowPopup(layout);
    }

    private void OpenCreateCardPopup(object? sender, EventArgs e)
    {
        var label = new Label { Text = "ADD CUSTOM CARD MANUALLY", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var example = new Label { Text = "Format examples:\nIdentification|Question|Answer\nMultipleChoice|Q|Opt1|Opt2|Opt3|Opt4|ANS|Opt1", TextColor = Colors.Gray, FontSize = 11 };
        var editor = new Editor { HeightRequest = 80, BackgroundColor = Colors.Black, TextColor = Colors.White, Placeholder = "Type here..." };
        var btn = new Button { Text = "CREATE", BackgroundColor = Color.FromArgb("#457B9D"), FontAttributes = FontAttributes.Bold };

        btn.Clicked += async (s, ev) => {
            if (_currentDeck == null) return;
            string raw = editor.Text?.Trim() ?? "";
            if (!ValidateCardFormat(raw, out string err))
            {
                await DisplayAlert("VALIDATION ERROR", err, "OK");
                return;
            }
            await _dbService.ImportDeckFromTextAsync(_currentDeck.Name, raw, clearExisting: false);
            PopupBackground.IsVisible = false;
            LoadCards();
        };

        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(label); layout.Children.Add(example); layout.Children.Add(editor); layout.Children.Add(btn);
        ShowPopup(layout);
    }

    private void OpenImportPopup(object? sender, EventArgs e)
    {
        var label = new Label { Text = "IMPORT DECK DATA TEXT:", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var editor = new Editor { HeightRequest = 180, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "MERGE / SAVE", BackgroundColor = Color.FromArgb("#A8DADC"), TextColor = Color.FromArgb("#1D3557"), FontAttributes = FontAttributes.Bold };

        btn.Clicked += async (s, ev) => {
            if (string.IsNullOrWhiteSpace(editor.Text) || _currentDeck == null) return;
            await _dbService.ImportDeckFromTextAsync(_currentDeck.Name, editor.Text, clearExisting: false);
            PopupBackground.IsVisible = false;
            LoadCards();
        };

        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(label); layout.Children.Add(editor); layout.Children.Add(btn);
        ShowPopup(layout);
    }

    private void OpenExportPopup(object? sender, EventArgs e)
    {
        if (_currentDeck == null) return;
        Task.Run(async () => {
            string contents = await _dbService.ExportDeckToTextAsync(_currentDeck.Id);
            MainThread.BeginInvokeOnMainThread(() => {
                var label = new Label { Text = "EXPORT DECK TEXT:", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
                var editor = new Editor { Text = contents, IsReadOnly = true, HeightRequest = 180, BackgroundColor = Colors.Black, TextColor = Colors.White };
                var btn = new Button { Text = "COPY TO CLIPBOARD", BackgroundColor = Color.FromArgb("#1D3557"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold };

                btn.Clicked += async (s, ev) => {
                    await Clipboard.Default.SetTextAsync(contents);
                    PopupBackground.IsVisible = false;
                };

                var layout = new VerticalStackLayout { Spacing = 10 };
                layout.Children.Add(label); layout.Children.Add(editor); layout.Children.Add(btn);
                ShowPopup(layout);
            });
        });
    }
}