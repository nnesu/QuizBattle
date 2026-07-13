using QuizBattle.Models;
using QuizBattle.Services;
using System.Linq;

namespace QuizBattle;

public partial class MainPage : ContentPage
{
    private readonly Random random = new();
    private List<Question> battleQuestions = new();
    private Question? currentQuestion;
    private int bossHP;

    private int playerLives;
    private int startingLives;

    private HashSet<string> selectedOptions = new HashSet<string>();
    private Button[] optionButtons = Array.Empty<Button>();
    private readonly Color defaultOptionColor = Colors.LightGray;
    private readonly Color selectedOptionColor = Colors.Green;

    private IDispatcherTimer? battleTimer;
    private int timeRemaining;

    private double heartSize = 28;
    private readonly double heartMinSize = 18;
    private readonly double heartMaxSize = 48;
    private readonly double heartSpacing = 4;

    public MainPage()
    {
        InitializeComponent();

        optionButtons = new Button[]
        {
            OptionButton1,
            OptionButton2,
            OptionButton3,
            OptionButton4
        };

        LivesLayout.SizeChanged += LivesLayout_SizeChanged;
    }

    private void LivesLayout_SizeChanged(object? sender, EventArgs e)
    {
        if (startingLives <= 0 || LivesLayout.Width <= 0)
            return;

        double totalSpacing = heartSpacing * Math.Max(0, startingLives - 1);
        double availableWidth = Math.Max(0, LivesLayout.Width - totalSpacing);
        double candidate = availableWidth / startingLives;
        double computed = Math.Clamp(candidate, heartMinSize, heartMaxSize);

        if (Math.Abs(computed - heartSize) > 0.5)
        {
            heartSize = computed;
            UpdateLivesDisplay();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartBattle();
    }

    // timer tick event
    private async void BattleTimerTick(object? sender, EventArgs e)
    {
        timeRemaining--;
        UpdateTimerLabel();

        if (timeRemaining > 0)
        {
            return;
        }

        battleTimer?.Stop();
        await HandleDefeat("TIME'S UP!", "YOU RAN OUT OF TIME.");
    }

    // start battle session
    private async Task StartBattle()
    {
        AnswerEntry.IsEnabled = true;

        foreach (Button button in optionButtons)
        {
            button.IsEnabled = true;
        }

        QuestionLoader loader = new QuestionLoader();
        string deckFile = string.IsNullOrWhiteSpace(GameSettings.SelectedDeckName)
            ? "QuestionList.txt"
            : GameSettings.SelectedDeckName;

        battleQuestions = await loader.LoadQuestionsAsync(deckFile);

        // reset question mastery progress
        foreach (var q in battleQuestions)
        {
            q.CorrectProgress = 0;
        }

        // assign boss hp based on difficulty settings
        if (GameSettings.IsZenMode || GameSettings.CurrentDifficulty == "Zen")
        {
            bossHP = -1; // represents infinite hp
        }
        else if (GameSettings.CurrentDifficulty == "Easy")
        {
            bossHP = 10;
        }
        else if (GameSettings.CurrentDifficulty == "Hard")
        {
            bossHP = 20;
        }
        else
        {
            bossHP = 15; // default medium hp
        }

        playerLives = GameSettings.PlayerLives;
        startingLives = GameSettings.PlayerLives;

        currentQuestion = null;
        AnswerEntry.Text = string.Empty;
        ResultLabel.Text = string.Empty;
        selectedOptions.Clear();

        foreach (Button button in optionButtons)
        {
            button.BackgroundColor = defaultOptionColor;
        }

        UpdateLabels();
        MasteryLabel.Text = string.Empty;

        if (battleTimer != null)
        {
            battleTimer.Stop();
            battleTimer.Tick -= BattleTimerTick;
            battleTimer = null;
        }

        if (GameSettings.TimeLimitSeconds != -1 && !GameSettings.IsZenMode)
        {
            battleTimer = Dispatcher.CreateTimer();
            battleTimer.Interval = TimeSpan.FromSeconds(1);
            battleTimer.Tick += BattleTimerTick;
        }

        if (LivesLayout.Width > 0)
            LivesLayout_SizeChanged(LivesLayout, EventArgs.Empty);

        NextQuestion();
    }

    // handle defeat dialog
    private async Task HandleDefeat(string title, string message)
    {
        AnswerEntry.IsEnabled = false;

        foreach (Button button in optionButtons)
        {
            button.IsEnabled = false;
        }

        battleTimer?.Stop();

        bool retry = await DisplayAlert(title, message, "RETRY", "LEAVE");

        if (retry)
        {
            await StartBattle();
        }
        else
        {
            bool confirm = await DisplayAlert("ADMIT DEFEAT FOR NOW?", "", "YES", "NO");

            if (confirm)
            {
                await Shell.Current.GoToAsync("//MainMenu");
            }
            else
            {
                await StartBattle();
            }
        }
    }

    // handle victory dialog
    private async Task HandleVictory()
    {
        AnswerEntry.IsEnabled = false;

        foreach (Button button in optionButtons)
        {
            button.IsEnabled = false;
        }

        battleTimer?.Stop();

        bool retry = await DisplayAlert("VICTORY!", "PLAY AGAIN?", "RETRY", "MAIN MENU");

        if (retry)
        {
            await StartBattle();
        }
        else
        {
            await Shell.Current.GoToAsync("//MainMenu");
        }
    }

    private void UpdateLabels()
    {
        // display infinite symbol for zen mode
        if (GameSettings.IsZenMode || bossHP < 0)
        {
            BossLabel.Text = "BOSS HP: ∞";
        }
        else
        {
            BossLabel.Text = $"BOSS HP: {bossHP}";
        }

        if (GameSettings.IsZenMode)
        {
            LivesLabel.Text = "LIVES: ∞";
            LivesLabel.IsVisible = true;
            LivesLayout.IsVisible = false;
        }
        else
        {
            LivesLabel.IsVisible = false;
            LivesLayout.IsVisible = true;
            UpdateLivesDisplay();
        }
    }

    // update player lives display
    private void UpdateLivesDisplay()
    {
        LivesLayout.Children.Clear();
        double size = heartSize <= 0 ? 28 : heartSize;

        for (int i = 0; i < playerLives; i++)
        {
            var img = new Image
            {
                Source = "heart_filled.png",
                WidthRequest = size,
                HeightRequest = size,
                Aspect = Aspect.AspectFit,
                Margin = new Thickness(0, 0, heartSpacing, 0)
            };
            LivesLayout.Children.Add(img);
        }

        if (LivesLayout.Children.Count == 0)
        {
            LivesLayout.Children.Add(new Label
            {
                Text = new string('♥', Math.Max(0, playerLives)),
                FontSize = Math.Min(24, size),
                TextColor = Colors.Red,
                VerticalOptions = LayoutOptions.Center
            });
        }
    }

    private void UpdateMasteryLabel()
    {
        if (currentQuestion == null)
        {
            MasteryLabel.Text = string.Empty;
            return;
        }
        MasteryLabel.Text = $"QUESTION MASTERY: {currentQuestion.CorrectProgress} / {GameSettings.CorrectAnswersRequired}";
    }

    private void UpdateTimerLabel()
    {
        if (GameSettings.TimeLimitSeconds == -1 || GameSettings.IsZenMode)
        {
            TimeLabel.Text = "TIME: ∞";
            return;
        }
        TimeSpan time = TimeSpan.FromSeconds(timeRemaining);
        TimeLabel.Text = $"TIME: {time:mm\\:ss}";
    }

    // load next question
    private void NextQuestion()
    {
        if (battleQuestions.Count == 0 || (!GameSettings.IsZenMode && bossHP <= 0))
        {
            QuestionLabel.Text = "YOU WIN!";
            AnswerEntry.IsEnabled = false;
            battleTimer?.Stop();
            _ = HandleVictory();
            return;
        }

        Question? nextQuestion;

        if (battleQuestions.Count == 1)
        {
            nextQuestion = battleQuestions[0];
        }
        else
        {
            do
            {
                int index = random.Next(battleQuestions.Count);
                nextQuestion = battleQuestions[index];
            }
            while (nextQuestion == currentQuestion);
        }

        currentQuestion = nextQuestion;
        currentQuestion.TimesAsked++;

        AnswerEntry.IsVisible = false;
        MultipleChoiceLayout.IsVisible = false;
        AnswerEntry.Text = string.Empty;
        selectedOptions.Clear();

        foreach (Button button in optionButtons)
        {
            button.BackgroundColor = defaultOptionColor;
        }

        ResultLabel.Text = string.Empty;
        QuestionLabel.Text = currentQuestion.Text;
        UpdateMasteryLabel();

        if (currentQuestion.Type == QuestionType.Identification)
        {
            AnswerEntry.IsVisible = true;
            AnswerEntry.Focus();
        }
        else if (currentQuestion.Type == QuestionType.MultipleChoice)
        {
            MultipleChoiceLayout.IsVisible = true;

            Button[] buttons = { OptionButton1, OptionButton2, OptionButton3, OptionButton4 };

            for (int index = 0; index < buttons.Length; index++)
            {
                if (index < currentQuestion.Options.Count)
                {
                    buttons[index].IsVisible = true;
                    buttons[index].Text = currentQuestion.Options[index];
                }
                else
                {
                    buttons[index].IsVisible = false;
                }
            }
        }

        if (GameSettings.TimeLimitSeconds != -1 && !GameSettings.IsZenMode && battleTimer != null)
        {
            battleTimer.Stop();
            timeRemaining = GameSettings.TimeLimitSeconds;
            UpdateTimerLabel();
            battleTimer.Start();
        }
        else
        {
            UpdateTimerLabel();
        }
    }

    private void OptionClicked(object sender, EventArgs e)
    {
        Button clickedButton = (Button)sender;

        if (selectedOptions.Contains(clickedButton.Text))
        {
            selectedOptions.Remove(clickedButton.Text);
            clickedButton.BackgroundColor = defaultOptionColor;
        }
        else
        {
            selectedOptions.Add(clickedButton.Text);
            clickedButton.BackgroundColor = selectedOptionColor;
        }
    }

    // submit answer logic
    private async void SubmitAnswer(object sender, EventArgs e)
    {
        if (currentQuestion == null)
        {
            return;
        }

        string playerAnswer = AnswerEntry.Text?.Trim() ?? string.Empty;
        bool isCorrect = false;

        if (currentQuestion.Type == QuestionType.Identification)
        {
            if (string.IsNullOrWhiteSpace(playerAnswer))
            {
                ResultLabel.Text = "PLEASE ENTER AN ANSWER.";
                ResultLabel.TextColor = Colors.Orange;
                return;
            }

            isCorrect = currentQuestion.CorrectAnswers.Any(answer =>
                answer.Equals(playerAnswer, StringComparison.OrdinalIgnoreCase));
        }
        else if (currentQuestion.Type == QuestionType.MultipleChoice)
        {
            if (selectedOptions.Count == 0)
            {
                ResultLabel.Text = "PLEASE SELECT AT LEAST ONE ANSWER.";
                ResultLabel.TextColor = Colors.Orange;
                return;
            }

            HashSet<string> correctAnswers = new HashSet<string>(
                currentQuestion.CorrectAnswers,
                StringComparer.OrdinalIgnoreCase);

            isCorrect = selectedOptions.SetEquals(correctAnswers);
        }

        battleTimer?.Stop();

        if (isCorrect)
        {
            ResultLabel.Text = "CORRECT!";
            ResultLabel.TextColor = Colors.Green;

            currentQuestion.TimesCorrect++;
            currentQuestion.CorrectProgress++;

            // reduce boss hp if not zen mode
            if (!GameSettings.IsZenMode && bossHP > 0)
            {
                bossHP--;
            }

            UpdateMasteryLabel();

            if (currentQuestion.CorrectProgress >= GameSettings.CorrectAnswersRequired)
            {
                battleQuestions.Remove(currentQuestion);
            }
        }
        else
        {
            ResultLabel.Text = "INCORRECT!";
            ResultLabel.TextColor = Colors.Red;

            currentQuestion.TimesIncorrect++;

            if (!GameSettings.IsZenMode)
            {
                playerLives--;
            }
        }

        UpdateLabels();

        if (!GameSettings.IsZenMode && playerLives <= 0)
        {
            await HandleDefeat("GAME OVER!", "YOU RAN OUT OF LIVES.");
            return;
        }

        if (!GameSettings.IsZenMode && bossHP <= 0)
        {
            await HandleVictory();
            return;
        }

        await Task.Delay(1000);
        NextQuestion();
    }

    private void OnGiveUpClicked(object? sender, EventArgs e)
    {
        OnBackButtonPressed();
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool confirm = await DisplayAlert("BATTLE IN PROGRESS!", "CONFIRM RETURN TO MENU?", "YES", "NO");

            if (confirm)
            {
                battleTimer?.Stop();
                await Shell.Current.GoToAsync("//MainMenu");
            }
        });

        return true;
    }
}