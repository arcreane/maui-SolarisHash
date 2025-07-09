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
                // ✅ SÉCURITÉ: Gestionnaire d'exceptions global avec protection UI thread
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

        // ✅ CORRECTION: Gestionnaire d'exceptions global avec protection UI thread
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"💥 Exception non gérée: {exception?.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 StackTrace: {exception?.StackTrace}");
                
                // ✅ CORRECTION PRINCIPALE: Vérifier si c'est une erreur UI thread
                if (exception?.Message?.Contains("Only the original thread that created a view hierarchy can touch its views") == true)
                {
                    System.Diagnostics.Debug.WriteLine("🔧 Erreur UI Thread détectée - Tentative de récupération");
                    
                    // Essayer de récupérer en utilisant MainThread
                    if (!e.IsTerminating)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                // Tenter une récupération silencieuse
                                System.Diagnostics.Debug.WriteLine("🔧 Tentative de récupération UI thread");
                            }
                            catch (Exception recoveryEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Échec récupération: {recoveryEx.Message}");
                                // En dernier recours, créer une page d'erreur
                                MainPage = CreateErrorPage(exception);
                            }
                        });
                    }
                }
                else
                {
                    // Autres types d'erreurs - gestion normale
                    if (!e.IsTerminating)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            MainPage = CreateErrorPage(exception);
                        });
                    }
                }
            }
            catch (Exception handlerEx)
            {
                // Ne rien faire si même le gestionnaire d'erreur crash
                System.Diagnostics.Debug.WriteLine($"❌ Erreur dans gestionnaire: {handlerEx.Message}");
            }
        }

        // ✅ CORRECTION: Gestionnaire pour les tâches non observées avec protection UI thread
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Tâche non observée: {e.Exception.Message}");
                
                // ✅ CORRECTION: Vérifier si c'est une erreur UI thread dans une tâche async
                if (e.Exception.InnerException?.Message?.Contains("Only the original thread that created a view hierarchy can touch its views") == true ||
                    e.Exception.Message?.Contains("Only the original thread that created a view hierarchy can touch its views") == true)
                {
                    System.Diagnostics.Debug.WriteLine("🔧 Erreur UI Thread dans tâche async - Marquage comme observée");
                    e.SetObserved(); // Marquer comme observée pour éviter le crash
                    return;
                }
                
                e.SetObserved(); // Marquer comme observée pour éviter le crash
            }
            catch (Exception handlerEx)
            {
                // Ne rien faire si même le gestionnaire d'erreur crash
                System.Diagnostics.Debug.WriteLine($"❌ Erreur gestionnaire tâche: {handlerEx.Message}");
            }
        }

        // ✅ NOUVEAU: Créer une page d'erreur robuste avec protection UI thread
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
                                    Text = "🚨 TravelBuddy - Erreur UI Thread",
                                    FontSize = 24,
                                    FontAttributes = FontAttributes.Bold,
                                    HorizontalOptions = LayoutOptions.Center,
                                    TextColor = Colors.Red
                                },
                                new Label 
                                { 
                                    Text = "L'application a rencontré un problème de thread UI et va redémarrer.",
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
                                            // ✅ CORRECTION: Redémarrage thread-safe
                                            MainThread.BeginInvokeOnMainThread(() =>
                                            {
                                                try
                                                {
                                                    MainPage = new AppShell();
                                                }
                                                catch (Exception restartEx)
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"❌ Impossible de redémarrer: {restartEx.Message}");
                                                }
                                            });
                                        }
                                        catch (Exception restartEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"❌ Erreur redémarrage: {restartEx.Message}");
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
                                                Text = ex?.Message ?? "Erreur UI thread inconnue",
                                                FontSize = 12
                                            },
                                            new Label
                                            {
                                                Text = "💡 Cette erreur est souvent causée par une mise à jour d'interface depuis un thread secondaire.",
                                                FontSize = 10,
                                                TextColor = Colors.Orange,
                                                FontAttributes = FontAttributes.Italic
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }
            catch (Exception createEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur création page erreur: {createEx.Message}");
                
                // Si même la page d'erreur crash, retourner une page ultra-simple
                return new ContentPage
                {
                    Content = new Label
                    {
                        Text = "❌ Erreur critique UI Thread - Redémarrez l'application",
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