using System.Text;
using QuizBattle.Models;
using QuizBattle.Services;

namespace QuizBattle;

public partial class SourceMaterial : ContentPage
{
    private readonly AIService aiService = new AIService();

    public SourceMaterial()
    {
        InitializeComponent();
    }

    private async void StartQuizClicked(object sender, EventArgs e)
    {
        var button = sender as Button;

        if (string.IsNullOrWhiteSpace(MaterialEditor.Text))
        {
            await DisplayAlert("No Learning Material", "Please enter your learning material to generate quiz questions.", "OK");
            return;
        }

        int maxQuestions = 20;

        if (!string.IsNullOrWhiteSpace(QuestionLimitEntry.Text))
        {
            if (!int.TryParse(QuestionLimitEntry.Text, out maxQuestions) || maxQuestions < 10 || maxQuestions > 50)
            {
                await DisplayAlert("Invalid Number of Questions", "The number of questions must be between 10 and 50.", "OK");
                return;
            }
        }

        // disable button while api request is running
        if (button != null)
        {
            button.IsEnabled = false;
            button.Text = "Generating Questions...";
        }

        try
        {
            GameSettings.MaxQuestions = maxQuestions;

            // call api service
            string formattedQuestions = await aiService.GenerateQuestionsAsync(MaterialEditor.Text, maxQuestions);

            if (string.IsNullOrWhiteSpace(formattedQuestions))
            {
                await DisplayAlert("Error", "AI failed to generate questions. Please try again.", "OK");
                return;
            }

            string filePath = Path.Combine(FileSystem.Current.AppDataDirectory, "QuestionList.txt");

            // write piped string output to questionlist.txt
            File.WriteAllText(filePath, formattedQuestions, Encoding.UTF8);

            await Navigation.PushAsync(new MainPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert("API Error", ex.Message, "OK");
        }
        finally
        {
            // re-enable button
            if (button != null)
            {
                button.IsEnabled = true;
                button.Text = "Start Quiz";
            }
        }
    }
}