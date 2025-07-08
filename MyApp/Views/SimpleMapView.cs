using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views
{
    public class SimpleMapView : ContentView
    {
        // ✅ CORRECTION: Enlever readonly pour pouvoir les assigner
        private WebView _webView;
        private Label _statusLabel;
        private Label _selectedPlaceInfo;
        
        private IEnumerable<Place>? _places;
        private Location? _userLocation;

        public static readonly BindableProperty PlacesProperty = BindableProperty.Create(
            nameof(Places), 
            typeof(IEnumerable<Place>), 
            typeof(SimpleMapView), 
            null, 
            propertyChanged: OnPlacesChanged);

        public IEnumerable<Place>? Places
        {
            get => (IEnumerable<Place>?)GetValue(PlacesProperty);
            set => SetValue(PlacesProperty, value);
        }

        public SimpleMapView()
        {
            CreateSimpleMap();
            LoadUserLocationAsync();
        }

        private void CreateSimpleMap()
        {
            // ✅ CARTE SIMPLE avec WebView (OpenStreetMap)
            _webView = new WebView
            {
                HeightRequest = 400,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            // Label de statut
            _statusLabel = new Label
            {
                Text = "🗺️ Chargement de la carte...",
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Blue,
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
                ColumnSpacing = 8,
                Margin = new Thickness(0, 8)
            };

            var centerButton = new Button
            {
                Text = "🎯 Centrer",
                BackgroundColor = Colors.Blue,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12
            };
            centerButton.Clicked += OnCenterClicked;
            Grid.SetColumn(centerButton, 0);
            controlsGrid.Children.Add(centerButton);

            var zoomInButton = new Button
            {
                Text = "🔍 Zoom +",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12
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
                FontSize = 12
            };
            zoomOutButton.Clicked += OnZoomOutClicked;
            Grid.SetColumn(zoomOutButton, 2);
            controlsGrid.Children.Add(zoomOutButton);

            // Layout principal
            var mainLayout = new StackLayout
            {
                Spacing = 8,
                Padding = 12,
                Children =
                {
                    new Label
                    {
                        Text = "🗺️ Carte Interactive (OpenStreetMap)",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.DarkBlue
                    },
                    _statusLabel,
                    _webView, // ✅ CARTE WEBVIEW
                    controlsGrid,
                    _selectedPlaceInfo,
                    new Label
                    {
                        Text = "💡 Carte OpenStreetMap • Cliquez sur les pins pour plus d'infos",
                        FontSize = 10,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.Gray,
                        FontAttributes = FontAttributes.Italic
                    }
                }
            };

            Content = new Frame
            {
                Content = mainLayout,
                BackgroundColor = Colors.White,
                BorderColor = Colors.LightGray,
                CornerRadius = 12,
                Padding = 0,
                HasShadow = true
            };
        }

        private static void OnPlacesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SimpleMapView mapView && newValue is IEnumerable<Place> places)
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
                        // Charger la carte avec la position utilisateur
                        LoadMapHtml(_userLocation.Latitude, _userLocation.Longitude);
                        _statusLabel.Text = $"✅ Carte centrée sur {_userLocation.Latitude:F4}, {_userLocation.Longitude:F4}";
                    }
                    else
                    {
                        // Position par défaut : Paris
                        LoadMapHtml(48.8566, 2.3522);
                        _statusLabel.Text = "⚠️ Position non disponible - Carte centrée sur Paris";
                    }
                }
                else
                {
                    LoadMapHtml(48.8566, 2.3522);
                    _statusLabel.Text = "❌ Service de localisation indisponible";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                LoadMapHtml(48.8566, 2.3522);
                Console.WriteLine($"❌ Erreur chargement position: {ex.Message}");
            }
        }

        private void LoadMapHtml(double lat, double lon)
        {
            var html = GenerateMapHtml(lat, lon, _places);
            var htmlSource = new HtmlWebViewSource { Html = html };
            _webView.Source = htmlSource;
        }

        private void UpdateMapWithPlaces(IEnumerable<Place> places)
        {
            try
            {
                _places = places;
                
                if (_userLocation != null)
                {
                    LoadMapHtml(_userLocation.Latitude, _userLocation.Longitude);
                    _statusLabel.Text = $"✅ {places.Count()} lieux affichés sur la carte";
                }
                else
                {
                    LoadMapHtml(48.8566, 2.3522);
                    _statusLabel.Text = $"✅ {places.Count()} lieux affichés (position par défaut)";
                }
                
                Console.WriteLine($"🗺️ Carte mise à jour avec {places.Count()} lieux");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur carte: {ex.Message}";
                Console.WriteLine($"❌ Erreur mise à jour carte: {ex.Message}");
            }
        }

        private string GenerateMapHtml(double centerLat, double centerLon, IEnumerable<Place>? places)
        {
            var markers = "";
            
            // Ajouter la position utilisateur
            markers += $@"
                L.marker([{centerLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, 
                         {centerLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}])
                 .addTo(map)
                 .bindPopup('📍 Votre position')
                 .openPopup();
            ";

            // Ajouter les lieux
            if (places != null)
            {
                foreach (var place in places.Take(30))
                {
                    if (place.Location != null)
                    {
                        var emoji = GetPlaceEmoji(place);
                        var popupContent = $"{emoji} {place.Name}\\n{place.MainCategory}\\n{place.FormattedDistance}";
                        
                        markers += $@"
                            L.marker([{place.Location.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, 
                                     {place.Location.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}])
                             .addTo(map)
                             .bindPopup('{popupContent}');
                        ";
                    }
                }
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>TravelBuddy Map</title>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body {{ margin: 0; padding: 0; }}
        #map {{ height: 100vh; width: 100%; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var map = L.map('map').setView([{centerLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, 
                                       {centerLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}], 14);
        
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '© OpenStreetMap contributors'
        }}).addTo(map);
        
        {markers}
    </script>
</body>
</html>";
        }

        private string GetPlaceEmoji(Place place)
        {
            var category = place.MainCategory.ToLower();
            return category switch
            {
                var c when c.Contains("restaurant") => "🍽️",
                var c when c.Contains("café") => "☕",
                var c when c.Contains("musée") => "🏛️",
                var c when c.Contains("monument") => "🏛️",
                var c when c.Contains("parc") => "🌳",
                var c when c.Contains("hôtel") => "🏨",
                var c when c.Contains("commerce") => "🏪",
                var c when c.Contains("hôpital") => "🏥",
                var c when c.Contains("église") => "⛪",
                _ => "📌"
            };
        }

        private void OnCenterClicked(object? sender, EventArgs e)
        {
            if (_userLocation != null)
            {
                LoadMapHtml(_userLocation.Latitude, _userLocation.Longitude);
                _statusLabel.Text = "🎯 Carte centrée sur votre position";
            }
        }

        private void OnZoomInClicked(object? sender, EventArgs e)
        {
            _webView.EvaluateJavaScriptAsync("map.zoomIn();");
            _statusLabel.Text = "🔍 Zoom avant";
        }

        private void OnZoomOutClicked(object? sender, EventArgs e)
        {
            _webView.EvaluateJavaScriptAsync("map.zoomOut();");
            _statusLabel.Text = "🔍 Zoom arrière";
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