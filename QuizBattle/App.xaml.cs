using QuizBattle.Helpers;

namespace QuizBattle
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Page initialPage;

            // Check if the user has a valid login token saved
            if (SessionManager.IsLoggedIn())
            {
                initialPage = new AppShell();
            }
            else
            {
                initialPage = new NavigationPage(new LoginPage());
            }

            return new Window(initialPage);
        }
    }
}