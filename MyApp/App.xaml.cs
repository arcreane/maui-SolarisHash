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
                // ✅ SÉCURITÉ: Gestionnaire d'exceptions global
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                
                MainPage = new AppShell();
                
                // Log de démarrage réussi
                System.Diagnostics.Debug.WriteLine("✅ TravelBuddy: Application démarrée avec succès");
            }
            catch (Exception ex)
            {
                // Log de l'erreur
                System.Diagnostics.Debug.WriteLine($"❌ TravelBuddy: Erreur démarrage - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                
                // ✅ SÉCURITÉ: Page d'erreur robuste au lieu de crash
                MainPage = CreateErrorPage(ex);
            }
        }

        // ✅ NOUVEAU: Gestionnaire d'exceptions global
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"💥 Exception non gérée: {exception?.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 StackTrace: {exception?.StackTrace}");
                
                // Essayer de récupérer gracieusement
                if (!e.IsTerminating)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainPage = CreateErrorPage(exception);
                    });
                }
            }
            catch
            {
                // Ne rien faire si même le gestionnaire d'erreur crash
            }
        }

        // ✅ NOUVEAU: Gestionnaire pour les tâches non observées
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Tâche non observée: {e.Exception.Message}");
                e.SetObserved(); // Marquer comme observée pour éviter le crash
            }
            catch
            {
                // Ne rien faire si même le gestionnaire d'erreur crash
            }
        }

        // ✅ NOUVEAU: Créer une page d'erreur robuste
        private ContentPage CreateErrorPage(Exception? ex)
        {
            try
            {
                return new ContentPage
                {
                    Title = "TravelBuddy - Erreur",
                    Content = new ScrollView
                    {
                        Content = new StackLayout
                        {
                            Padding = 20,
                            Spacing = 20,
                            Children =
                            {
                                new Label 
                                { 
                                    Text = "🚨 TravelBuddy - Erreur",
                                    FontSize = 24,
                                    FontAttributes = FontAttributes.Bold,
                                    HorizontalOptions = LayoutOptions.Center,
                                    TextColor = Colors.Red
                                },
                                new Label 
                                { 
                                    Text = "L'application a rencontré un problème et va redémarrer.",
                                    FontSize = 16,
                                    HorizontalOptions = LayoutOptions.Center,
                                    HorizontalTextAlignment = TextAlignment.Center
                                },
                                new Button
                                {
                                    Text = "🔄 Redémarrer l'application",
                                    BackgroundColor = Colors.Blue,
                                    TextColor = Colors.White,
                                    Command = new Command(() =>
                                    {
                                        try
                                        {
                                            MainPage = new AppShell();
                                        }
                                        catch
                                        {
                                            System.Diagnostics.Debug.WriteLine("❌ Impossible de redémarrer");
                                        }
                                    })
                                },
                                new Frame
                                {
                                    BackgroundColor = Colors.LightGray,
                                    Padding = 10,
                                    IsVisible = ex != null,
                                    Content = new StackLayout
                                    {
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = "Détails de l'erreur:",
                                                FontAttributes = FontAttributes.Bold
                                            },
                                            new Label
                                            {
                                                Text = ex?.Message ?? "Erreur inconnue",
                                                FontSize = 12
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }
            catch
            {
                // Si même la page d'erreur crash, retourner une page ultra-simple
                return new ContentPage
                {
                    Content = new Label
                    {
                        Text = "❌ Erreur critique - Redémarrez l'application",
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    }
                };
            }
        }

        // ✅ SÉCURITÉ: Méthodes du cycle de vie avec protection
        protected override void OnStart()
        {
            try
            {
                base.OnStart();
                System.Diagnostics.Debug.WriteLine("🚀 TravelBuddy: OnStart");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur OnStart: {ex.Message}");
            }
        }

        protected override void OnSleep()
        {
            try
            {
                base.OnSleep();
                System.Diagnostics.Debug.WriteLine("😴 TravelBuddy: OnSleep");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur OnSleep: {ex.Message}");
            }
        }

        protected override void OnResume()
        {
            try
            {
                base.OnResume();
                System.Diagnostics.Debug.WriteLine("👋 TravelBuddy: OnResume");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur OnResume: {ex.Message}");
            }
        }
    }
}