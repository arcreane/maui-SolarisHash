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

            // Services HTTP et API
            builder.Services.AddSingleton<HttpClient>();
            
            // Services de lieux - Service hybride (API réelle + fallback)
            builder.Services.AddSingleton<IPlaceService, HybridPlaceService>();
            
            // Services de localisation
            builder.Services.AddSingleton<ILocationService, SmartLocationService>();
            builder.Services.AddSingleton<ICompassService, CompassService>();
            
            // ViewModels et Pages
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}