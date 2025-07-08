using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views
{
    public class InteractiveMapView : ContentView
    {
        private readonly ScrollView _mapScrollView;
        private readonly Grid _mapContainer;
        private readonly Frame _mapFrame;
        private readonly Label _userLocationPin;
        private readonly Dictionary<Place, Label> _placePins = new();
        private readonly Label _selectedPlaceInfo;
        private readonly Button _centerButton;
        
        private IEnumerable<Place>? _places;
        private Location? _userLocation;
        private double _mapScale = 1.0;
        private const double MapWidth = 800;
        private const double MapHeight = 600;

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
            // Container principal de la carte
            _mapContainer = new Grid
            {
                WidthRequest = MapWidth,
                HeightRequest = MapHeight,
                BackgroundColor = Colors.LightBlue // Couleur "eau" comme fond
            };

            // Pin de l'utilisateur (vous êtes ici)
            _userLocationPin = new Label
            {
                Text = "📍",
                FontSize = 32,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false
            };
            _mapContainer.Children.Add(_userLocationPin);

            // Frame contenant la carte
            _mapFrame = new Frame
            {
                Content = _mapContainer,
                BackgroundColor = Colors.White,
                BorderColor = Colors.Gray,
                CornerRadius = 12,
                HasShadow = true,
                Padding = 0
            };

            // ScrollView pour pouvoir zoomer/déplacer
            _mapScrollView = new ScrollView
            {
                Content = _mapFrame,
                Orientation = ScrollOrientation.Both,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always,
                // ZoomScale = 1.0,
                // MaximumZoomScale = 3.0,
                // MinimumZoomScale = 0.5
            };

            // Informations du lieu sélectionné
            _selectedPlaceInfo = new Label
            {
                Text = "📍 Cliquez sur un pin pour voir les détails",
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Gray,
                Padding = new Thickness(16, 8),
                BackgroundColor = Colors.LightYellow
            };

            // Bouton pour centrer sur l'utilisateur
            _centerButton = new Button
            {
                Text = "🎯 Me localiser",
                BackgroundColor = Colors.Blue,
                TextColor = Colors.White,
                CornerRadius = 20,
                FontSize = 14,
                Padding = new Thickness(16, 8),
                HorizontalOptions = LayoutOptions.Center
            };
            _centerButton.Clicked += OnCenterButtonClicked;

            // Légende de la carte
            var legend = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 16,
                Children =
                {
                    CreateLegendItem("📍", "Vous", Colors.Red),
                    CreateLegendItem("🏛️", "Monuments", Colors.Purple),
                    CreateLegendItem("🍽️", "Restaurants", Colors.Orange),
                    CreateLegendItem("🏪", "Services", Colors.Green),
                    CreateLegendItem("🌳", "Parcs", Colors.DarkGreen)
                }
            };

            // Layout principal
            var mainLayout = new StackLayout
            {
                Spacing = 12,
                Padding = 16,
                Children =
                {
                    new Label
                    {
                        Text = "🗺️ Carte Interactive",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.Black
                    },
                    _mapScrollView,
                    _selectedPlaceInfo,
                    _centerButton,
                    legend,
                    new Label
                    {
                        Text = "💡 Pincez pour zoomer, glissez pour naviguer",
                        FontSize = 12,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.Gray,
                        // FontStyle = FontStyles.Italic
                        FontAttributes = FontAttributes.Italic

                    }
                }
            };

            Content = mainLayout;

            // Charger la position utilisateur
            LoadUserLocationAsync();
        }

        private StackLayout CreateLegendItem(string emoji, string text, Color color)
        {
            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 4,
                Children =
                {
                    new Label
                    {
                        Text = emoji,
                        FontSize = 16,
                        VerticalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = text,
                        FontSize = 12,
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
                var locationService = GetService<ILocationService>();
                if (locationService != null)
                {
                    _userLocation = await locationService.GetCurrentLocationAsync();
                    UpdateUserLocationPin();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur chargement position: {ex.Message}");
            }
        }

        private void UpdateUserLocationPin()
        {
            if (_userLocation == null) return;

            try
            {
                var (x, y) = ConvertGpsToMapCoordinates(_userLocation.Latitude, _userLocation.Longitude);
                
                _userLocationPin.IsVisible = true;
                _userLocationPin.TranslationX = x - 16; // Centrer le pin
                _userLocationPin.TranslationY = y - 16;
                
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

                if (!places.Any()) return;

                // Ajouter les nouveaux pins
                foreach (var place in places.Take(20)) // Limiter à 20 pour les performances
                {
                    if (place.Location != null)
                    {
                        CreatePlacePin(place);
                    }
                }

                Console.WriteLine($"🗺️ Carte mise à jour avec {_placePins.Count} pins");
                
                // Centrer automatiquement si c'est la première fois
                if (_userLocation != null)
                {
                    CenterMapOnUser();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur mise à jour carte: {ex.Message}");
            }
        }

        private void CreatePlacePin(Place place)
        {
            try
            {
                var (x, y) = ConvertGpsToMapCoordinates(place.Location.Latitude, place.Location.Longitude);
                
                var pinEmoji = GetPlaceEmoji(place);
                var pinColor = GetPlaceColor(place);

                var pin = new Label
                {
                    Text = pinEmoji,
                    FontSize = 24,
                    TextColor = pinColor,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TranslationX = x - 12,
                    TranslationY = y - 12
                };

                // Ajouter interaction tactile
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnPlacePinTapped(place);
                pin.GestureRecognizers.Add(tapGesture);

                _mapContainer.Children.Add(pin);
                _placePins[place] = pin;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur création pin pour {place.Name}: {ex.Message}");
            }
        }

        private (double X, double Y) ConvertGpsToMapCoordinates(double latitude, double longitude)
        {
            // Conversion GPS → coordonnées carte simplifiée
            // On centre la carte sur la position utilisateur ou Paris par défaut
            var centerLat = _userLocation?.Latitude ?? 48.8566;
            var centerLon = _userLocation?.Longitude ?? 2.3522;

            // Facteur de conversion (approximatif pour la France)
            var latRange = 0.02; // ~2km en latitude
            var lonRange = 0.02; // ~2km en longitude

            var x = ((longitude - centerLon) / lonRange) * (MapWidth / 2) + (MapWidth / 2);
            var y = ((centerLat - latitude) / latRange) * (MapHeight / 2) + (MapHeight / 2);

            // Limiter aux bordures de la carte
            x = Math.Max(0, Math.Min(MapWidth, x));
            y = Math.Max(0, Math.Min(MapHeight, y));

            return (x, y);
        }

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
                var c when c.Contains("parc") || c.Contains("jardin") => Colors.DarkGreen,
                var c when c.Contains("hôtel") => Colors.Blue,
                var c when c.Contains("commerce") => Colors.Green,
                var c when c.Contains("hôpital") => Colors.Red,
                _ => Colors.Black
            };
        }

        private void OnPlacePinTapped(Place place)
        {
            try
            {
                _selectedPlaceInfo.Text = $"📍 {place.Name}\n🏷️ {place.MainCategory} • 📏 {place.FormattedDistance}\n📋 {place.Address}";
                _selectedPlaceInfo.BackgroundColor = Colors.LightCyan;
                _selectedPlaceInfo.TextColor = Colors.Black;

                Console.WriteLine($"🎯 Pin sélectionné: {place.Name}");

                // Animation du pin sélectionné
                if (_placePins.TryGetValue(place, out var pin))
                {
                    pin.ScaleTo(1.5, 200).ContinueWith(_ => pin.ScaleTo(1.0, 200));
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

        private void CenterMapOnUser()
        {
            try
            {
                if (_userLocation == null) return;

                // Centrer le ScrollView sur la position utilisateur
                var (userX, userY) = ConvertGpsToMapCoordinates(_userLocation.Latitude, _userLocation.Longitude);
                
                var scrollX = Math.Max(0, userX - (_mapScrollView.Width / 2));
                var scrollY = Math.Max(0, userY - (_mapScrollView.Height / 2));

                _mapScrollView.ScrollToAsync(scrollX, scrollY, true);
                
                Console.WriteLine($"🎯 Carte centrée sur utilisateur");
            }
            catch (Exception ex)
            {
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