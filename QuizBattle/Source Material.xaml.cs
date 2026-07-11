using System.Text;
using QuizBattle.Models;

namespace QuizBattle;

public partial class SourceMaterial : ContentPage
{
    public SourceMaterial()
    {
        InitializeComponent();
    }

    private async void StartQuizClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MaterialEditor.Text))
        {
            await DisplayAlert(
                "No Learning Material",
                "Please enter your learning material to generate quiz questions.",
                "OK");

            return;
        }

        int maxQuestions = 20; //default number of questions

        if (!string.IsNullOrWhiteSpace(QuestionLimitEntry.Text))
        {
            if (!int.TryParse(QuestionLimitEntry.Text, out maxQuestions) ||
                maxQuestions < 10 ||
                maxQuestions > 50)
            {
                await DisplayAlert(
                    "Invalid Number of Questions",
                    "The number of questions must be between 10 and 50.",
                    "OK");

                return;
            }
        }

        GameSettings.MaxQuestions = maxQuestions;

        string filePath = Path.Combine(
                FileSystem.Current.AppDataDirectory,
                "QuestionList.txt");

        File.WriteAllText(
            filePath,
            MaterialEditor.Text,
            Encoding.UTF8);

        await Navigation.PushAsync(new MainPage());
    }
}