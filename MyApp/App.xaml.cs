using Microsoft.Extensions.Logging;

namespace MyApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            try
            {
                MainPage = new AppShell();
                
                // Log de démarrage réussi
                System.Diagnostics.Debug.WriteLine("✅ TravelBuddy: Application démarrée avec succès");
            }
            catch (Exception ex)
            {
                // Log de l'erreur
                System.Diagnostics.Debug.WriteLine($"❌ TravelBuddy: Erreur démarrage - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                
                // Fallback vers une page d'erreur simple
                MainPage = new ContentPage
                {
                    Content = new StackLayout
                    {
                        Children =
                        {
                            new Label 
                            { 
                                Text = "❌ Erreur de démarrage", 
                                FontSize = 18, 
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center
                            },
                            new Label 
                            { 
                                Text = ex.Message, 
                                FontSize = 12, 
                                HorizontalOptions = LayoutOptions.Center
                            }
                        },
                        VerticalOptions = LayoutOptions.Center,
                        Padding = 20
                    }
                };
            }
        }
    }
}