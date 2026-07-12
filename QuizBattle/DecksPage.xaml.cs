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
                Text = name,
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
        var addBtn = new Button { Text = "+ New Deck", HeightRequest = 100, BackgroundColor = Color.FromArgb("#444"), TextColor = Colors.White };
        addBtn.Clicked += OnAddNewDeckClicked;
        Grid.SetRow(addBtn, row);
        Grid.SetColumn(addBtn, col);
        DecksGrid.Children.Add(addBtn);
    }

    private void ViewDeck(string deckName)
    {
        _currentDeck = deckName;
        DeckTitleLabel.Text = deckName;
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
            string cardText = parts[1];

            var cardBtn = new Button
            {
                Text = $"[{parts[0]}]\n{cardText}",
                HeightRequest = 90,
                FontSize = 12,
                BackgroundColor = Color.FromArgb("#1D3557"),
                TextColor = Colors.White
            };

            cardBtn.Clicked += async (s, e) => {
                bool confirm = await DisplayAlert("Delete Card", "Delete this specific card layout line?", "Yes", "No");
                if (confirm)
                {
                    var updatedLines = File.ReadAllLines(path).ToList();
                    updatedLines.RemoveAt(currentLineIndex);
                    File.WriteAllLines(path, updatedLines, Encoding.UTF8);
                    LoadCards();
                }
            };

            Grid.SetRow(cardBtn, row);
            Grid.SetColumn(cardBtn, col);
            CardsGrid.Children.Add(cardBtn);

            col++;
            if (col > 1) { col = 0; row++; }
        }
    }

    public async void OnAddNewDeckClicked(object? sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("New Deck", "Enter deck filename title:", "Create", "Cancel");
        if (!string.IsNullOrWhiteSpace(name))
        {
            string path = Path.Combine(_decksFolder, $"{name.Trim()}.txt");
            if (!File.Exists(path)) File.WriteAllText(path, "", Encoding.UTF8);
            LoadDecks();
        }
    }

    private async void OnRenameDeckClicked(object? sender, EventArgs e)
    {
        string newName = await DisplayPromptAsync("Rename Deck", "Enter a new name:", "Save", "Cancel", initialValue: _currentDeck);
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
        bool confirm = await DisplayAlert("Delete Deck", $"Permanently delete '{_currentDeck}'?", "Yes", "No");
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
        var label = new Label { Text = "Paste Material Notes:", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var editor = new Editor { HeightRequest = 180, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "Generate API", BackgroundColor = Color.FromArgb("#2A9D8F"), TextColor = Colors.White };

        btn.Clicked += async (s, ev) => {
            if (string.IsNullOrWhiteSpace(editor.Text)) return;
            btn.IsEnabled = false; btn.Text = "Running...";
            try
            {
                string res = await _aiService.GenerateQuestionsAsync(editor.Text, 10);
                string path = Path.Combine(_decksFolder, $"{_currentDeck}.txt");
                File.AppendAllText(path, Environment.NewLine + res, Encoding.UTF8);
                PopupBackground.IsVisible = false;
                LoadCards();
            }
            catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        };

        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(label); layout.Children.Add(editor); layout.Children.Add(btn);
        ShowPopup(layout);
    }

    private void OpenCreateCardPopup(object? sender, EventArgs e)
    {
        var label = new Label { Text = "Add Custom Card Manually", TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var example = new Label { Text = "Format examples:\nIdentification|Question|Answer\nMultipleChoice|Q|Opt1|Opt2|Opt3|Opt4|ANS|Opt1", TextColor = Colors.Gray, FontSize = 11 };
        var editor = new Editor { HeightRequest = 80, BackgroundColor = Colors.Black, TextColor = Colors.White, Placeholder = "Type here..." };
        var btn = new Button { Text = "Create", BackgroundColor = Color.FromArgb("#457B9D") };

        btn.Clicked += (s, ev) => {
            string raw = editor.Text?.Trim() ?? "";
            var tokens = raw.Split('|');
            if (tokens.Length < 3 || (!tokens[0].Equals("Identification") && !tokens[0].Equals("MultipleChoice")))
            {
                DisplayAlert("Validation Blocked", "Line format does not conform to required syntax:\nType|Question|Answer...", "Fix");
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
        var label = new Label { Text = "Import Deck Data Text:", TextColor = Colors.White };
        var editor = new Editor { HeightRequest = 180, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "Merge / Save", BackgroundColor = Color.FromArgb("#A8DADC") };

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

        var label = new Label { Text = "Export Deck Text:", TextColor = Colors.White };
        var editor = new Editor { Text = contents, IsReadOnly = true, HeightRequest = 180, BackgroundColor = Colors.Black, TextColor = Colors.White };
        var btn = new Button { Text = "Copy to Clipboard", BackgroundColor = Color.FromArgb("#1D3557") };

        btn.Clicked += async (s, ev) => {
            await Clipboard.Default.SetTextAsync(contents);
            PopupBackground.IsVisible = false;
        };

        var layout = new VerticalStackLayout { Spacing = 10 };
        layout.Children.Add(label); layout.Children.Add(editor); layout.Children.Add(btn);
        ShowPopup(layout);
    }
}