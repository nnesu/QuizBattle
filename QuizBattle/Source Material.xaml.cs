using System.Text;
using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class SourceMaterial : ContentPage
{
    private readonly AIService _aiService = new AIService();
    private readonly DatabaseService _dbService = new DatabaseService();
    private bool _isChangingDifficulty = false;

    public SourceMaterial()
    {
        InitializeComponent();
        GameSettings.SelectedDeckName = "";
        RefreshDropdownItems();
    }

    // difficulty checkbox change
    private void OnDifficultyChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isChangingDifficulty || !e.Value) return;
        _isChangingDifficulty = true;

        var cb = sender as CheckBox;
        if (cb == EasyCheck)
        {
            NormalCheck.IsChecked = false; HardCheck.IsChecked = false;
            GameSettings.CurrentDifficulty = "Easy";
            TimerTitleLabel.Text = "Timer (0-100s, Default: 0s):";
            TimerEntry.Text = "0";
        }
        else if (cb == NormalCheck)
        {
            EasyCheck.IsChecked = false; HardCheck.IsChecked = false;
            GameSettings.CurrentDifficulty = "Normal";
            TimerTitleLabel.Text = "Timer (0-100s, Default: 15s):";
            TimerEntry.Text = "15";
        }
        else if (cb == HardCheck)
        {
            EasyCheck.IsChecked = false; NormalCheck.IsChecked = false;
            GameSettings.CurrentDifficulty = "Hard";
            TimerTitleLabel.Text = "Timer (0-100s, Default: 7s):";
            TimerEntry.Text = "7";
        }

        _isChangingDifficulty = false;
    }

    // timer input validation
    private void OnTimerTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue)) return;

        string digitsOnly = new string(e.NewTextValue.Where(char.IsDigit).ToArray());

        if (digitsOnly != e.NewTextValue)
        {
            TimerEntry.Text = digitsOnly;
            return;
        }

        if (int.TryParse(digitsOnly, out int val))
        {
            if (val > 100)
            {
                TimerEntry.Text = "100";
            }
        }
    }

    private void ToggleDropdownLayout(object? sender, EventArgs e)
    {
        DropdownScrollContainer.IsVisible = !DropdownScrollContainer.IsVisible;
        if (DropdownScrollContainer.IsVisible) RefreshDropdownItems();
    }

    // refresh deck dropdown list
    private async void RefreshDropdownItems()
    {
        DropdownStackList.Children.Clear();
        var decks = await _dbService.GetDecksAsync();

        foreach (var deck in decks)
        {
            var rowBtn = new Button
            {
                Text = deck.Name.ToUpper(),
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Fill,
                CornerRadius = 0,
                HeightRequest = 40,
                FontAttributes = FontAttributes.Bold
            };

            rowBtn.Clicked += (s, e) => {
                GameSettings.SelectedDeckName = deck.Name;
                DeckDropdownButton.Text = $"ACTIVE DECK: {deck.Name.ToUpper()} ▾";
                DropdownScrollContainer.IsVisible = false;
            };

            DropdownStackList.Children.Add(rowBtn);
        }
    }

    private void CloseSetupPopup(object? sender, EventArgs e) => SetupPopupOverlay.IsVisible = false;

    // generate deck with ai
    private void OpenGenerateDeckModal(object? sender, EventArgs e)
    {
        var title = new Entry { Placeholder = "New Deck Name...", TextColor = Colors.White, BackgroundColor = Colors.Black };
        var inputNotes = new Editor { Placeholder = "Paste study material notes here...", HeightRequest = 140, TextColor = Colors.White, BackgroundColor = Colors.Black };
        var genBtn = new Button { Text = "GENERATE QUESTIONS", BackgroundColor = Colors.SeaGreen, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };

        genBtn.Clicked += async (s, ev) => {
            if (string.IsNullOrWhiteSpace(title.Text) || string.IsNullOrWhiteSpace(inputNotes.Text)) return;
            genBtn.IsEnabled = false; genBtn.Text = "GENERATING DECK...";
            try
            {
                string results = await _aiService.GenerateQuestionsAsync(inputNotes.Text, 15);
                await _dbService.ImportDeckFromTextAsync(title.Text.Trim(), results);

                GameSettings.SelectedDeckName = title.Text.Trim();
                DeckDropdownButton.Text = $"ACTIVE DECK: {GameSettings.SelectedDeckName.ToUpper()} ▾";
                SetupPopupOverlay.IsVisible = false;
            }
            catch (Exception ex) { await DisplayAlert("ERROR", ex.Message, "OK"); }
        };

        SetupPopupBody.Children.Clear();
        SetupPopupBody.Children.Add(new Label { Text = "Generate Deck with AI:", TextColor = Colors.White, FontAttributes = FontAttributes.Bold });
        SetupPopupBody.Children.Add(title); SetupPopupBody.Children.Add(inputNotes); SetupPopupBody.Children.Add(genBtn);
        SetupPopupOverlay.IsVisible = true;
    }

    // import deck text
    private void OpenImportDeckModal(object? sender, EventArgs e)
    {
        var importName = new Entry { Placeholder = "Deck Name...", TextColor = Colors.White, BackgroundColor = Colors.Black };
        var streamData = new Editor { Placeholder = "Paste exported text format...", HeightRequest = 140, TextColor = Colors.White, BackgroundColor = Colors.Black };
        var saveBtn = new Button { Text = "SAVE DECK", BackgroundColor = Colors.DodgerBlue, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };

        saveBtn.Clicked += async (s, ev) => {
            if (string.IsNullOrWhiteSpace(importName.Text) || string.IsNullOrWhiteSpace(streamData.Text)) return;
            await _dbService.ImportDeckFromTextAsync(importName.Text.Trim(), streamData.Text);

            GameSettings.SelectedDeckName = importName.Text.Trim();
            DeckDropdownButton.Text = $"ACTIVE DECK: {GameSettings.SelectedDeckName.ToUpper()} ▾";
            SetupPopupOverlay.IsVisible = false;
        };

        SetupPopupBody.Children.Clear();
        SetupPopupBody.Children.Add(new Label { Text = "Import Deck Text Format:", TextColor = Colors.White });
        SetupPopupBody.Children.Add(importName); SetupPopupBody.Children.Add(streamData); SetupPopupBody.Children.Add(saveBtn);
        SetupPopupOverlay.IsVisible = true;
    }

    // launch battle game session
    private async void OnFinalLaunchClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GameSettings.SelectedDeckName))
        {
            await DisplayAlert("SELECTION REQUIRED", "Please select or create a deck before launching.", "OK");
            return;
        }

        var deck = await _dbService.GetDeckByNameAsync(GameSettings.SelectedDeckName);
        if (deck == null)
        {
            await DisplayAlert("DECK NOT FOUND", "Selected deck does not exist in the database.", "OK");
            return;
        }

        var questions = await _dbService.GetQuestionsForDeckAsync(deck.Id);
        if (questions.Count == 0)
        {
            await DisplayAlert("EMPTY DECK", "The selected deck contains no question cards.", "OK");
            return;
        }

        if (int.TryParse(TimerEntry.Text, out int parsedTimer))
        {
            parsedTimer = Math.Clamp(parsedTimer, 0, 100);
            if (parsedTimer == 0)
            {
                GameSettings.IsTimerEnabled = false;
                GameSettings.TimeLimitSeconds = -1;
                GameSettings.IsZenMode = true;
            }
            else
            {
                GameSettings.IsTimerEnabled = true;
                GameSettings.TimerSeconds = parsedTimer;
                GameSettings.TimeLimitSeconds = parsedTimer;
                GameSettings.IsZenMode = false;
            }
        }

        await Navigation.PushAsync(new MainPage());
    }
}