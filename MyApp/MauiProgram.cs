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

            // Enregistrement des services
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<IPlaceService, OverpassService>();
            builder.Services.AddSingleton<ICompassService, CompassService>();
            
            // Enregistrement des ViewModels et Pages
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}