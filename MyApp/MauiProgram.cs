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

            // Enregistrement du HttpClient et du service Overpass
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<IPlaceService, OverpassService>();
            
            // Enregistrement des ViewModels
            builder.Services.AddTransient<MainPageViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}