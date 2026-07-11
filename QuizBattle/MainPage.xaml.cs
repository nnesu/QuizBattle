using QuizBattle.Services;
using QuizBattle.Models;
using System.Linq; //Needed because we use Any()

namespace QuizBattle;

public partial class MainPage : ContentPage
{
    private readonly Random random = new();
    private List<Question> battleQuestions = new();
    private Question? currentQuestion;
    private int bossHP;

    private int playerLives;

    private HashSet<string> selectedOptions = new HashSet<string>();    
    private Button[] optionButtons = Array.Empty<Button>();
    private readonly Color defaultOptionColor = Colors.LightGray;
    private readonly Color selectedOptionColor = Colors.Green;

    private IDispatcherTimer? battleTimer;
    private int timeRemaining;

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

        await HandleDefeat(
            "Time's Up!",
            "You ran out of time.");
    }

    private async Task StartBattle()
    {
        AnswerEntry.IsEnabled = true;

        foreach (Button button in optionButtons)
        {
            button.IsEnabled = true;
        }

        QuestionLoader loader = new QuestionLoader();

        battleQuestions = await loader.LoadQuestionsAsync("QuestionList.txt");

        bossHP = battleQuestions.Count;

        playerLives = GameSettings.PlayerLives;

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
        }

        timeRemaining = GameSettings.TimeLimitSeconds;

        UpdateTimerLabel();

        if (GameSettings.TimeLimitSeconds != -1)
        {
            battleTimer = Dispatcher.CreateTimer();

            battleTimer.Interval = TimeSpan.FromSeconds(1);

            battleTimer.Tick += BattleTimerTick;

            battleTimer.Start();
        }

        NextQuestion();
    }

    private async Task HandleDefeat(string title, string message)
    {
        AnswerEntry.IsEnabled = false;

        foreach (Button button in optionButtons)
        {
            button.IsEnabled = false;
        }

        battleTimer?.Stop();

        bool retry = await DisplayAlert(
            title,
            message,
            "Retry",
            "Leave");

        if (retry)
        {
            await StartBattle();
        }
        else
        {
            bool confirm = await DisplayAlert(
                "Admit defeat for now?",
                "",
                "Yes",
                "No");

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

        bool retry = await DisplayAlert(
            "Victory!",
            "Play again?",
            "Retry",
            "Main Menu");

        if (retry)
        {
            await StartBattle();
        }
        else
        {
            await Shell.Current.GoToAsync("//MainMenu");
        }
    }

    //This is for health indicators only
    private void UpdateLabels()
    {
        BossLabel.Text = $"Boss HP: {bossHP}";
        LivesLabel.Text = $"Lives: {playerLives}";
    }

    //Needed for medium and hard difficulty
    //where a question needs to be answered correctly more than once before it gets counted
    private void UpdateMasteryLabel()
    {
        if (currentQuestion == null)
        {
            MasteryLabel.Text = string.Empty;
            return;
        }

        MasteryLabel.Text =
            $"Question Mastery: {currentQuestion.CorrectProgress} / {GameSettings.CorrectAnswersRequired}";
    }

    private void UpdateTimerLabel()
    {
        if (GameSettings.TimeLimitSeconds == -1)
        {
            TimeLabel.Text = "Time: ∞";
            return;
        }

        TimeSpan time = TimeSpan.FromSeconds(timeRemaining);

        TimeLabel.Text = $"Time: {time:mm\\:ss}";
    }

    private void NextQuestion()
    {
        if (battleQuestions.Count == 0)
        {
            QuestionLabel.Text = "You Win!";
            AnswerEntry.IsEnabled = false;
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

        // Reset the UI
        AnswerEntry.IsVisible = false;
        MultipleChoiceLayout.IsVisible = false;

        AnswerEntry.Text = string.Empty;
        selectedOptions.Clear();

        foreach (Button button in optionButtons)
        {
            button.BackgroundColor = defaultOptionColor ;
        }

        ResultLabel.Text = string.Empty;

        QuestionLabel.Text = currentQuestion.Text;

        UpdateMasteryLabel();

        // Display the appropriate input controls
        if (currentQuestion.Type == QuestionType.Identification)
        {
            AnswerEntry.IsVisible = true;
            AnswerEntry.Focus();
        }
        else if (currentQuestion.Type == QuestionType.MultipleChoice)
        {
            MultipleChoiceLayout.IsVisible = true;

            Button[] buttons =
            {
            OptionButton1,
            OptionButton2,
            OptionButton3,
            OptionButton4
        };

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
                ResultLabel.Text = "Please enter an answer.";
                return;
            }

            isCorrect = currentQuestion.CorrectAnswers.Any(answer =>
                answer.Equals(playerAnswer, StringComparison.OrdinalIgnoreCase));
        }
        else if (currentQuestion.Type == QuestionType.MultipleChoice)
        {
            if (selectedOptions.Count == 0)
            {
                ResultLabel.Text = "Please select at least one answer.";
                return;
            }

            HashSet<string> correctAnswers = new HashSet<string>(
                currentQuestion.CorrectAnswers,
                StringComparer.OrdinalIgnoreCase);

            isCorrect = selectedOptions.SetEquals(correctAnswers);
        }

        if (isCorrect)
        {
            ResultLabel.Text = "Correct!";

            currentQuestion.TimesCorrect++;
            currentQuestion.CorrectProgress++;

            UpdateMasteryLabel();

            if (currentQuestion.CorrectProgress >= GameSettings.CorrectAnswersRequired)
            {
                bossHP--;
                battleQuestions.Remove(currentQuestion);
            }
        }
        else
        {
            ResultLabel.Text = "Incorrect!";

            currentQuestion.TimesIncorrect++;

            playerLives--;
        }

        UpdateLabels();

        if (playerLives <= 0)
        {
            await HandleDefeat(
                "Game Over!",
                "You ran out of lives.");

            return;
        }

        if (bossHP <= 0)
        {
            await HandleVictory();
            return;
        }

        await Task.Delay(1000);
        NextQuestion();
    }


    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool confirm = await DisplayAlert(
                "Battle in Progress!",
                "Confirm return to menu?",
                "Yes",
                "No");

            if (confirm)
            {
                await Shell.Current.GoToAsync("//MainMenu");
            }
        });

        return true;
    }


}