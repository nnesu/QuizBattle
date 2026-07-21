using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using QuizBattle.Services;

namespace QuizBattle
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .AddAudio() // Native audio plugin initialization
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register MAUI Audio Manager and Audio Service
            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton<AudioService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}