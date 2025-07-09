using MyApp.Services;

namespace MyApp
{
    public partial class SplashPage : ContentPage
    {
        private readonly ILocationService _locationService;
        private bool _animationsRunning = true;

        public SplashPage(ILocationService locationService)
        {
            InitializeComponent();
            _locationService = locationService;
            
            // ✅ Ne pas appeler async void dans le constructeur
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            await StartMagicalExperience();
        }

        private async Task StartMagicalExperience()
        {
            try
            {
                // 1) initialisation des positions
                InitializeAnimationPositions();

                // 2) ✅ Démarrer les animations SANS CancellationToken mais thread-safe
                var particleTask = AnimateParticles();
                var dotsTask = AnimateLoadingDots();

                // 3) attendre l'animation d'entrée + l'initialisation des services
                await Task.WhenAll(
                    AnimateEntrance(),
                    InitializeServices()
                );

                // 4) ✅ Arrêter toutes les animations
                _animationsRunning = false;

                // 5) ✅ Attendre que les tâches se terminent
                try
                {
                    await Task.WhenAll(particleTask, dotsTask).WaitAsync(TimeSpan.FromSeconds(1));
                }
                catch (TimeoutException)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Timeout lors de l'arrêt des animations");
                }

                // 6) enfin, transition vers la MapPage
                await NavigateToMapWithStyle();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur StartMagicalExperience: {ex.Message}");
                _animationsRunning = false;
                await Shell.Current.GoToAsync("//MapPage");
            }
        }

        private void InitializeAnimationPositions()
        {
            try
            {
                // ✅ CORRECTION: S'assurer qu'on est sur le main thread pour les animations
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // Logo commence invisible et petit
                        if (LogoBorder != null)
                        {
                            LogoBorder.Scale = 0;
                            LogoBorder.Opacity = 0;
                        }
                        if (LogoIcon != null)
                        {
                            LogoIcon.Rotation = -180;
                        }

                        // Titre vient du côté
                        if (TitleLabel != null)
                        {
                            TitleLabel.TranslationX = -300;
                            TitleLabel.Opacity = 0;
                        }

                        // Sous-titre vient de l'autre côté
                        if (SubtitleLabel != null)
                        {
                            SubtitleLabel.TranslationX = 300;
                            SubtitleLabel.Opacity = 0;
                        }

                        // Loading stack invisible
                        if (LoadingStack != null)
                        {
                            LoadingStack.TranslationY = 50;
                            LoadingStack.Opacity = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Erreur init animations: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur InitializeAnimationPositions: {ex.Message}");
            }
        }

        private async Task AnimateParticles()
        {
            try
            {
                while (_animationsRunning)
                {
                    if (!_animationsRunning)
                        break;

                    // ✅ CORRECTION: Animations thread-safe
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (!_animationsRunning) return;

                        var tasks = new List<Task>();
                        if (Particle1 != null) tasks.Add(AnimateParticle(Particle1, 3000));
                        if (Particle2 != null) tasks.Add(AnimateParticle(Particle2, 4000));
                        if (Particle3 != null) tasks.Add(AnimateParticle(Particle3, 3500));
                        if (Particle4 != null) tasks.Add(AnimateParticle(Particle4, 4500));

                        if (tasks.Count > 0 && _animationsRunning)
                        {
                            await Task.WhenAll(tasks);
                        }
                    });
                    
                    if (_animationsRunning)
                    {
                        await Task.Delay(50);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur AnimateParticles: {ex.Message}");
            }
        }

        private async Task AnimateParticle(Microsoft.Maui.Controls.Shapes.Ellipse particle, uint duration)
        {
            try
            {
                if (!_animationsRunning || particle == null)
                    return;

                var actualDuration = _animationsRunning ? Math.Min(duration, 1000u) : 0u;

                await Task.WhenAll(
                    particle.TranslateTo(
                        Random.Shared.Next(-50, 50), 
                        Random.Shared.Next(-30, 30), 
                        actualDuration, 
                        Easing.SinInOut),
                    particle.FadeTo(Random.Shared.NextDouble() * 0.5 + 0.3, actualDuration)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur AnimateParticle: {ex.Message}");
            }
        }

        private async Task AnimateEntrance()
        {
            try
            {
                // ✅ CORRECTION: Animations thread-safe
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // 1. Logo apparaît avec style
                    var logoTasks = new List<Task>();
                    if (LogoBorder != null)
                    {
                        logoTasks.Add(LogoBorder.ScaleTo(1, 1000, Easing.BounceOut));
                        logoTasks.Add(LogoBorder.FadeTo(1, 800));
                    }
                    if (LogoIcon != null)
                    {
                        logoTasks.Add(LogoIcon.RotateTo(0, 1000, Easing.BounceOut));
                    }
                    if (logoTasks.Count > 0)
                        await Task.WhenAll(logoTasks);

                    // 2. Titre glisse en place
                    var titleTasks = new List<Task>();
                    if (TitleLabel != null)
                    {
                        titleTasks.Add(TitleLabel.TranslateTo(0, 0, 600, Easing.CubicOut));
                        titleTasks.Add(TitleLabel.FadeTo(1, 600));
                    }
                    if (titleTasks.Count > 0)
                        await Task.WhenAll(titleTasks);

                    // 3. Sous-titre suit
                    var subtitleTasks = new List<Task>();
                    if (SubtitleLabel != null)
                    {
                        subtitleTasks.Add(SubtitleLabel.TranslateTo(0, 0, 600, Easing.CubicOut));
                        subtitleTasks.Add(SubtitleLabel.FadeTo(1, 600));
                    }
                    if (subtitleTasks.Count > 0)
                        await Task.WhenAll(subtitleTasks);

                    // 4. Loading apparaît
                    var loadingTasks = new List<Task>();
                    if (LoadingStack != null)
                    {
                        loadingTasks.Add(LoadingStack.TranslateTo(0, 0, 400, Easing.CubicOut));
                        loadingTasks.Add(LoadingStack.FadeTo(1, 400));
                    }
                    if (loadingTasks.Count > 0)
                        await Task.WhenAll(loadingTasks);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur AnimateEntrance: {ex.Message}");
            }
        }

        private async Task AnimateLoadingDots()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var dots = new[] { Dot1, Dot2, Dot3, Dot4 }.Where(d => d != null).ToArray();
                    
                    while (_animationsRunning)
                    {
                        if (!_animationsRunning)
                            break;

                        for (int i = 0; i < dots.Length; i++)
                        {
                            if (!_animationsRunning)
                                break;

                            var dot = dots[i];
                            if (dot == null) continue;
                            
                            await Task.WhenAll(
                                dot.FadeTo(1, 150),
                                dot.ScaleTo(1.3, 150)
                            );
                            
                            if (!_animationsRunning)
                                break;
                            
                            await Task.WhenAll(
                                dot.FadeTo(0.4, 150),
                                dot.ScaleTo(1, 150)
                            );

                            if (_animationsRunning)
                            {
                                await Task.Delay(100);
                            }
                        }
                        
                        if (_animationsRunning)
                        {
                            await Task.Delay(300);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur AnimateLoadingDots: {ex.Message}");
            }
        }

        private async Task InitializeServices()
        {
            try
            {
                await UpdateStatus("🔮 Réveil des services magiques...");
                await Task.Delay(800);

                await UpdateStatus("🌟 Calibrage des capteurs...");
                await Task.Delay(600);

                await UpdateStatus("📡 Connexion aux satellites...");
                
                // ✅ CORRECTION: Background task thread-safe pour les services de localisation
                _ = Task.Run(async () => 
                {
                    try
                    {
                        var location = await _locationService.GetCurrentLocationAsync();
                        
                        // ✅ UI update must be on main thread
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await UpdateStatus($"📍 Position trouvée: {location?.Latitude:F4}, {location?.Longitude:F4}");
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Erreur préchargement location: {ex.Message}");
                        
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await UpdateStatus("⚠️ Localisation en cours...");
                        });
                    }
                });
                
                await Task.Delay(800);
                await UpdateStatus("🗺️ Préparation de la carte...");
                await Task.Delay(500);
                await UpdateStatus("✨ Tout est prêt pour l'aventure !");
                await Task.Delay(600);
            }
            catch (Exception ex)
            {
                await UpdateStatus("⚡ Finalisation...");
                System.Diagnostics.Debug.WriteLine($"❌ Erreur init: {ex.Message}");
                await Task.Delay(500);
            }
        }

        private async Task UpdateStatus(string message)
        {
            try
            {
                // ✅ CORRECTION: Toujours utiliser MainThread pour les mises à jour UI
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (StatusLabel != null)
                    {
                        StatusLabel.Text = message;
                        // Animation effects
                        await StatusLabel.ScaleTo(1.05, 100);
                        await StatusLabel.ScaleTo(1, 100);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur UpdateStatus: {ex.Message}");
            }
        }

        private async Task NavigateToMapWithStyle()
        {
            try
            {
                // ✅ S'assurer que les animations sont arrêtées AVANT la sortie
                _animationsRunning = false;

                // ✅ CORRECTION: Animation de sortie thread-safe
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Animation de sortie plus rapide
                    var exitTasks = new List<Task>();
                    
                    if (LogoBorder != null)
                    {
                        exitTasks.Add(LogoBorder.ScaleTo(0.8, 300));
                        exitTasks.Add(LogoBorder.FadeTo(0, 300));
                    }
                    if (LogoIcon != null)
                    {
                        exitTasks.Add(LogoIcon.RotateTo(180, 300));
                    }
                    if (TitleLabel != null)
                    {
                        exitTasks.Add(TitleLabel.TranslateTo(-300, 0, 300, Easing.CubicIn));
                        exitTasks.Add(TitleLabel.FadeTo(0, 300));
                    }
                    if (SubtitleLabel != null)
                    {
                        exitTasks.Add(SubtitleLabel.TranslateTo(300, 0, 300, Easing.CubicIn));
                        exitTasks.Add(SubtitleLabel.FadeTo(0, 300));
                    }
                    if (LoadingStack != null)
                    {
                        exitTasks.Add(LoadingStack.TranslateTo(0, 50, 300, Easing.CubicIn));
                        exitTasks.Add(LoadingStack.FadeTo(0, 300));
                    }

                    if (exitTasks.Count > 0)
                        await Task.WhenAll(exitTasks);
                });

                // ✅ Petite pause pour s'assurer que tout est terminé
                await Task.Delay(100);

                // Navigation vers la carte
                await Shell.Current.GoToAsync("//MapPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur navigation: {ex.Message}");
                await Shell.Current.GoToAsync("//MapPage");
            }
        }

        // ✅ Nettoyer les ressources à la fermeture SANS dispose immédiat
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _animationsRunning = false;
        }

        // ✅ Ajout d'une méthode pour arrêt d'urgence SANS dispose
        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            _animationsRunning = false;
        }
    }
}