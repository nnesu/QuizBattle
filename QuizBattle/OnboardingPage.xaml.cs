namespace QuizBattle;
public partial class OnboardingPage : ContentPage
{
    private int _currentStep = 1;
    private readonly int _totalSteps = 6;
    public OnboardingPage()
    {
        InitializeComponent();
    }
    private async void OnNextClicked(object sender, EventArgs e)
    {
        if (_currentStep < _totalSteps)
        {
            _currentStep++;
            UpdateStep();
        }
        else
        {
            // Last step - go to main menu
            await Navigation.PopAsync();
        }
    }
    private async void OnSkipClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    private void UpdateStep()
    {
        // Hide all steps
        Step1.IsVisible = false;
        Step2.IsVisible = false;
        Step3.IsVisible = false;
        Step4.IsVisible = false;
        Step5.IsVisible = false;
        Step6.IsVisible = false;
        // Show current step
        switch (_currentStep)
        {
            case 1:
                Step1.IsVisible = true;
                NextButton.Text = "NEXT";
                break;
            case 2:
                Step2.IsVisible = true;
                NextButton.Text = "NEXT";
                break;
            case 3:
                Step3.IsVisible = true;
                NextButton.Text = "NEXT";
                break;
            case 4:
                Step4.IsVisible = true;
                NextButton.Text = "NEXT";
                break;
            case 5:
                Step5.IsVisible = true;
                NextButton.Text = "NEXT";
                break;
            case 6:
                Step6.IsVisible = true;
                NextButton.Text = "FINISH";
                break;
        }
        // Update step indicator
        StepIndicatorLabel.Text = $"STEP {_currentStep} OF {_totalSteps}";
        // Update progress bars
        Progress1.BackgroundColor = _currentStep >= 1 ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#31405F");
        Progress2.BackgroundColor = _currentStep >= 2 ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#31405F");
        Progress3.BackgroundColor = _currentStep >= 3 ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#31405F");
        Progress4.BackgroundColor = _currentStep >= 4 ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#31405F");
        Progress5.BackgroundColor = _currentStep >= 5 ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#31405F");
        Progress6.BackgroundColor = _currentStep >= 6 ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#31405F");
    }
}