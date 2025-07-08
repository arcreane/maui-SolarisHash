using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views
{
    public class InteractiveMapView : ContentView
    {
        // ✅ CORRECTION: Changer readonly en private pour pouvoir les modifier
        private ScrollView _mapScrollView;
        private Grid _mapContainer;
        private Frame _mapFrame;
        private Label _userLocationPin;
        private readonly Dictionary<Place, View> _placePins = new(); // ✅ CORRECTION: View au lieu de Label
        private Label _selectedPlaceInfo;
        private Button _centerButton;
        private Label _statusLabel;
        
        private IEnumerable<Place>? _places;
        private Location? _userLocation;
        private const double MapWidth = 1000;
        private const double MapHeight = 800;
        private const double MapScale = 50000; // Échelle pour la conversion GPS

        public static readonly BindableProperty PlacesProperty = BindableProperty.Create(
            nameof(Places), 
            typeof(IEnumerable<Place>), 
            typeof(InteractiveMapView), 
            null, 
            propertyChanged: OnPlacesChanged);

        public IEnumerable<Place>? Places
        {
            get => (IEnumerable<Place>?)GetValue(PlacesProperty);
            set => SetValue(PlacesProperty, value);
        }

        public InteractiveMapView()
        {
            CreateMapInterface();
            LoadUserLocationAsync();
        }

        private void CreateMapInterface()
        {
            // Container principal de la carte avec fond carte réaliste
            _mapContainer = new Grid
            {
                WidthRequest = MapWidth,
                HeightRequest = MapHeight,
                BackgroundColor = Color.FromRgb(240, 248, 255) // Bleu très clair pour l'eau
            };

            // Ajouter une grille de fond pour simuler une carte
            AddMapBackground();

            // Pin de l'utilisateur (vous êtes ici)
            _userLocationPin = new Label
            {
                Text = "📍",
                FontSize = 28,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                IsVisible = false,
                ZIndex = 100 // Au-dessus de tout
            };
            _mapContainer.Children.Add(_userLocationPin);

            // Frame contenant la carte avec bordure
            _mapFrame = new Frame
            {
                Content = _mapContainer,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromRgb(100, 149, 237),
                CornerRadius = 8,
                HasShadow = true,
                Padding = 2
            };

            // ScrollView pour navigation
            _mapScrollView = new ScrollView
            {
                Content = _mapFrame,
                Orientation = ScrollOrientation.Both,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always,
                HeightRequest = 400 // Hauteur fixe pour l'affichage
            };

            // Label de statut
            _statusLabel = new Label
            {
                Text = "🗺️ Chargement de la carte...",
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Gray,
                Padding = new Thickness(8, 4)
            };

            // Informations du lieu sélectionné
            _selectedPlaceInfo = new Label
            {
                Text = "📍 Cliquez sur un pin pour voir les détails",
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.DarkBlue,
                Padding = new Thickness(12, 8),
                BackgroundColor = Color.FromRgb(240, 248, 255),
                LineBreakMode = LineBreakMode.WordWrap
            };

            // Boutons de contrôle
            var controlsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 8
            };

            _centerButton = new Button
            {
                Text = "🎯 Centrer",
                BackgroundColor = Colors.Blue,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12,
                Padding = new Thickness(8, 4)
            };
            _centerButton.Clicked += OnCenterButtonClicked;
            Grid.SetColumn(_centerButton, 0);
            controlsGrid.Children.Add(_centerButton);

            var zoomInButton = new Button
            {
                Text = "🔍 Zoom +",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12,
                Padding = new Thickness(8, 4)
            };
            zoomInButton.Clicked += OnZoomInClicked;
            Grid.SetColumn(zoomInButton, 1);
            controlsGrid.Children.Add(zoomInButton);

            var zoomOutButton = new Button
            {
                Text = "🔍 Zoom -",
                BackgroundColor = Colors.Orange,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12,
                Padding = new Thickness(8, 4)
            };
            zoomOutButton.Clicked += OnZoomOutClicked;
            Grid.SetColumn(zoomOutButton, 2);
            controlsGrid.Children.Add(zoomOutButton);

            // Légende compacte
            var legend = CreateCompactLegend();

            // Layout principal
            var mainLayout = new StackLayout
            {
                Spacing = 8,
                Padding = 12,
                Children =
                {
                    new Label
                    {
                        Text = "🗺️ Carte Interactive",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.DarkBlue
                    },
                    _statusLabel,
                    _mapScrollView,
                    controlsGrid,
                    _selectedPlaceInfo,
                    legend
                }
            };

            Content = mainLayout;
        }

        private void AddMapBackground()
        {
            try
            {
                // ✅ CORRECTION: Ajout d'un fond de base pour la carte
                var mapBackground = new BoxView
                {
                    Color = Color.FromRgb(245, 252, 255), // Bleu très clair pour simuler l'eau
                    WidthRequest = MapWidth,
                    HeightRequest = MapHeight,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill
                };
                _mapContainer.Children.Add(mapBackground);

                // ✅ CORRECTION: Grille verticale (lignes nord-sud) avec positionnement corrigé
                for (int x = 0; x <= MapWidth; x += 100)
                {
                    var verticalLine = new BoxView
                    {
                        Color = Color.FromRgba(180, 180, 180, 0.4), // Plus visible
                        WidthRequest = 1,
                        HeightRequest = MapHeight,
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.Fill,
                        TranslationX = x, // ✅ CORRECTION: Utiliser TranslationX au lieu de Margin
                        ZIndex = 1 // Sous les pins
                    };
                    _mapContainer.Children.Add(verticalLine);
                }

                // ✅ CORRECTION: Grille horizontale (lignes est-ouest) avec positionnement corrigé
                for (int y = 0; y <= MapHeight; y += 100)
                {
                    var horizontalLine = new BoxView
                    {
                        Color = Color.FromRgba(180, 180, 180, 0.4), // Plus visible
                        WidthRequest = MapWidth,
                        HeightRequest = 1,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Start,
                        TranslationY = y, // ✅ CORRECTION: Utiliser TranslationY au lieu de Margin
                        ZIndex = 1 // Sous les pins
                    };
                    _mapContainer.Children.Add(horizontalLine);
                }

                // ✅ AJOUT: Quelques éléments décoratifs pour simuler une vraie carte
                AddMapDecorations();

                Console.WriteLine($"🗺️ Fond de carte ajouté: {MapWidth}x{MapHeight}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur création fond de carte: {ex.Message}");
            }
        }

        private void AddMapDecorations()
        {
            try
            {
                // Ajouter quelques "zones" colorées pour simuler des quartiers
                var zones = new[]
                {
                    new { X = 200, Y = 150, Width = 150, Height = 100, Color = Color.FromRgba(144, 238, 144, 0.3), Name = "Zone Verte" },
                    new { X = 500, Y = 300, Width = 200, Height = 120, Color = Color.FromRgba(255, 182, 193, 0.3), Name = "Zone Résidentielle" },
                    new { X = 300, Y = 500, Width = 180, Height = 90, Color = Color.FromRgba(173, 216, 230, 0.3), Name = "Zone Commerciale" },
                    new { X = 700, Y = 200, Width = 120, Height = 150, Color = Color.FromRgba(255, 255, 224, 0.3), Name = "Zone Industrielle" }
                };

                foreach (var zone in zones)
                {
                    var zoneBox = new Frame
                    {
                        BackgroundColor = zone.Color,
                        BorderColor = Colors.Transparent,
                        CornerRadius = 8,
                        Padding = 4,
                        WidthRequest = zone.Width,
                        HeightRequest = zone.Height,
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.Start,
                        TranslationX = zone.X,
                        TranslationY = zone.Y,
                        ZIndex = 2, // Au-dessus du fond mais sous les pins
                        Content = new Label
                        {
                            Text = zone.Name,
                            FontSize = 10,
                            TextColor = Colors.Gray,
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center
                        }
                    };
                    _mapContainer.Children.Add(zoneBox);
                }

                // Ajouter une boussole décorative dans le coin
                var compass = new Label
                {
                    Text = "🧭 N",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.DarkBlue,
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0.8),
                    Padding = new Thickness(8, 4),
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start,
                    TranslationX = MapWidth - 60,
                    TranslationY = 20,
                    ZIndex = 50 // Au-dessus de tout
                };
                _mapContainer.Children.Add(compass);

                Console.WriteLine("🎨 Décorations de carte ajoutées");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur décorations: {ex.Message}");
            }
        }

        private StackLayout CreateCompactLegend()
        {
            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 12,
                Children =
                {
                    CreateLegendItem("📍", "Vous", Colors.Red),
                    CreateLegendItem("🏛️", "Monuments", Colors.Purple),
                    CreateLegendItem("🍽️", "Restaurants", Colors.Orange),
                    CreateLegendItem("🌳", "Parcs", Colors.Green)
                }
            };
        }

        private StackLayout CreateLegendItem(string emoji, string text, Color color)
        {
            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = emoji,
                        FontSize = 14,
                        VerticalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = text,
                        FontSize = 10,
                        TextColor = color,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            };
        }

        private static void OnPlacesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is InteractiveMapView mapView && newValue is IEnumerable<Place> places)
            {
                mapView.UpdateMapWithPlaces(places);
            }
        }

        private async void LoadUserLocationAsync()
        {
            try
            {
                _statusLabel.Text = "📍 Chargement de votre position...";
                
                var locationService = GetService<ILocationService>();
                if (locationService != null)
                {
                    _userLocation = await locationService.GetCurrentLocationAsync();
                    if (_userLocation != null)
                    {
                        UpdateUserLocationPin();
                        _statusLabel.Text = $"✅ Position: {_userLocation.Latitude:F4}, {_userLocation.Longitude:F4}";
                    }
                    else
                    {
                        _statusLabel.Text = "⚠️ Position non disponible";
                    }
                }
                else
                {
                    _statusLabel.Text = "❌ Service de localisation indisponible";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                Console.WriteLine($"❌ Erreur chargement position: {ex.Message}");
            }
        }

        private void UpdateUserLocationPin()
        {
            if (_userLocation == null) return;

            try
            {
                var (x, y) = ConvertGpsToMapCoordinates(_userLocation.Latitude, _userLocation.Longitude);
                
                // Position absolue dans le container
                _userLocationPin.TranslationX = x - 14; // Centrer le pin
                _userLocationPin.TranslationY = y - 28; // Centrer le pin
                _userLocationPin.IsVisible = true;
                
                Console.WriteLine($"📍 Position utilisateur sur carte: {x:F0}, {y:F0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur positionnement pin utilisateur: {ex.Message}");
            }
        }

        private void UpdateMapWithPlaces(IEnumerable<Place> places)
        {
            try
            {
                _places = places;
                
                // Nettoyer les anciens pins
                foreach (var pin in _placePins.Values)
                {
                    _mapContainer.Children.Remove(pin);
                }
                _placePins.Clear();

                if (!places.Any()) 
                {
                    _statusLabel.Text = "⚠️ Aucun lieu à afficher";
                    return;
                }

                // Calculer les limites pour le centrage automatique
                var bounds = CalculateMapBounds(places);
                
                // Ajouter les nouveaux pins
                int pinsAdded = 0;
                foreach (var place in places.Take(30)) // Limiter pour les performances
                {
                    if (place.Location != null)
                    {
                        CreatePlacePin(place);
                        pinsAdded++;
                    }
                }

                _statusLabel.Text = $"✅ {pinsAdded} lieux affichés sur la carte";
                Console.WriteLine($"🗺️ Carte mise à jour avec {pinsAdded} pins");
                
                // Centrer automatiquement sur la zone d'intérêt
                if (_userLocation != null)
                {
                    CenterMapOnUserWithDelay();
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur carte: {ex.Message}";
                Console.WriteLine($"❌ Erreur mise à jour carte: {ex.Message}");
            }
        }

        private (double minLat, double maxLat, double minLon, double maxLon) CalculateMapBounds(IEnumerable<Place> places)
        {
            var validPlaces = places.Where(p => p.Location != null).ToList();
            if (!validPlaces.Any()) return (0, 0, 0, 0);

            var minLat = validPlaces.Min(p => p.Location!.Latitude);
            var maxLat = validPlaces.Max(p => p.Location!.Latitude);
            var minLon = validPlaces.Min(p => p.Location!.Longitude);
            var maxLon = validPlaces.Max(p => p.Location!.Longitude);

            return (minLat, maxLat, minLon, maxLon);
        }

        private void CreatePlacePin(Place place)
        {
            try
            {
                var (x, y) = ConvertGpsToMapCoordinates(place.Location.Latitude, place.Location.Longitude);
                
                var pinEmoji = GetPlaceEmoji(place);
                var pinColor = GetPlaceColor(place);

                // Pin avec fond pour meilleure visibilité
                var pinContainer = new Frame
                {
                    Content = new Label
                    {
                        Text = pinEmoji,
                        FontSize = 20,
                        TextColor = pinColor,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    },
                    BackgroundColor = Colors.White,
                    BorderColor = pinColor,
                    CornerRadius = 15,
                    Padding = 4,
                    HasShadow = true,
                    WidthRequest = 30,
                    HeightRequest = 30,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start
                };

                // Position absolue
                pinContainer.TranslationX = x - 15;
                pinContainer.TranslationY = y - 15;

                // Ajouter interaction tactile
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnPlacePinTapped(place);
                pinContainer.GestureRecognizers.Add(tapGesture);

                _mapContainer.Children.Add(pinContainer);
                _placePins[place] = pinContainer; // ✅ CORRECTION: Maintenant compatible View = Frame

                Console.WriteLine($"📌 Pin créé pour {place.Name} à ({x:F0}, {y:F0})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur création pin pour {place.Name}: {ex.Message}");
            }
        }

        private (double X, double Y) ConvertGpsToMapCoordinates(double latitude, double longitude)
        {
            try
            {
                // Utiliser la position utilisateur comme centre, ou Paris par défaut
                var centerLat = _userLocation?.Latitude ?? 48.8566;
                var centerLon = _userLocation?.Longitude ?? 2.3522;

                // Conversion avec projection Mercator simplifiée
                var deltaLat = latitude - centerLat;
                var deltaLon = longitude - centerLon;

                // Facteur de conversion (ajusté pour la France)
                var metersPerDegree = 111000; // Environ 111km par degré
                var scale = MapScale; // Mètres que représente la carte

                var x = (deltaLon * metersPerDegree * Math.Cos(ToRadians(centerLat)) / scale) * (MapWidth / 2) + (MapWidth / 2);
                var y = (-deltaLat * metersPerDegree / scale) * (MapHeight / 2) + (MapHeight / 2);

                // Limiter aux bordures de la carte
                x = Math.Max(30, Math.Min(MapWidth - 30, x));
                y = Math.Max(30, Math.Min(MapHeight - 30, y));

                return (x, y);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur conversion GPS: {ex.Message}");
                return (MapWidth / 2, MapHeight / 2);
            }
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;

        private string GetPlaceEmoji(Place place)
        {
            var category = place.MainCategory.ToLower();
            return category switch
            {
                var c when c.Contains("restaurant") || c.Contains("café") => "🍽️",
                var c when c.Contains("musée") || c.Contains("monument") => "🏛️",
                var c when c.Contains("parc") || c.Contains("jardin") => "🌳",
                var c when c.Contains("hôtel") => "🏨",
                var c when c.Contains("commerce") || c.Contains("shop") => "🏪",
                var c when c.Contains("hôpital") || c.Contains("pharmacie") => "🏥",
                var c when c.Contains("école") || c.Contains("université") => "🎓",
                var c when c.Contains("église") => "⛪",
                var c when c.Contains("tourisme") => "🗺️",
                _ => "📌"
            };
        }

        private Color GetPlaceColor(Place place)
        {
            var category = place.MainCategory.ToLower();
            return category switch
            {
                var c when c.Contains("restaurant") || c.Contains("café") => Colors.Orange,
                var c when c.Contains("musée") || c.Contains("monument") => Colors.Purple,
                var c when c.Contains("parc") || c.Contains("jardin") => Colors.Green,
                var c when c.Contains("hôtel") => Colors.Blue,
                var c when c.Contains("commerce") => Colors.DarkGreen,
                var c when c.Contains("hôpital") => Colors.Red,
                var c when c.Contains("tourisme") => Colors.DarkBlue,
                _ => Colors.Gray
            };
        }

        private void OnPlacePinTapped(Place place)
        {
            try
            {
                var info = $"📍 {place.Name}\n" +
                          $"🏷️ {place.MainCategory}\n" +
                          $"📏 {place.FormattedDistance}\n" +
                          $"📍 {place.Address}";

                if (!string.IsNullOrEmpty(place.Description))
                {
                    var cleanDescription = place.Description.Replace("[RÉEL]", "").Replace("[DÉMO]", "").Trim();
                    if (!string.IsNullOrEmpty(cleanDescription))
                    {
                        info += $"\n📝 {cleanDescription}";
                    }
                }

                _selectedPlaceInfo.Text = info;
                _selectedPlaceInfo.BackgroundColor = Color.FromRgb(230, 244, 255);
                _selectedPlaceInfo.TextColor = Colors.DarkBlue;

                Console.WriteLine($"🎯 Pin sélectionné: {place.Name}");

                // Animation du pin sélectionné
                if (_placePins.TryGetValue(place, out var pin))
                {
                    var originalScale = pin.Scale;
                    pin.ScaleTo(1.3, 200, Easing.BounceOut)
                       .ContinueWith(_ => pin.ScaleTo(originalScale, 200, Easing.BounceIn));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur sélection pin: {ex.Message}");
            }
        }

        private void OnCenterButtonClicked(object? sender, EventArgs e)
        {
            CenterMapOnUser();
        }

        private void OnZoomInClicked(object? sender, EventArgs e)
        {
            // Simplifier le zoom - ajuster la taille du container
            _mapContainer.Scale = Math.Min(_mapContainer.Scale * 1.3, 3.0);
            _statusLabel.Text = $"🔍 Zoom: {_mapContainer.Scale:F1}x";
        }

        private void OnZoomOutClicked(object? sender, EventArgs e)
        {
            _mapContainer.Scale = Math.Max(_mapContainer.Scale / 1.3, 0.5);
            _statusLabel.Text = $"🔍 Zoom: {_mapContainer.Scale:F1}x";
        }

        private async void CenterMapOnUserWithDelay()
        {
            await Task.Delay(500); // Laisser le temps au layout de se construire
            CenterMapOnUser();
        }

        private void CenterMapOnUser()
        {
            try
            {
                if (_userLocation == null) 
                {
                    _statusLabel.Text = "⚠️ Position non disponible pour centrage";
                    return;
                }

                var (userX, userY) = ConvertGpsToMapCoordinates(_userLocation.Latitude, _userLocation.Longitude);
                
                // Centrer le ScrollView sur la position utilisateur
                var scrollX = Math.Max(0, userX - (_mapScrollView.Width / 2));
                var scrollY = Math.Max(0, userY - (_mapScrollView.Height / 2));

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await _mapScrollView.ScrollToAsync(scrollX, scrollY, true);
                    _statusLabel.Text = "🎯 Carte centrée sur votre position";
                });
                
                Console.WriteLine($"🎯 Carte centrée sur ({userX:F0}, {userY:F0})");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "❌ Erreur centrage carte";
                Console.WriteLine($"❌ Erreur centrage carte: {ex.Message}");
            }
        }

        private static T? GetService<T>() where T : class
        {
            try
            {
                return Application.Current?.Handler?.MauiContext?.Services?.GetService<T>();
            }
            catch
            {
                return null;
            }
        }
    }
}