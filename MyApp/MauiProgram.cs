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
            builder.Services.AddSingleton<HttpClient>();
            
            // Services de lieux - Service hybride (API réelle + fallback)
            builder.Services.AddSingleton<IPlaceService, HybridPlaceService>();
            
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
#endif

            return builder.Build();
        }
    }
}