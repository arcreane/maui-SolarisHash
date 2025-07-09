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

            // ✅ CORRECTION: Ordre d'enregistrement des services
            try
            {
                // Services de base
                builder.Services.AddSingleton<IPlaceService, RobustHttpService>();
                builder.Services.AddSingleton<ILocationService, SamsungLocationService>();
                builder.Services.AddSingleton<ISamsungSensorService, SamsungSensorService>();
                
                // ✅ NOUVEAU: Service cardinal AVANT OrientationService
                builder.Services.AddSingleton<ICardinalDirectionService, CardinalDirectionService>();
                
                // Service d'orientation
                builder.Services.AddSingleton<IOrientationService, OrientationService>();

                // ViewModels
                builder.Services.AddTransient<MainPageViewModel>();

                // Pages
                builder.Services.AddTransient<SplashPage>();
                builder.Services.AddTransient<MapPage>();
                builder.Services.AddTransient<MainPage>();

                Console.WriteLine("✅ Tous les services enregistrés avec succès");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur enregistrement services: {ex.Message}");
            }

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            Console.WriteLine("🚀 TravelBuddy avec Boussole + Disposition Cardinale");
#endif

            return builder.Build();
        }
    }
}