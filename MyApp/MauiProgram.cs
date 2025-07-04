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
                // .UseMauiMaps() // TEMPORAIREMENT DÉSACTIVÉ pour éviter les erreurs Google Maps
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services HTTP et API
            builder.Services.AddSingleton<HttpClient>(serviceProvider =>
            {
                var httpClient = new HttpClient();
                
                // Configuration optimisée pour les vraies données
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "TravelBuddy/1.0 (Real Data App)");
                
                return httpClient;
            });
            
            // ⭐ CHANGEMENT PRINCIPAL: Service avec VRAIES données uniquement
            builder.Services.AddSingleton<IPlaceService, RealDataOnlyService>();
            
            // Alternative si vous voulez garder l'hybride mais sans données fictives:
            // builder.Services.AddSingleton<IPlaceService, OverpassService>();
            
            // Services de localisation et capteurs (ordre important)
            builder.Services.AddSingleton<ILocationService, SmartLocationService>();
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
            
            // Log pour confirmer la configuration
            Console.WriteLine("🌍 TravelBuddy configuré avec VRAIES DONNÉES UNIQUEMENT (OpenStreetMap)");
            Console.WriteLine("📍 Aucune donnée fictive ou de démonstration ne sera utilisée");
#endif

            return builder.Build();
        }
    }
}