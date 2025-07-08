using MyApp.ViewModels;

namespace MyApp
{
    public partial class MapPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;
        private bool _isSearchPanelVisible = false;
        private bool _isPlacesPanelVisible = false;

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
            
            PlacesToggleButtonFrame.Scale = 0;
            PlacesToggleButtonFrame.Rotation = -180;
        }

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            try
            {
                // ✅ SOLUTION: Lancer les animations en premier pour que l'UI soit responsive
                var animationTask = AnimateEntranceAsync();
                
                // ✅ Lancer le chargement en parallèle (pas en await)
                var loadingTask = Task.Run(async () =>
                {
                    try
                    {
                        if (_viewModel.LoadPlacesCommand.CanExecute(null))
                        {
                            await _viewModel.LoadPlacesCommand.ExecuteAsync(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Erreur LoadPlaces: {ex.Message}");
                    }
                });
                
                // Attendre que les animations soient terminées
                await animationTask;
                
                // Optionnel: Attendre que le chargement soit fini aussi
                // await loadingTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur chargement: {ex.Message}");
            }
        }

        private async Task AnimateEntranceAsync()
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
                PlacesToggleButtonFrame.ScaleTo(1, 600, Easing.BounceOut),
                PlacesToggleButtonFrame.RotateTo(0, 600, Easing.CubicOut)
            );

            await Task.WhenAll(headerTask, buttonsTask);
        }

        private async void OnSearchToggleClicked(object? sender, EventArgs e)
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur animation search: {ex.Message}");
            }
        }

        private async void OnPlacesToggleClicked(object? sender, EventArgs e)
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur animation places: {ex.Message}");
            }
        }

        private async void OnClosePlacesPanel(object? sender, EventArgs e)
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur fermeture places: {ex.Message}");
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
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel?.Dispose();
        }
    }
}