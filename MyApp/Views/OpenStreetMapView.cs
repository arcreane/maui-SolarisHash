using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views
{
    public class OpenStreetMapView : ContentView
    {
        private WebView _webView;
        private Label _statusLabel;
        private Label _selectedPlaceInfo;
        
        private IEnumerable<Place>? _places;
        private Location? _userLocation;

        public static readonly BindableProperty PlacesProperty = BindableProperty.Create(
            nameof(Places), 
            typeof(IEnumerable<Place>), 
            typeof(OpenStreetMapView), 
            null, 
            propertyChanged: OnPlacesChanged);

        public IEnumerable<Place>? Places
        {
            get => (IEnumerable<Place>?)GetValue(PlacesProperty);
            set => SetValue(PlacesProperty, value);
        }

        public OpenStreetMapView()
        {
            CreateMapInterface();
            LoadUserLocationAsync();
        }

        private void CreateMapInterface()
        {
            // WebView pour la carte OpenStreetMap
            _webView = new WebView
            {
                HeightRequest = 400,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            // Label de statut
            _statusLabel = new Label
            {
                Text = "🗺️ Chargement de la carte OpenStreetMap...",
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Blue,
                Padding = new Thickness(8, 4)
            };

            // Informations du lieu sélectionné
            _selectedPlaceInfo = new Label
            {
                Text = "📍 La carte va charger automatiquement avec vos lieux",
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

            var reloadButton = new Button
            {
                Text = "🔄 Recharger",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12
            };
            reloadButton.Clicked += OnReloadClicked;
            Grid.SetColumn(reloadButton, 1);
            controlsGrid.Children.Add(reloadButton);

            var fullscreenButton = new Button
            {
                Text = "🔍 Plein écran",
                BackgroundColor = Colors.Orange,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12
            };
            fullscreenButton.Clicked += OnFullscreenClicked;
            Grid.SetColumn(fullscreenButton, 2);
            controlsGrid.Children.Add(fullscreenButton);

            // Layout principal
            var mainLayout = new StackLayout
            {
                Spacing = 8,
                Padding = 12,
                Children =
                {
                    new Label
                    {
                        Text = "🗺️ Carte OpenStreetMap",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.DarkBlue
                    },
                    _statusLabel,
                    _webView,
                    controlsGrid,
                    _selectedPlaceInfo,
                    new Frame
                    {
                        BackgroundColor = Color.FromRgb(230, 255, 230),
                        BorderColor = Colors.Green,
                        CornerRadius = 8,
                        Padding = 8,
                        Content = new Label
                        {
                            Text = "✅ Carte 100% gratuite • Pas de clé API nécessaire • Données en temps réel",
                            FontSize = 10,
                            TextColor = Colors.DarkGreen,
                            HorizontalTextAlignment = TextAlignment.Center,
                            FontAttributes = FontAttributes.Bold
                        }
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
            if (bindable is OpenStreetMapView mapView && newValue is IEnumerable<Place> places)
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
                        LoadMapHtml(_userLocation.Latitude, _userLocation.Longitude);
                        _statusLabel.Text = $"✅ Carte centrée sur {_userLocation.Latitude:F4}, {_userLocation.Longitude:F4}";
                    }
                    else
                    {
                        LoadMapHtml(48.8566, 2.3522); // Paris par défaut
                        _statusLabel.Text = "⚠️ Position non disponible - Carte centrée sur Paris";
                    }
                }
                else
                {
                    LoadMapHtml(48.8566, 2.3522);
                    _statusLabel.Text = "❌ Service de localisation indisponible - Affichage Paris";
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
            try
            {
                var html = GenerateOpenStreetMapHtml(lat, lon, _places);
                var htmlSource = new HtmlWebViewSource { Html = html };
                _webView.Source = htmlSource;
                Console.WriteLine($"🗺️ Carte chargée pour {lat:F4}, {lon:F4}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur chargement carte: {ex.Message}");
                _statusLabel.Text = "❌ Erreur chargement carte";
            }
        }

        private void UpdateMapWithPlaces(IEnumerable<Place> places)
        {
            try
            {
                _places = places;
                
                if (_userLocation != null)
                {
                    LoadMapHtml(_userLocation.Latitude, _userLocation.Longitude);
                    _statusLabel.Text = $"✅ {places.Count()} lieux affichés sur OpenStreetMap";
                }
                else
                {
                    LoadMapHtml(48.8566, 2.3522);
                    _statusLabel.Text = $"✅ {places.Count()} lieux affichés (Paris par défaut)";
                }
                
                // Mettre à jour les infos
                if (places.Any())
                {
                    _selectedPlaceInfo.Text = $"📍 {places.Count()} lieux trouvés • Cliquez sur les pins pour plus d'infos • Utilisez les gestes tactiles pour naviguer";
                    _selectedPlaceInfo.BackgroundColor = Color.FromRgb(230, 255, 230);
                }
                
                Console.WriteLine($"🗺️ OpenStreetMap mise à jour avec {places.Count()} lieux");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur mise à jour: {ex.Message}";
                Console.WriteLine($"❌ Erreur mise à jour carte: {ex.Message}");
            }
        }

        private string GenerateOpenStreetMapHtml(double centerLat, double centerLon, IEnumerable<Place>? places)
        {
            var markers = "";
            
            // Ajouter la position utilisateur avec une icône spéciale
            markers += $@"
                var userIcon = L.divIcon({{
                    className: 'user-marker',
                    html: '<div style=""background: red; border: 3px solid white; border-radius: 50%; width: 20px; height: 20px; box-shadow: 0 2px 6px rgba(0,0,0,0.3);""></div>',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10]
                }});
                
                L.marker([{centerLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, 
                         {centerLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}], {{icon: userIcon}})
                 .addTo(map)
                 .bindPopup('<b>📍 Votre position</b><br>Latitude: {centerLat:F6}<br>Longitude: {centerLon:F6}')
                 .openPopup();
            ";

            // Ajouter les lieux avec des icônes colorées
            if (places != null)
            {
                var colors = new[] { "blue", "green", "orange", "purple", "red", "darkgreen", "cadetblue", "darkred", "lightgreen", "beige" };
                var colorIndex = 0;

                foreach (var place in places.Take(50)) // Maximum 50 pins pour les performances
                {
                    if (place.Location != null)
                    {
                        var color = colors[colorIndex % colors.Length];
                        colorIndex++;
                        
                        var emoji = GetPlaceEmoji(place);
                        var popupContent = $@"
                            <div style='min-width: 200px;'>
                                <h4 style='margin: 0 0 8px 0; color: #333;'>{emoji} {EscapeForHtml(place.Name)}</h4>
                                <p style='margin: 4px 0; color: #666;'><strong>Catégorie:</strong> {EscapeForHtml(place.MainCategory)}</p>
                                <p style='margin: 4px 0; color: #666;'><strong>Distance:</strong> {place.FormattedDistance}</p>
                                <p style='margin: 4px 0; color: #666; font-size: 12px;'>{EscapeForHtml(place.Address)}</p>
                            </div>
                        ";
                        
                        markers += $@"
                            var icon{colorIndex} = L.divIcon({{
                                className: 'place-marker',
                                html: '<div style=""background: {color}; border: 2px solid white; border-radius: 50%; width: 16px; height: 16px; box-shadow: 0 1px 3px rgba(0,0,0,0.4); display: flex; align-items: center; justify-content: center; font-size: 10px;"">{emoji}</div>',
                                iconSize: [16, 16],
                                iconAnchor: [8, 8]
                            }});
                            
                            L.marker([{place.Location.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, 
                                     {place.Location.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}], {{icon: icon{colorIndex}}})
                             .addTo(map)
                             .bindPopup(`{popupContent}`);
                        ";
                    }
                }
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=yes'>
    <title>TravelBuddy - OpenStreetMap</title>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' 
          integrity='sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=' crossorigin=''/>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'
            integrity='sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo=' crossorigin=''></script>
    <style>
        body {{ 
            margin: 0; 
            padding: 0; 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
        }}
        #map {{ 
            height: 100vh; 
            width: 100%; 
        }}
        .user-marker {{
            background: transparent !important;
            border: none !important;
        }}
        .place-marker {{
            background: transparent !important;
            border: none !important;
        }}
        .leaflet-popup-content-wrapper {{
            border-radius: 8px;
            box-shadow: 0 3px 14px rgba(0,0,0,0.4);
        }}
        .leaflet-popup-content {{
            margin: 12px 16px;
            line-height: 1.4;
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        // Initialiser la carte OpenStreetMap
        var map = L.map('map', {{
            center: [{centerLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, 
                    {centerLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}],
            zoom: 14,
            zoomControl: true,
            scrollWheelZoom: true,
            doubleClickZoom: true,
            dragging: true
        }});
        
        // Ajouter les tuiles OpenStreetMap (gratuit, pas d'API key)
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '© <a href=""https://www.openstreetmap.org/copyright"">OpenStreetMap</a> contributors',
            maxZoom: 19,
            subdomains: 'abc'
        }}).addTo(map);
        
        // Ajouter une couche satellite alternative (optionnel)
        var satellite = L.tileLayer('https://{{s}}.tile.opentopomap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '© <a href=""https://opentopomap.org"">OpenTopoMap</a> contributors',
            maxZoom: 17
        }});
        
        // Contrôle des couches
        var baseMaps = {{
            ""Carte"": L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png'),
            ""Topo"": satellite
        }};
        
        L.control.layers(baseMaps).addTo(map);
        
        // Ajouter les marqueurs
        {markers}
        
        // Fonctions utilitaires
        map.on('click', function(e) {{
            console.log('Carte cliquée à: ' + e.latlng);
        }});
        
        // Ajuster la vue pour inclure tous les marqueurs si nécessaire
        setTimeout(function() {{
            try {{
                if (map.getBounds && Object.keys(map._layers).length > 2) {{
                    map.fitBounds(map.getBounds(), {{padding: [20, 20]}});
                }}
            }} catch(e) {{
                console.log('Erreur ajustement vue:', e);
            }}
        }}, 1000);
        
        console.log('OpenStreetMap chargée avec succès');
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
                var c when c.Contains("tourisme") => "🗺️",
                _ => "📌"
            };
        }

        private string EscapeForHtml(string text)
        {
            return text.Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }

        private void OnCenterClicked(object? sender, EventArgs e)
        {
            if (_userLocation != null)
            {
                LoadMapHtml(_userLocation.Latitude, _userLocation.Longitude);
                _statusLabel.Text = "🎯 Carte centrée sur votre position";
            }
            else
            {
                _statusLabel.Text = "⚠️ Position non disponible pour centrage";
            }
        }

        private void OnReloadClicked(object? sender, EventArgs e)
        {
            try
            {
                if (_userLocation != null)
                {
                    LoadMapHtml(_userLocation.Latitude, _userLocation.Longitude);
                }
                else
                {
                    LoadMapHtml(48.8566, 2.3522);
                }
                _statusLabel.Text = "🔄 Carte rechargée";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur rechargement: {ex.Message}";
            }
        }

        private async void OnFullscreenClicked(object? sender, EventArgs e)
        {
            try
            {
                await _webView.EvaluateJavaScriptAsync("map.invalidateSize(); map.fitWorld();");
                _statusLabel.Text = "🔍 Vue ajustée";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
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