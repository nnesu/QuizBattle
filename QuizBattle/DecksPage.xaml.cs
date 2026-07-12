using System.Text;
using QuizBattle.Services;

namespace QuizBattle;

public partial class DecksPage : ContentPage
{
    private readonly AIService _aiService = new AIService();
    private string _currentDeck = "";
    private readonly string _decksFolder = Path.Combine(FileSystem.Current.AppDataDirectory, "Decks");

    public DecksPage()
    {
        InitializeComponent();
        if (!Directory.Exists(_decksFolder)) Directory.CreateDirectory(_decksFolder);
        LoadDecks();
    }

    private async void OnUniversalBackClicked(object? sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
        else
        {
            await Shell.Current.GoToAsync("//MainMenu");
        }
    }

    private void LoadDecks()
    {
        DecksGrid.Children.Clear();
        DecksGrid.RowDefinitions.Clear();

        var files = Directory.GetFiles(_decksFolder, "*.txt");
        int row = 0, col = 0;

        foreach (var file in files)
        {
            if (col == 0) DecksGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            string name = Path.GetFileNameWithoutExtension(file);
            var btn = new Button
            {
                Text = name.ToUpper(),
                HeightRequest = 100,
                BackgroundColor = Color.FromArgb("#264653"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold
            };
            btn.Clicked += (s, e) => ViewDeck(name);

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

    private void ViewDeck(string deckName)
    {
        _currentDeck = deckName;
        DeckTitleLabel.Text = deckName.ToUpper();
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

    private void LoadCards()
    {
        CardsGrid.Children.Clear();
        CardsGrid.RowDefinitions.Clear();

        string path = Path.Combine(_decksFolder, $"{_currentDeck}.txt");
        if (!File.Exists(path)) return;

        var lines = File.ReadAllLines(path);
        int row = 0, col = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var parts = lines[i].Split('|');
            if (parts.Length < 3) continue;

            if (col == 0) CardsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            int currentLineIndex = i;
            string rawLineText = lines[i];
            string questionDisplay = parts[1];

            var cardBtn = new Button
            {
                Text = $"[{parts[0].ToUpper()}]\n{questionDisplay}",
                HeightRequest = 90,
                FontSize = 12,
                BackgroundColor = Color.FromArgb("#1D3557"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold
            };

            cardBtn.Clicked += (s, e) => OpenEditCardPopup(currentLineIndex, rawLineText);

            Grid.SetRow(cardBtn, row);
            Grid.SetColumn(cardBtn, col);
            CardsGrid.Children.Add(cardBtn);

            col++;
            if (col > 1) { col = 0; row++; }
        }
    }

    private void OpenEditCardPopup(int lineIndex, string currentRawText)
    {
        var label = new Label { Text = "EDIT CARD FORMAT:", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var editor = new Editor { Text = currentRawText, HeightRequest = 100, BackgroundColor = Colors.Black, TextColor = Colors.White };

        var saveBtn = new Button { Text = "SAVE CHANGES", BackgroundColor = Color.FromArgb("#2A9D8F"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var deleteBtn = new Button { Text = "DELETE", BackgroundColor = Color.FromArgb("#E63946"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold };

        string path = Path.Combine(_decksFolder, $"{_currentDeck}.txt");

        saveBtn.Clicked += async (s, ev) => {
            string updatedText = editor.Text?.Trim() ?? "";

            if (!ValidateCardFormat(updatedText, out string errorMsg))
            {
                await DisplayAlert("INVALID FORMAT", errorMsg, "OK");
                return;
            }

            var fileLines = File.ReadAllLines(path).ToList();
            if (lineIndex >= 0 && lineIndex < fileLines.Count)
            {
                fileLines[lineIndex] = updatedText;
                File.WriteAllLines(path, fileLines, Encoding.UTF8);
            }

            PopupBackground.IsVisible = false;
            LoadCards();
        };

        deleteBtn.Clicked += (s, ev) => {
            var fileLines = File.ReadAllLines(path).ToList();
            if (lineIndex >= 0 && lineIndex < fileLines.Count)
            {
                fileLines.RemoveAt(lineIndex);
                File.WriteAllLines(path, fileLines, Encoding.UTF8);
            }

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
            string path = Path.Combine(_decksFolder, $"{name.Trim()}.txt");
            if (!File.Exists(path)) File.WriteAllText(path, "", Encoding.UTF8);
            LoadDecks();
        }
    }

    private async void OnRenameDeckClicked(object? sender, EventArgs e)
    {
        string newName = await DisplayPromptAsync("RENAME DECK", "Enter a new name:", "SAVE", "CANCEL", initialValue: _currentDeck);
        if (!string.IsNullOrWhiteSpace(newName) && newName != _currentDeck)
        {
            string oldPath = Path.Combine(_decksFolder, $"{_currentDeck}.txt");
            string newPath = Path.Combine(_decksFolder, $"{newName.Trim()}.txt");
            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                File.Move(oldPath, newPath);
                ViewDeck(newName.Trim());
            }
        }
    }

    private async void OnDeleteDeckClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("DELETE DECK", $"Permanently delete '{_currentDeck}'?", "YES", "NO");
        if (confirm)
        {
            string path = Path.Combine(_decksFolder, $"{_currentDeck}.txt");
            if (File.Exists(path)) File.Delete(path);
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
            if (string.IsNullOrWhiteSpace(editor.Text)) return;
            btn.IsEnabled = false; btn.Text = "RUNNING...";
            try
            {
                string res = await _aiService.GenerateQuestionsAsync(editor.Text, 10);
                string path = Path.Combine(_decksFolder, $"{_currentDeck}.txt");
                File.AppendAllText(path, Environment.NewLine + res, Encoding.UTF8);
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
            string raw = editor.Text?.Trim() ?? "";
            if (!ValidateCardFormat(raw, out string err))
            {
                await DisplayAlert("VALIDATION ERROR", err, "OK");
                return;
            }
            string path = Path.Combine(_decksFolder, $"{_currentDeck}.txt");
            File.AppendAllText(path, Environment.NewLine + raw, Encoding.UTF8);
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

        btn.Clicked += (s, ev) => {
            if (string.IsNullOrWhiteSpace(editor.Text)) return;
            string path = Path.Combine(_decksFolder, $"{_currentDeck}.txt");
            File.WriteAllText(path, editor.Text, Encoding.UTF8);
            PopupBackground.IsVisible = false;
            LoadCards();
        };

        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(label); layout.Children.Add(editor); layout.Children.Add(btn);
        ShowPopup(layout);
    }

    private void OpenExportPopup(object? sender, EventArgs e)
    {
        string path = Path.Combine(_decksFolder, $"{_currentDeck}.txt");
        string contents = File.Exists(path) ? File.ReadAllText(path) : "";

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
    }
}