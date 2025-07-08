using Microsoft.Extensions.Logging;
using MyApp.Services;
using MyApp.ViewModels;

namespace MyApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<IPlaceService, RobustHttpService>();
            builder.Services.AddSingleton<ILocationService, SamsungLocationService>();
            builder.Services.AddSingleton<ISamsungSensorService, SamsungSensorService>();
            builder.Services.AddSingleton<IOrientationService>(serviceProvider =>
            {
                return new OrientationService();
            });

            // ViewModels
            builder.Services.AddTransient<MainPageViewModel>();

            // Pages
            builder.Services.AddTransient<SplashPage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<MainPage>(); // Garde pour compatibilité

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            Console.WriteLine("🚀 TravelBuddy avec Splash + Carte Full-Screen");
#endif

            return builder.Build();
        }
    }
}