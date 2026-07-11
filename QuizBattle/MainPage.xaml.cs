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
    private int playerLives = 5;
    private const int CorrectAnswersRequired = 1;
    private HashSet<string> selectedOptions = new HashSet<string>();    
    private Button[] optionButtons = Array.Empty<Button>();
    private readonly Color defaultOptionColor = Colors.LightGray;
    private readonly Color selectedOptionColor = Colors.Green;

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

        QuestionLoader loader = new QuestionLoader();

        battleQuestions = await loader.LoadQuestionsAsync("Data/QuestionList.txt");

        bossHP = battleQuestions.Count;

        UpdateLabels();

        NextQuestion();
    }

    private void UpdateLabels()
    {
        BossLabel.Text = $"Boss HP: {bossHP}";
        LivesLabel.Text = $"Lives: {playerLives}";
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

            bossHP--;

            if (currentQuestion.CorrectProgress >= CorrectAnswersRequired)
            {
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
            await DisplayAlert("Game Over", "You lost!", "OK");

            AnswerEntry.IsEnabled = false;

            return;
        }

        if (bossHP <= 0)
        {
            await DisplayAlert("Victory", "You defeated the boss!", "OK");

            AnswerEntry.IsEnabled = false;

            return;
        }

        NextQuestion();
    }
}