using MyApp.ViewModels;

namespace MyApp
{
    public partial class MapPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;
        private bool _isSearchPanelVisible = false;
        private bool _isPlacesPanelVisible = false;
        private bool _isCompassPanelVisible = false; // ✅ NOUVEAU

        public MapPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            // Animations d'entrée
            InitializeAnimations();

            // Lancer automatiquement la recherche
            Loaded += OnPageLoaded;
        }

        private void InitializeAnimations()
        {
            // Header entre par le haut avec effet de rebond
            HeaderCard.Opacity = 0;

            // Boutons flottants entrent avec rotation
            SearchToggleButtonFrame.Scale = 0;
            SearchToggleButtonFrame.Rotation = 180;

            CompassToggleButtonFrame.Scale = 0; // ✅ NOUVEAU
            CompassToggleButtonFrame.Rotation = 180; // ✅ NOUVEAU

            PlacesToggleButtonFrame.Scale = 0;
            PlacesToggleButtonFrame.Rotation = -180;
        }

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            try
            {
                // ✅ SOLUTION: Lancer les animations en premier pour que l'UI soit responsive
                var animationTask = AnimateEntranceAsync();

                // ✅ CORRECTION: Lancer le chargement en arrière-plan de manière thread-safe
                var loadingTask = Task.Run(async () =>
                {
                    try
                    {
                        // ✅ S'assurer que l'exécution de la commande se fait sur le main thread
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            if (_viewModel.LoadPlacesCommand.CanExecute(null))
                            {
                                await _viewModel.LoadPlacesCommand.ExecuteAsync(null);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Erreur LoadPlaces: {ex.Message}");
                    }
                });

                // Attendre que les animations soient terminées
                await animationTask;

                // Optionnel: Attendre que le chargement soit fini aussi
                // await loadingTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement: {ex.Message}");
            }
        }

        private async Task AnimateEntranceAsync()
        {
            try
            {
                // ✅ S'assurer qu'on est sur le main thread pour les animations
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // ✅ Déplacer temporairement pour l'animation
                    HeaderCard.TranslationY = -50;

                    // Header glisse du haut avec rebond
                    var headerTask = Task.WhenAll(
                        HeaderCard.TranslateTo(0, 0, 800, Easing.BounceOut),
                        HeaderCard.FadeTo(1, 600)
                    );

                    // Attendre un peu puis animer les boutons
                    await Task.Delay(300);

                    var buttonsTask = Task.WhenAll(
                        SearchToggleButtonFrame.ScaleTo(1, 600, Easing.BounceOut),
                        SearchToggleButtonFrame.RotateTo(0, 600, Easing.CubicOut),
                        CompassToggleButtonFrame.ScaleTo(1, 600, Easing.BounceOut), // ✅ NOUVEAU
                        CompassToggleButtonFrame.RotateTo(0, 600, Easing.CubicOut), // ✅ NOUVEAU
                        PlacesToggleButtonFrame.ScaleTo(1, 600, Easing.BounceOut),
                        PlacesToggleButtonFrame.RotateTo(0, 600, Easing.CubicOut)
                    );

                    await Task.WhenAll(headerTask, buttonsTask);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur AnimateEntrance: {ex.Message}");
            }
        }

        private async void OnSearchToggleClicked(object? sender, EventArgs e)
        {
            try
            {
                // ✅ CORRECTION: S'assurer qu'on est sur le main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _isSearchPanelVisible = !_isSearchPanelVisible;

                    if (_isSearchPanelVisible)
                    {
                        // Afficher l'overlay sombre
                        DarkOverlay.IsVisible = true;
                        await Task.WhenAll(
                            DarkOverlay.FadeTo(0.5, 300),
                            SearchPanel.TranslateTo(0, 0, 400, Easing.CubicOut)
                        );

                        // Animation du bouton
                        SearchToggleButton.Text = "✕";
                        await SearchToggleButtonFrame.ScaleTo(1.1, 100);
                        await SearchToggleButtonFrame.ScaleTo(1, 100);
                    }
                    else
                    {
                        // Fermer avec animation inverse
                        await Task.WhenAll(
                            DarkOverlay.FadeTo(0, 300),
                            SearchPanel.TranslateTo(0, 400, 400, Easing.CubicIn)
                        );
                        DarkOverlay.IsVisible = false;

                        SearchToggleButton.Text = "⚙️";
                        await SearchToggleButtonFrame.RotateTo(360, 300);
                        SearchToggleButtonFrame.Rotation = 0;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur animation search: {ex.Message}");
            }
        }

        // ✅ NOUVELLE MÉTHODE: Gestion du panneau boussole
        private async void OnCompassToggleClicked(object? sender, EventArgs e)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _isCompassPanelVisible = !_isCompassPanelVisible;

                    if (_isCompassPanelVisible)
                    {
                        // Afficher l'overlay et le panneau
                        DarkOverlay.IsVisible = true;
                        await Task.WhenAll(
                            DarkOverlay.FadeTo(0.3, 300),
                            CompassPanel.TranslateTo(0, 0, 400, Easing.CubicOut)
                        );

                        CompassToggleButton.Text = "✕";
                        await CompassToggleButtonFrame.ScaleTo(1.1, 100);
                        await CompassToggleButtonFrame.ScaleTo(1, 100);
                    }
                    else
                    {
                        // Fermer avec style
                        await Task.WhenAll(
                            DarkOverlay.FadeTo(0, 300),
                            CompassPanel.TranslateTo(340, 0, 400, Easing.CubicIn)
                        );
                        DarkOverlay.IsVisible = false;

                        CompassToggleButton.Text = "🧭";
                        await CompassToggleButtonFrame.RotateTo(360, 300);
                        CompassToggleButtonFrame.Rotation = 0;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur animation compass: {ex.Message}");
            }
        }

        // ✅ NOUVELLE MÉTHODE: Fermer le panneau boussole
        private async void OnCloseCompassPanel(object? sender, EventArgs e)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _isCompassPanelVisible = false;

                    await Task.WhenAll(
                        DarkOverlay.FadeTo(0, 300),
                        CompassPanel.TranslateTo(340, 0, 400, Easing.CubicIn)
                    );
                    DarkOverlay.IsVisible = false;

                    CompassToggleButton.Text = "🧭";
                    await CompassToggleButtonFrame.RotateTo(360, 300);
                    CompassToggleButtonFrame.Rotation = 0;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur fermeture compass: {ex.Message}");
            }
        }

        private async void OnPlacesToggleClicked(object? sender, EventArgs e)
        {
            try
            {
                // ✅ CORRECTION: S'assurer qu'on est sur le main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _isPlacesPanelVisible = !_isPlacesPanelVisible;

                    if (_isPlacesPanelVisible)
                    {
                        // Afficher l'overlay et le panneau
                        DarkOverlay.IsVisible = true;
                        await Task.WhenAll(
                            DarkOverlay.FadeTo(0.3, 300),
                            PlacesPanel.TranslateTo(0, 0, 400, Easing.CubicOut)
                        );

                        PlacesToggleButton.Text = "✕";
                        await PlacesToggleButtonFrame.ScaleTo(1.1, 100);
                        await PlacesToggleButtonFrame.ScaleTo(1, 100);
                    }
                    else
                    {
                        // Fermer avec style
                        await Task.WhenAll(
                            DarkOverlay.FadeTo(0, 300),
                            PlacesPanel.TranslateTo(-340, 0, 400, Easing.CubicIn)
                        );
                        DarkOverlay.IsVisible = false;

                        PlacesToggleButton.Text = "📍";
                        await PlacesToggleButtonFrame.RotateTo(-360, 300);
                        PlacesToggleButtonFrame.Rotation = 0;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur animation places: {ex.Message}");
            }
        }

        private async void OnClosePlacesPanel(object? sender, EventArgs e)
        {
            try
            {
                // ✅ CORRECTION: S'assurer qu'on est sur le main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _isPlacesPanelVisible = false;

                    await Task.WhenAll(
                        DarkOverlay.FadeTo(0, 300),
                        PlacesPanel.TranslateTo(-340, 0, 400, Easing.CubicIn)
                    );
                    DarkOverlay.IsVisible = false;

                    PlacesToggleButton.Text = "📍";
                    await PlacesToggleButtonFrame.RotateTo(-360, 300);
                    PlacesToggleButtonFrame.Rotation = 0;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur fermeture places: {ex.Message}");
            }
        }

        private async void OnOverlayTapped(object? sender, EventArgs e)
        {
            // Fermer tous les panneaux si on tape sur l'overlay
            if (_isSearchPanelVisible)
            {
                OnSearchToggleClicked(sender, e);
            }
            if (_isPlacesPanelVisible)
            {
                OnClosePlacesPanel(sender, e);
            }
            if (_isCompassPanelVisible) // ✅ NOUVEAU
            {
                OnCloseCompassPanel(sender, e);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel?.Dispose();
        }
    }
}