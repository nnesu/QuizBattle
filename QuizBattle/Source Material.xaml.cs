using System.Text;
using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class SourceMaterial : ContentPage
{
    private readonly AIService _aiService = new AIService();
    private readonly string _decksDir = Path.Combine(FileSystem.Current.AppDataDirectory, "Decks");
    private bool _isChangingDifficulty = false;

    public SourceMaterial()
    {
        InitializeComponent();
        if (!Directory.Exists(_decksDir)) Directory.CreateDirectory(_decksDir);

        GameSettings.SelectedDeckName = "";
        RefreshDropdownItems();
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

    private void RefreshDropdownItems()
    {
        DropdownStackList.Children.Clear();
        var files = Directory.GetFiles(_decksDir, "*.txt");

        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            var rowBtn = new Button
            {
                Text = name.ToUpper(),
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Fill,
                CornerRadius = 0,
                HeightRequest = 40,
                FontAttributes = FontAttributes.Bold
            };

            rowBtn.Clicked += (s, e) => {
                GameSettings.SelectedDeckName = name;
                DeckDropdownButton.Text = $"ACTIVE DECK: {name.ToUpper()} ▾";
                DropdownScrollContainer.IsVisible = false;
            };

            DropdownStackList.Children.Add(rowBtn);
        }
    }

    private void CloseSetupPopup(object? sender, EventArgs e) => SetupPopupOverlay.IsVisible = false;

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
                string writePath = Path.Combine(_decksDir, $"{title.Text.Trim()}.txt");
                File.WriteAllText(writePath, results, Encoding.UTF8);

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

    private void OpenImportDeckModal(object? sender, EventArgs e)
    {
        var importName = new Entry { Placeholder = "Deck Name...", TextColor = Colors.White, BackgroundColor = Colors.Black };
        var streamData = new Editor { Placeholder = "Paste exported text format...", HeightRequest = 140, TextColor = Colors.White, BackgroundColor = Colors.Black };
        var saveBtn = new Button { Text = "SAVE DECK", BackgroundColor = Colors.DodgerBlue, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };

        saveBtn.Clicked += (s, ev) => {
            if (string.IsNullOrWhiteSpace(importName.Text) || string.IsNullOrWhiteSpace(streamData.Text)) return;
            string targetPath = Path.Combine(_decksDir, $"{importName.Text.Trim()}.txt");
            File.WriteAllText(targetPath, streamData.Text, Encoding.UTF8);

            GameSettings.SelectedDeckName = importName.Text.Trim();
            DeckDropdownButton.Text = $"ACTIVE DECK: {GameSettings.SelectedDeckName.ToUpper()} ▾";
            SetupPopupOverlay.IsVisible = false;
        };

        SetupPopupBody.Children.Clear();
        SetupPopupBody.Children.Add(new Label { Text = "Import Deck Text Format:", TextColor = Colors.White });
        SetupPopupBody.Children.Add(importName); SetupPopupBody.Children.Add(streamData); SetupPopupBody.Children.Add(saveBtn);
        SetupPopupOverlay.IsVisible = true;
    }

    private async void OnFinalLaunchClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GameSettings.SelectedDeckName))
        {
            await DisplayAlert("SELECTION REQUIRED", "Please select or create a deck before launching.", "OK");
            return;
        }

        string fullCheckPath = Path.Combine(_decksDir, $"{GameSettings.SelectedDeckName}.txt");
        if (!File.Exists(fullCheckPath) || File.ReadLines(fullCheckPath).Count(l => !string.IsNullOrWhiteSpace(l)) == 0)
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