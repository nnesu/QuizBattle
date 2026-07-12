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

    private void OnDifficultyChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isChangingDifficulty || !e.Value) return;
        _isChangingDifficulty = true;

        var cb = sender as CheckBox;
        if (cb == EasyCheck)
        {
            NormalCheck.IsChecked = false; HardCheck.IsChecked = false;
            GameSettings.CurrentDifficulty = "Easy"; GameSettings.IsTimerEnabled = false;
            GameSettings.IsZenMode = true; GameSettings.TimeLimitSeconds = -1;
            TimerStatusPanel.IsVisible = false;
        }
        else if (cb == NormalCheck)
        {
            EasyCheck.IsChecked = false; HardCheck.IsChecked = false;
            GameSettings.CurrentDifficulty = "Normal"; GameSettings.IsTimerEnabled = true; GameSettings.TimerSeconds = 15;
            GameSettings.TimeLimitSeconds = 15; GameSettings.IsZenMode = false;
            TimerStatusPanel.IsVisible = true; TimerPanelLabel.Text = "Active Turn Counter window: 15 seconds limit"; TimerPanelLabel.TextColor = Colors.Orange;
        }
        else if (cb == HardCheck)
        {
            EasyCheck.IsChecked = false; NormalCheck.IsChecked = false;
            GameSettings.CurrentDifficulty = "Hard"; GameSettings.IsTimerEnabled = true; GameSettings.TimerSeconds = 7;
            GameSettings.TimeLimitSeconds = 7; GameSettings.IsZenMode = false;
            TimerStatusPanel.IsVisible = true; TimerPanelLabel.Text = "CRITICAL BATTLE WINDOW SPEED RUN ATOM TIMER: 7 SECONDS RUN LIMIT!"; TimerPanelLabel.TextColor = Colors.Red;
        }

        _isChangingDifficulty = false;
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
                Text = name,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Fill,
                CornerRadius = 0,
                HeightRequest = 40
            };

            rowBtn.Clicked += (s, e) => {
                GameSettings.SelectedDeckName = name;
                DeckDropdownButton.Text = $"Active Deck: {name} ▾";
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
        var genBtn = new Button { Text = "Generate Questions", BackgroundColor = Colors.SeaGreen };

        genBtn.Clicked += async (s, ev) => {
            if (string.IsNullOrWhiteSpace(title.Text) || string.IsNullOrWhiteSpace(inputNotes.Text)) return;
            genBtn.IsEnabled = false; genBtn.Text = "Generating Deck...";
            try
            {
                string results = await _aiService.GenerateQuestionsAsync(inputNotes.Text, 15);
                string writePath = Path.Combine(_decksDir, $"{title.Text.Trim()}.txt");
                File.WriteAllText(writePath, results, Encoding.UTF8);

                GameSettings.SelectedDeckName = title.Text.Trim();
                DeckDropdownButton.Text = $"Active Deck: {GameSettings.SelectedDeckName} ▾";
                SetupPopupOverlay.IsVisible = false;
            }
            catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
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
        var saveBtn = new Button { Text = "Save Deck", BackgroundColor = Colors.DodgerBlue };

        saveBtn.Clicked += (s, ev) => {
            if (string.IsNullOrWhiteSpace(importName.Text) || string.IsNullOrWhiteSpace(streamData.Text)) return;
            string targetPath = Path.Combine(_decksDir, $"{importName.Text.Trim()}.txt");
            File.WriteAllText(targetPath, streamData.Text, Encoding.UTF8);

            GameSettings.SelectedDeckName = importName.Text.Trim();
            DeckDropdownButton.Text = $"Active Deck: {GameSettings.SelectedDeckName} ▾";
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
            await DisplayAlert("Selection Required", "Please select or create a deck before launching.", "OK");
            return;
        }

        string fullCheckPath = Path.Combine(_decksDir, $"{GameSettings.SelectedDeckName}.txt");
        if (!File.Exists(fullCheckPath) || File.ReadLines(fullCheckPath).Count(l => !string.IsNullOrWhiteSpace(l)) == 0)
        {
            await DisplayAlert("Empty Deck", "The selected deck contains no question cards.", "OK");
            return;
        }

        await Navigation.PushAsync(new MainPage());
    }
}