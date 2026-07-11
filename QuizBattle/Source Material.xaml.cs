using System.Text;

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