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
        GameSettings.SelectedDeckUid = "";
        RefreshDropdownItems();
        ApplyDifficultyPreset("Medium");
    }

    private void OnDifficultyChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isChangingDifficulty || !e.Value) return;
        var cb = sender as CheckBox;
        if (cb == EasyCheck) ApplyDifficultyPreset("Easy");
        else if (cb == MediumCheck) ApplyDifficultyPreset("Medium");
        else if (cb == HardCheck) ApplyDifficultyPreset("Hard");
        else if (cb == ZenCheck) ApplyDifficultyPreset("Zen");
    }

    private void ApplyDifficultyPreset(string level)
    {
        _isChangingDifficulty = true;
        EasyCheck.IsChecked = (level == "Easy");
        MediumCheck.IsChecked = (level == "Medium");
        HardCheck.IsChecked = (level == "Hard");
        ZenCheck.IsChecked = (level == "Zen");

        if (level == "Easy")
        {
            GameSettings.CurrentDifficulty = "Easy";
            GameSettings.IsZenMode = false;
            GameSettings.PlayerLives = 5;
            GameSettings.CorrectAnswersRequired = 1;
            GameSettings.TimeLimitSeconds = 30;
            GameSettings.MaxQuestions = 20;
            TimerTitleLabel.Text = "Timer (Default: 30s):";
            TimerEntry.Text = "30";
            TimerEntry.IsEnabled = true;
        }
        else if (level == "Medium")
        {
            GameSettings.CurrentDifficulty = "Medium";
            GameSettings.IsZenMode = false;
            GameSettings.PlayerLives = 4;
            GameSettings.CorrectAnswersRequired = 2;
            GameSettings.TimeLimitSeconds = 15;
            GameSettings.MaxQuestions = 20;
            TimerTitleLabel.Text = "Timer (Default: 15s):";
            TimerEntry.Text = "15";
            TimerEntry.IsEnabled = true;
        }
        else if (level == "Hard")
        {
            GameSettings.CurrentDifficulty = "Hard";
            GameSettings.IsZenMode = false;
            GameSettings.PlayerLives = 3;
            GameSettings.CorrectAnswersRequired = 3;
            GameSettings.TimeLimitSeconds = 7;
            GameSettings.MaxQuestions = 20;
            TimerTitleLabel.Text = "Timer (Default: 7s):";
            TimerEntry.Text = "7";
            TimerEntry.IsEnabled = true;
        }
        else if (level == "Zen")
        {
            GameSettings.CurrentDifficulty = "Zen";
            GameSettings.IsZenMode = true;
            GameSettings.PlayerLives = 999;
            GameSettings.CorrectAnswersRequired = 3;
            GameSettings.TimeLimitSeconds = -1;
            GameSettings.MaxQuestions = 20;
            TimerTitleLabel.Text = "Timer: NO TIMER (ZEN MODE)";
            TimerEntry.Text = "0";
            TimerEntry.IsEnabled = false;
        }
        _isChangingDifficulty = false;
    }

    private void OnTimerTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue) || !TimerEntry.IsEnabled) return;
        string digitsOnly = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
        if (digitsOnly != e.NewTextValue)
        {
            TimerEntry.Text = digitsOnly;
            return;
        }
        if (int.TryParse(digitsOnly, out int val) && val > 100)
        {
            TimerEntry.Text = "100";
        }
    }

    private void ToggleDropdownLayout(object? sender, EventArgs e)
    {
        DropdownScrollContainer.IsVisible = !DropdownScrollContainer.IsVisible;
        if (DropdownScrollContainer.IsVisible) RefreshDropdownItems();
    }

    private async void RefreshDropdownItems()
    {
        DropdownStackList.Children.Clear();
        var decks = await _dbService.GetDecksAsync();
        foreach (var deck in decks)
        {
            string shortenedUid = deck.Uid.Length > 5 ? deck.Uid.Substring(0, 5) : deck.Uid;
            string displayLabel = $"{deck.Name.ToUpper()} [{shortenedUid}]";

            var rowBtn = new Button
            {
                Text = displayLabel,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Fill,
                CornerRadius = 0,
                HeightRequest = 40,
                FontAttributes = FontAttributes.Bold
            };

            rowBtn.Clicked += (s, e) => {
                GameSettings.SelectedDeckName = deck.Name;
                GameSettings.SelectedDeckUid = deck.Uid;
                DeckDropdownButton.Text = $"ACTIVE DECK: {deck.Name.ToUpper()} ({shortenedUid})";
                DropdownScrollContainer.IsVisible = false;
            };
            DropdownStackList.Children.Add(rowBtn);
        }
    }

    private void CloseSetupPopup(object? sender, EventArgs e) => SetupPopupOverlay.IsVisible = false;
    private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

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
                string results = await _aiService.GenerateQuestionsAsync(inputNotes.Text, GameSettings.MaxQuestions);

                var newDeck = await _dbService.CreateDeckAsync(title.Text.Trim());
                await _dbService.ImportDeckFromTextAsync(newDeck.Name, results, clearExisting: true, false, newDeck.Uid);

                GameSettings.SelectedDeckName = newDeck.Name;
                GameSettings.SelectedDeckUid = newDeck.Uid;

                string shortenedUid = newDeck.Uid.Length > 5 ? newDeck.Uid.Substring(0, 5) : newDeck.Uid;
                DeckDropdownButton.Text = $"ACTIVE DECK: {GameSettings.SelectedDeckName.ToUpper()} ({shortenedUid})";
                SetupPopupOverlay.IsVisible = false;
            }
            catch (Exception ex) { await DisplayAlert("ERROR", ex.Message, "OK"); }
        };

        SetupPopupBody.Children.Clear();
        SetupPopupBody.Children.Add(new Label { Text = "Generate Deck with AI:", TextColor = Colors.White, FontAttributes = FontAttributes.Bold });
        SetupPopupBody.Children.Add(title); SetupPopupBody.Children.Add(inputNotes); SetupPopupBody.Children.Add(genBtn);
        SetupPopupOverlay.IsVisible = true;
    }

    private void OpenImportDeckModal(object? sender, EventArgs e)
    {
        var importName = new Entry { Placeholder = "Deck Name...", TextColor = Colors.White, BackgroundColor = Colors.Black };
        var streamData = new Editor { Placeholder = "Paste exported text format...", HeightRequest = 140, TextColor = Colors.White, BackgroundColor = Colors.Black };
        var saveBtn = new Button { Text = "SAVE DECK", BackgroundColor = Colors.DodgerBlue, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };

        saveBtn.Clicked += async (s, ev) => {
            if (string.IsNullOrWhiteSpace(importName.Text) || string.IsNullOrWhiteSpace(streamData.Text)) return;

            var newDeck = await _dbService.CreateDeckAsync(importName.Text.Trim());
            await _dbService.ImportDeckFromTextAsync(newDeck.Name, streamData.Text, clearExisting: true, false, newDeck.Uid);

            GameSettings.SelectedDeckName = newDeck.Name;
            GameSettings.SelectedDeckUid = newDeck.Uid;

            string shortenedUid = newDeck.Uid.Length > 5 ? newDeck.Uid.Substring(0, 5) : newDeck.Uid;
            DeckDropdownButton.Text = $"ACTIVE DECK: {GameSettings.SelectedDeckName.ToUpper()} ({shortenedUid})";
            SetupPopupOverlay.IsVisible = false;
        };

        SetupPopupBody.Children.Clear();
        SetupPopupBody.Children.Add(new Label { Text = "Import Deck Text Format:", TextColor = Colors.White });
        SetupPopupBody.Children.Add(importName); SetupPopupBody.Children.Add(streamData); SetupPopupBody.Children.Add(saveBtn);
        SetupPopupOverlay.IsVisible = true;
    }

    private async void OnFinalLaunchClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GameSettings.SelectedDeckUid))
        {
            await DisplayAlert("SELECTION REQUIRED", "Please select or create a deck before launching.", "OK");
            return;
        }

        var deck = await _dbService.GetDeckByUidAsync(GameSettings.SelectedDeckUid);
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

        if (!GameSettings.IsZenMode && int.TryParse(TimerEntry.Text, out int parsedTimer))
        {
            parsedTimer = Math.Clamp(parsedTimer, 0, 100);
            GameSettings.TimeLimitSeconds = parsedTimer == 0 ? -1 : parsedTimer;
        }
        await Navigation.PushAsync(new MainPage());
    }
}