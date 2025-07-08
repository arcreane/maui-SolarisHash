using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views
{
    public class AdvancedInteractiveMapView : ContentView
    {
        private WebView _mapWebView;
        private Label _statusLabel;
        
        private IEnumerable<Place>? _places;
        private Location? _userLocation;

        public static readonly BindableProperty PlacesProperty = BindableProperty.Create(
            nameof(Places), 
            typeof(IEnumerable<Place>), 
            typeof(AdvancedInteractiveMapView), 
            null, 
            propertyChanged: OnPlacesChanged);

        public IEnumerable<Place>? Places
        {
            get => (IEnumerable<Place>?)GetValue(PlacesProperty);
            set => SetValue(PlacesProperty, value);
        }

        public AdvancedInteractiveMapView()
        {
            CreateMapInterface();
            LoadUserLocationAsync();
        }

        private void CreateMapInterface()
        {
            _mapWebView = new WebView
            {
                HeightRequest = 450,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            _statusLabel = new Label
            {
                Text = "🗺️ Chargement de la carte...",
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Blue,
                Padding = new Thickness(12, 8),
                BackgroundColor = Color.FromRgb(240, 248, 255)
            };

            var refreshButton = new Button
            {
                Text = "🔄 Actualiser la carte",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 14,
                Margin = new Thickness(16, 8)
            };
            refreshButton.Clicked += OnRefreshClicked;

            var mainLayout = new StackLayout
            {
                Spacing = 0,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Colors.White,
                        BorderColor = Colors.LightGray,
                        CornerRadius = 12,
                        Padding = 16,
                        Margin = 8,
                        HasShadow = true,
                        Content = new StackLayout
                        {
                            Children =
                            {
                                new Label
                                {
                                    Text = "🗺️ Carte Interactive",
                                    FontSize = 18,
                                    FontAttributes = FontAttributes.Bold,
                                    HorizontalTextAlignment = TextAlignment.Center,
                                    TextColor = Colors.DarkBlue,
                                    Margin = new Thickness(0, 0, 0, 12)
                                },
                                _statusLabel,
                                _mapWebView,
                                refreshButton
                            }
                        }
                    }
                }
            };

            Content = mainLayout;
        }

        private static void OnPlacesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is AdvancedInteractiveMapView mapView && newValue is IEnumerable<Place> places)
            {
                mapView.UpdateMapWithPlaces(places);
            }
        }

        private async void LoadUserLocationAsync()
        {
            try
            {
                _statusLabel.Text = "📍 Localisation en cours...";
                
                var locationService = GetService<ILocationService>();
                if (locationService != null)
                {
                    _userLocation = await locationService.GetCurrentLocationAsync();
                    if (_userLocation != null)
                    {
                        LoadMap(_userLocation.Latitude, _userLocation.Longitude);
                        _statusLabel.Text = $"✅ Carte prête • Position: {_userLocation.Latitude:F4}, {_userLocation.Longitude:F4}";
                    }
                    else
                    {
                        LoadMap(48.8566, 2.3522);
                        _statusLabel.Text = "⚠️ Position par défaut: Paris";
                    }
                }
                else
                {
                    LoadMap(48.8566, 2.3522);
                    _statusLabel.Text = "❌ Service indisponible • Carte Paris";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                LoadMap(48.8566, 2.3522);
            }
        }

        private void UpdateMapWithPlaces(IEnumerable<Place> places)
        {
            try
            {
                _places = places;
                
                if (_userLocation != null)
                {
                    LoadMap(_userLocation.Latitude, _userLocation.Longitude);
                }
                else
                {
                    LoadMap(48.8566, 2.3522);
                }
                
                var count = places.Count();
                _statusLabel.Text = count > 0 
                    ? $"✅ {count} lieux affichés sur la carte • Cliquez pour détails"
                    : "ℹ️ Aucun lieu à afficher";
                
                Console.WriteLine($"🗺️ Carte mise à jour avec {count} lieux");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur mise à jour: {ex.Message}";
                Console.WriteLine($"❌ Erreur UpdateMapWithPlaces: {ex.Message}");
            }
        }

        private void LoadMap(double lat, double lon)
        {
            try
            {
                var html = GenerateMapHtml(lat, lon, _places);
                var htmlSource = new HtmlWebViewSource { Html = html };
                _mapWebView.Source = htmlSource;
                
                Console.WriteLine($"🗺️ Carte chargée: {lat:F4}, {lon:F4} avec {_places?.Count() ?? 0} lieux");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur LoadMap: {ex.Message}");
            }
        }

        private string GenerateMapHtml(double centerLat, double centerLon, IEnumerable<Place>? places)
        {
            var userLat = centerLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var userLon = centerLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            
            var markers = GenerateMarkers(centerLat, centerLon, places);

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=yes'>
    <title>TravelBuddy</title>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body {{ margin: 0; padding: 0; font-family: Arial, sans-serif; }}
        #map {{ height: 100vh; width: 100%; }}
        .user-marker, .place-marker {{ background: transparent !important; border: none !important; }}
        .leaflet-popup-content-wrapper {{ border-radius: 8px; }}
        .leaflet-popup-content {{ margin: 12px; line-height: 1.4; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var map = L.map('map', {{
            center: [{userLat}, {userLon}],
            zoom: 14,
            zoomControl: true,
            scrollWheelZoom: true,
            doubleClickZoom: true,
            dragging: true,
            touchZoom: true
        }});
        
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }}).addTo(map);
        
        {markers}
        
        console.log('Carte chargée avec succès');
    </script>
</body>
</html>";
        }

        private string GenerateMarkers(double centerLat, double centerLon, IEnumerable<Place>? places)
        {
            var userLat = centerLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var userLon = centerLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            
            var markers = $@"
                var userIcon = L.divIcon({{
                    className: 'user-marker',
                    html: '<div style=""background: red; border: 3px solid white; border-radius: 50%; width: 18px; height: 18px; box-shadow: 0 2px 6px rgba(0,0,0,0.3);""></div>',
                    iconSize: [18, 18],
                    iconAnchor: [9, 9]
                }});
                
                L.marker([{userLat}, {userLon}], {{icon: userIcon}})
                 .addTo(map)
                 .bindPopup('<b>📍 Votre position</b>')
                 .openPopup();
            ";

            if (places != null && places.Any())
            {
                var colors = new[] { "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FECA57", "#FF9FF3", "#54A0FF" };
                var colorIndex = 0;

                foreach (var place in places.Take(30))
                {
                    if (place.Location != null)
                    {
                        var color = colors[colorIndex % colors.Length];
                        var emoji = GetPlaceEmoji(place);
                        var placeLat = place.Location.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                        var placeLon = place.Location.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                        
                        var safeName = EscapeForJs(place.Name);
                        var safeCategory = EscapeForJs(place.MainCategory);
                        var safeAddress = EscapeForJs(place.Address);
                        
                        markers += $@"
                            var placeIcon{colorIndex} = L.divIcon({{
                                className: 'place-marker',
                                html: '<div style=""background: {color}; border: 2px solid white; border-radius: 50%; width: 24px; height: 24px; display: flex; align-items: center; justify-content: center; cursor: pointer; box-shadow: 0 2px 6px rgba(0,0,0,0.3); font-size: 12px;"">{emoji}</div>',
                                iconSize: [24, 24],
                                iconAnchor: [12, 12]
                            }});
                            
                            L.marker([{placeLat}, {placeLon}], {{icon: placeIcon{colorIndex}}})
                             .addTo(map)
                             .bindPopup('<div style=""min-width: 200px;""><h4>{emoji} {safeName}</h4><p><strong>Type:</strong> {safeCategory}</p><p><strong>Distance:</strong> {place.FormattedDistance}</p><p style=""font-size: 12px; color: #666;"">{safeAddress}</p></div>');
                        ";
                        colorIndex++;
                    }
                }
            }

            return markers;
        }

        private void OnRefreshClicked(object? sender, EventArgs e)
        {
            try
            {
                if (_userLocation != null)
                {
                    LoadMap(_userLocation.Latitude, _userLocation.Longitude);
                    _statusLabel.Text = "🔄 Carte actualisée";
                }
                else
                {
                    LoadUserLocationAsync();
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur actualisation: {ex.Message}";
            }
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
                var c when c.Contains("tourisme") => "🗺️",
                _ => "📍"
            };
        }

        private string EscapeForJs(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
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