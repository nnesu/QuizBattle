using QuizBattle.Models;
using QuizBattle.Services;
using System.Linq;

namespace QuizBattle;

public partial class MainPage : ContentPage
{
    private readonly Random random = new();
    private List<Question> battleQuestions = new();
    private Question? currentQuestion;
    private Boss boss = new();
    private double bossHP;
    private double damagePerCorrectAnswer;
    private int playerLives;
    private int startingLives;
    private int totalDeckCardsCount = 0;

    // Score tracking
    private int baseQuestionsScore = 0;
    private int totalStreakBonusEarned = 0;
    private int totalPenaltiesDeducted = 0;
    private int currentStreak = 0;

    // Timer tracking
    private System.Diagnostics.Stopwatch quizTimer = new();
    private int totalMaxTimeAllowed = 0;

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
    private bool heartSizeLocked = false;

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
        if (heartSizeLocked)
            return;
        if (Math.Abs(computed - heartSize) > 0.5)
        {
            heartSize = computed;
            heartSizeLocked = true;
            UpdateLivesDisplay();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartBattle();
    }

    private async void BattleTimerTick(object? sender, EventArgs e)
    {
        timeRemaining--;
        UpdateTimerLabel();
        if (timeRemaining > 0)
        {
            return;
        }
        battleTimer?.Stop();
        quizTimer.Stop();
        ResetScoreMetrics();
        await HandleDefeat("TIME'S UP!", "YOU RAN OUT OF TIME.");
    }

    private async Task StartBattle()
    {
        AnswerEntry.IsEnabled = true;
        foreach (Button button in optionButtons)
        {
            button.IsEnabled = true;
        }

        QuestionLoader loader = new QuestionLoader();
        battleQuestions = await loader.LoadQuestionsAsync(GameSettings.SelectedDeckUid);
        totalDeckCardsCount = battleQuestions.Count;

        foreach (var q in battleQuestions)
        {
            q.CorrectProgress = 0;
        }

        double maxBossHP = 100.0;
        if (GameSettings.IsZenMode || GameSettings.CurrentDifficulty == "Zen")
        {
            maxBossHP = -1;
        }

        bossHP = maxBossHP;

        // Initialize Boss reaction properties
        boss = new Boss
        {
            Name = "P e n g u",
            IdleImage = "pengu_idle.png",
            HurtImage = "pengu_hurt.png",
            AttackImage = "pengu_attack_peck.png",
            DefeatedImage = "pengu_hurt.png",
            MaxHp = 100,
            Hp = 100
        };
        BossImage.Source = boss.IdleImage;

        int totalHitsNeeded = battleQuestions.Count * GameSettings.CorrectAnswersRequired;
        if (totalHitsNeeded <= 0) totalHitsNeeded = 1;
        damagePerCorrectAnswer = maxBossHP / totalHitsNeeded;

        playerLives = GameSettings.PlayerLives;
        startingLives = GameSettings.PlayerLives;

        ResetScoreMetrics();

        int baseTimeLimit = GameSettings.TimeLimitSeconds == -1 ? 15 : GameSettings.TimeLimitSeconds;
        totalMaxTimeAllowed = totalHitsNeeded * baseTimeLimit;

        quizTimer.Reset();
        quizTimer.Start();

        heartSizeLocked = false;
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

    private void ResetScoreMetrics()
    {
        baseQuestionsScore = 0;
        totalStreakBonusEarned = 0;
        totalPenaltiesDeducted = 0;
        currentStreak = 0;
    }

    private async Task HandleDefeat(string title, string message)
    {
        AnswerEntry.IsEnabled = false;
        foreach (Button button in optionButtons)
        {
            button.IsEnabled = false;
        }
        battleTimer?.Stop();
        quizTimer.Stop();

        bool retry = await DisplayAlert(title, message + "\nYour points have been cleared.", "RETRY", "LEAVE");
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

    private async Task HandleVictory()
    {
        AnswerEntry.IsEnabled = false;
        foreach (Button button in optionButtons)
        {
            button.IsEnabled = false;
        }
        battleTimer?.Stop();

        int subtotal = (baseQuestionsScore + totalStreakBonusEarned) - totalPenaltiesDeducted;
        if (subtotal < 0) subtotal = 0;

        bool retry = await DisplayAlert("VICTORY!", $"FINAL SCORE: {subtotal}\n\nPLAY AGAIN?", "RETRY", "MAIN MENU");
        if (retry)
        {
            await StartBattle();
        }
        else
        {
            await Shell.Current.GoToAsync("//MainMenu");
        }
    }

    private async Task HandleNonHardVictoryAsync()
    {
        AnswerEntry.IsEnabled = false;
        foreach (Button button in optionButtons)
        {
            button.IsEnabled = false;
        }
        battleTimer?.Stop();

        bool retry = await DisplayAlert(
            "YOU WIN!",
            "YOU CLEARED THE DECK!\n\nScores and Leaderboard submissions are only tracked on HARD difficulty. Step up to Hard mode to test your true skills!\n\nPLAY AGAIN?",
            "RETRY",
            "MAIN MENU"
        );

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
        if (GameSettings.IsZenMode || bossHP < 0)
        {
            BossLabel.Text = "BOSS HP:          ";
        }
        else
        {
            BossLabel.Text = $"BOSS HP: {Math.Ceiling(bossHP)}";
        }

        if (GameSettings.IsZenMode)
        {
            LivesLabel.Text = "LIVES:              ";
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
                Text = new string(' ', Math.Max(0, playerLives)),
                FontSize = Math.Min(24, size),
                TextColor = Colors.Red,
                VerticalOptions = LayoutOptions.Center
            });
        }
    }

    private void UpdateMasteryLabel()
    {
        CardsCountLabel.Text = $"Cards Left: {battleQuestions.Count}/{totalDeckCardsCount}";

        if (currentQuestion == null) return;

        int currentProgress = currentQuestion.CorrectProgress;
        int totalNeeded = GameSettings.CorrectAnswersRequired;
        MasteryLabel.Text = $"Card Mastery: {currentProgress}/{totalNeeded}";
    }

    private void UpdateTimerLabel()
    {
        if (GameSettings.TimeLimitSeconds == -1 || GameSettings.IsZenMode)
        {
            TimeLabel.Text = "TIME:              ";
            return;
        }
        TimeSpan time = TimeSpan.FromSeconds(timeRemaining);
        TimeLabel.Text = $"TIME: {time:mm\\:ss}";
    }

    private void NextQuestion()
    {
        if (battleQuestions.Count == 0 || (!GameSettings.IsZenMode && bossHP <= 0))
        {
            QuestionLabel.Text = "YOU WIN!";
            AnswerEntry.IsEnabled = false;
            battleTimer?.Stop();
            quizTimer.Stop();

            if (GameSettings.CurrentDifficulty == "Hard")
            {
                int preBonusTotal = (baseQuestionsScore + totalStreakBonusEarned) - totalPenaltiesDeducted;
                if (preBonusTotal < 0) preBonusTotal = 0;

                double totalSecondsSpent = quizTimer.Elapsed.TotalSeconds;
                if (totalSecondsSpent > totalMaxTimeAllowed) totalSecondsSpent = totalMaxTimeAllowed;

                double secondsSaved = totalMaxTimeAllowed - totalSecondsSpent;
                double timeSavedRatio = totalMaxTimeAllowed > 0 ? (secondsSaved / totalMaxTimeAllowed) : 0;

                int bonusPoints = (int)Math.Round(preBonusTotal * timeSavedRatio);
                baseQuestionsScore += bonusPoints;

                _ = DisplayScoreBreakdownAsync(preBonusTotal, totalSecondsSpent, secondsSaved, timeSavedRatio, bonusPoints);
            }
            else
            {
                _ = HandleNonHardVictoryAsync();
            }
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

    private async Task DisplayScoreBreakdownAsync(int performanceSubtotal, double timeSpent, double timeSaved, double ratio, int bonus)
    {
        int finalScore = performanceSubtotal + bonus;
        string breakdownMessage =
            $"  Base Score: {baseQuestionsScore - bonus} pts (1 pt per hit)\n" +
            $"  Streak Bonus: +{totalStreakBonusEarned} pts (consecutive items)\n" +
            $"  Mistakes Penalty: -{totalPenaltiesDeducted} pts (1 pt per slip)\n" +
            $"-------------------------------\n" +
            $"Performance Subtotal: {performanceSubtotal} pts\n\n" +
            $"  Time Bonus:\n" +
            $"  Saved {Math.Ceiling(timeSaved)} / {totalMaxTimeAllowed} seconds!\n" +
            $"  Speed Bonus Percentage: +{(ratio * 100):F0}%\n" +
            $"  Bonus Awarded: +{bonus} pts\n\n" +
            $"===============================\n" +
            $"GRAND TOTAL SCORE: {finalScore} pts!";

        await DisplayAlert("COMBAT SCORE BREAKDOWN", breakdownMessage, "VIEW RATING");

        try
        {
            DatabaseService db = new DatabaseService();
            var currentDeck = await db.GetDeckByUidAsync(GameSettings.SelectedDeckUid);
            if (currentDeck != null)
            {
                await db.SaveDeckMasteryAsync(currentDeck.Id, finalScore);

                if (QuizBattle.Helpers.SessionManager.IsLoggedIn())
                {
                    var user = QuizBattle.Helpers.SessionManager.GetUser();
                    FirestoreService firestore = new FirestoreService();
                    await firestore.SubmitLeaderboardScore(user.LocalId, user.DisplayName, currentDeck.Uid, finalScore, user.IdToken);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save score: {ex.Message}");
        }

        baseQuestionsScore = finalScore;
        totalStreakBonusEarned = 0;
        totalPenaltiesDeducted = 0;

        await HandleVictory();
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
            baseQuestionsScore += 1;

            if (currentStreak > 0)
            {
                totalStreakBonusEarned += currentStreak;
            }

            currentStreak++;
            currentQuestion.TimesCorrect++;
            currentQuestion.CorrectProgress++;

            if (!GameSettings.IsZenMode && bossHP > 0)
            {
                bossHP -= damagePerCorrectAnswer;
                await BossTakeDamage((int)Math.Ceiling(damagePerCorrectAnswer));
            }

            if (currentQuestion.CorrectProgress >= GameSettings.CorrectAnswersRequired)
            {
                battleQuestions.Remove(currentQuestion);
            }

            UpdateMasteryLabel();
            UpdateLabels();

            if (battleQuestions.Count == 0 && !GameSettings.IsZenMode)
            {
                bossHP = 0;
                UpdateLabels();
            }
        }
        else
        {
            ResultLabel.Text = "INCORRECT!";
            ResultLabel.TextColor = Colors.Red;
            currentStreak = 0;
            totalPenaltiesDeducted += 1;
            currentQuestion.TimesIncorrect++;

            if (!GameSettings.IsZenMode)
            {
                playerLives--;
                await BossAttack();
            }

            UpdateMasteryLabel();
            UpdateLabels();
        }

        if (!GameSettings.IsZenMode && playerLives <= 0)
        {
            quizTimer.Stop();
            ResetScoreMetrics();
            await HandleDefeat("GAME OVER!", "YOU RAN OUT OF LIVES.");
            return;
        }

        if (!GameSettings.IsZenMode && bossHP <= 0)
        {
            await Task.Delay(1000);
            NextQuestion();
            return;
        }

        await Task.Delay(1000);
        NextQuestion();
    }

    // --- BOSS REACTION ANIMATION METHODS ---
    private async Task BossTakeDamage(int damage)
    {
        boss.Hp -= damage;
        BossImage.Source = boss.HurtImage;
        await ShakeBoss();
        await ShowDamage(damage);
        await Task.Delay(250);

        if (boss.Hp <= 0)
            BossImage.Source = boss.DefeatedImage;
        else
            BossImage.Source = boss.IdleImage;
    }

    private async Task BossAttack()
    {
        BossImage.Source = boss.AttackImage;
        await BossImage.ScaleTo(1.1, 100);
        await BossImage.ScaleTo(1.0, 100);
        await Task.Delay(250);
        BossImage.Source = boss.IdleImage;
    }

    private async Task ShakeBoss()
    {
        for (int i = 0; i < 5; i++)
        {
            await BossImage.TranslateTo(-8, 0, 20);
            await BossImage.TranslateTo(8, 0, 20);
        }
        await BossImage.TranslateTo(0, 0, 20);
    }

    private async Task ShowDamage(int damage)
    {
        DamageLabel.Text = "-" + damage;
        DamageLabel.IsVisible = true;
        DamageLabel.TranslationY = 0;
        DamageLabel.Opacity = 1;
        await Task.WhenAll(
            DamageLabel.TranslateTo(0, -60, 500),
            DamageLabel.FadeTo(0, 500)
        );
        DamageLabel.IsVisible = false;
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
                quizTimer.Stop();
                await Shell.Current.GoToAsync("//MainMenu");
            }
        });
        return true;
    }
}