using Microsoft.Extensions.Logging;
using MyApp.Services;
using MyApp.ViewModels;
using Microsoft.Maui.Maps;

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

            // ⭐ SERVICE PRINCIPAL: Robuste pour téléphone ET émulateur
            builder.Services.AddSingleton<IPlaceService, RobustHttpService>();

            // 📱 SERVICE SAMSUNG: Géolocalisation optimisée pour Samsung
            builder.Services.AddSingleton<ILocationService, SamsungLocationService>();

            // 🧭 SERVICE CAPTEURS SAMSUNG: Boussole et diagnostic
            builder.Services.AddSingleton<ISamsungSensorService, SamsungSensorService>();

            // Services capteurs legacy (pour compatibilité)
            builder.Services.AddSingleton<ICompassService, CompassService>();

            // OrientationService avec injection du CompassService
            builder.Services.AddSingleton<IOrientationService>(serviceProvider =>
            {
                var compassService = serviceProvider.GetService<ICompassService>();
                return new OrientationService(compassService);
            });

            // ViewModels et Pages
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            Console.WriteLine("📱 TravelBuddy configuré pour SAMSUNG");
            Console.WriteLine("🛰️ Service GPS Samsung optimisé");
            Console.WriteLine("🗺️ Microsoft.Maui.Maps configuré"); // ✅ AJOUTÉ
            Console.WriteLine("🎨 Interface moderne avec vraies données");
#endif

            return builder.Build();
        }
    }
}