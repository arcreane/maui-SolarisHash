using MyApp.Models;
using MyApp.Services;
using Microsoft.Maui.Media;

namespace MyApp.Views
{
    public class AdvancedInteractiveMapView : ContentView
    {
        private WebView _mapWebView;
        private Label _statusLabel;
        private Frame _placeDetailsPanel;
        private StackLayout _placeDetailsContent;
        private Button _navigationButton;
        private Button _takePhotoButton;
        private Button _viewPhotosButton;
        private ScrollView _photosScrollView;
        private StackLayout _photosContainer;
        
        private IEnumerable<Place>? _places;
        private Location? _userLocation;
        private Place? _selectedPlace;
        private readonly Dictionary<string, List<PlacePhoto>> _placePhotos = new();

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
            CreateAdvancedMapInterface();
            LoadUserLocationAsync();
            InitializePlacePhotos();
        }

        private void CreateAdvancedMapInterface()
        {
            // WebView pour la carte avec zoom tactile
            _mapWebView = new WebView
            {
                HeightRequest = 400,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            // Label de statut
            _statusLabel = new Label
            {
                Text = "🗺️ Chargement de la carte interactive...",
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Blue,
                Padding = new Thickness(8, 4)
            };

            // Panel des détails du lieu (initialement masqué)
            CreatePlaceDetailsPanel();

            // Layout principal
            var mainLayout = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto }, // Status
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, // Map
                    new RowDefinition { Height = GridLength.Auto } // Details panel
                }
            };

            // Ajouter les éléments au Grid
            Grid.SetRow(_statusLabel, 0);
            mainLayout.Children.Add(_statusLabel);

            Grid.SetRow(_mapWebView, 1);
            mainLayout.Children.Add(_mapWebView);

            Grid.SetRow(_placeDetailsPanel, 2);
            mainLayout.Children.Add(_placeDetailsPanel);

            Content = mainLayout;
        }

        private void CreatePlaceDetailsPanel()
        {
            // Container pour les photos
            _photosContainer = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 8
            };

            _photosScrollView = new ScrollView
            {
                Content = _photosContainer,
                Orientation = ScrollOrientation.Horizontal,
                HeightRequest = 80,
                IsVisible = false
            };

            // Boutons d'action
            _navigationButton = new Button
            {
                Text = "🧭 Itinéraire",
                BackgroundColor = Colors.Blue,
                TextColor = Colors.White,
                CornerRadius = 20,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            _navigationButton.Clicked += OnNavigationClicked;

            _takePhotoButton = new Button
            {
                Text = "📷 Prendre photo",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 20,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            _takePhotoButton.Clicked += OnTakePhotoClicked;

            _viewPhotosButton = new Button
            {
                Text = "🖼️ Voir photos (0)",
                BackgroundColor = Colors.Orange,
                TextColor = Colors.White,
                CornerRadius = 20,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            _viewPhotosButton.Clicked += OnViewPhotosClicked;

            // Contenu des détails
            _placeDetailsContent = new StackLayout
            {
                Spacing = 12,
                Padding = 16
            };

            // Panel principal (masqué par défaut)
            _placeDetailsPanel = new Frame
            {
                BackgroundColor = Colors.White,
                BorderColor = Colors.LightGray,
                CornerRadius = 16,
                Padding = 0,
                Margin = new Thickness(8, 0, 8, 8),
                HasShadow = true,
                IsVisible = false,
                Content = new StackLayout
                {
                    Children =
                    {
                        // Header avec bouton fermer
                        new Grid
                        {
                            BackgroundColor = Color.FromRgb(240, 248, 255),
                            Padding = new Thickness(16, 12),
                            ColumnDefinitions = new ColumnDefinitionCollection
                            {
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = GridLength.Auto }
                            },
                            Children =
                            {
                                new Label
                                {
                                    Text = "📍 Détails du lieu",
                                    FontSize = 16,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Colors.DarkBlue,
                                    VerticalOptions = LayoutOptions.Center
                                }.Row(0).Column(0),
                                new Button
                                {
                                    Text = "✕",
                                    BackgroundColor = Colors.Transparent,
                                    TextColor = Colors.Gray,
                                    FontSize = 18,
                                    WidthRequest = 40,
                                    HeightRequest = 40,
                                    CornerRadius = 20,
                                    Command = new Command(HidePlaceDetails)
                                }.Row(0).Column(1)
                            }
                        },
                        _placeDetailsContent,
                        _photosScrollView,
                        new Grid
                        {
                            ColumnDefinitions = new ColumnDefinitionCollection
                            {
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                            },
                            ColumnSpacing = 8,
                            Padding = new Thickness(16, 0, 16, 16),
                            Children =
                            {
                                _navigationButton.Column(0),
                                _takePhotoButton.Column(1),
                                _viewPhotosButton.Column(2)
                            }
                        }
                    }
                }
            };
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
                        LoadInteractiveMap(_userLocation.Latitude, _userLocation.Longitude);
                        _statusLabel.Text = $"✅ Carte centrée • Zoom tactile activé";
                    }
                    else
                    {
                        LoadInteractiveMap(48.8566, 2.3522);
                        _statusLabel.Text = "⚠️ Position par défaut (Paris) • Zoom tactile activé";
                    }
                }
                else
                {
                    LoadInteractiveMap(48.8566, 2.3522);
                    _statusLabel.Text = "❌ Service indisponible • Carte Paris • Zoom tactile activé";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                LoadInteractiveMap(48.8566, 2.3522);
            }
        }

        private void LoadInteractiveMap(double lat, double lon)
        {
            try
            {
                var html = GenerateAdvancedMapHtml(lat, lon, _places);
                var htmlSource = new HtmlWebViewSource { Html = html };
                _mapWebView.Source = htmlSource;
                
                Console.WriteLine($"🗺️ Carte interactive chargée pour {lat:F4}, {lon:F4}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur chargement carte: {ex.Message}");
            }
        }

        private void UpdateMapWithPlaces(IEnumerable<Place> places)
        {
            try
            {
                _places = places;
                
                if (_userLocation != null)
                {
                    LoadInteractiveMap(_userLocation.Latitude, _userLocation.Longitude);
                }
                else
                {
                    LoadInteractiveMap(48.8566, 2.3522);
                }
                
                _statusLabel.Text = $"✅ {places.Count()} lieux sur la carte • Cliquez pour détails";
                Console.WriteLine($"🗺️ Carte mise à jour avec {places.Count()} lieux cliquables");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur mise à jour: {ex.Message}";
            }
        }

        private string GenerateAdvancedMapHtml(double centerLat, double centerLon, IEnumerable<Place>? places)
        {
            var markers = "";
            var userLat = centerLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var userLon = centerLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            
            // Position utilisateur
            markers += $@"
                var userIcon = L.divIcon({{
                    className: 'user-marker',
                    html: '<div style=""background: #007AFF; border: 3px solid white; border-radius: 50%; width: 16px; height: 16px; box-shadow: 0 2px 8px rgba(0,122,255,0.4); animation: pulse 2s infinite;""></div>',
                    iconSize: [16, 16],
                    iconAnchor: [8, 8]
                }});
                
                L.marker([{userLat}, {userLon}], {{icon: userIcon}})
                 .addTo(map)
                 .bindPopup('<div style=""text-align: center; padding: 8px;""><b>📍 Votre position</b><br><span style=""color: #666; font-size: 12px;"">Lat: {centerLat:F6}<br>Lon: {centerLon:F6}</span></div>')
                 .openPopup();
            ";

            // Lieux avec interaction avancée
            if (places != null)
            {
                var placeIndex = 0;
                foreach (var place in places.Take(50))
                {
                    if (place.Location != null)
                    {
                        var emoji = GetPlaceEmoji(place);
                        var color = GetPlaceColor(place);
                        var placeLat = place.Location.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                        var placeLon = place.Location.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                        
                        var safeDescription = EscapeForJs(place.Description);
                        var safeName = EscapeForJs(place.Name);
                        var safeAddress = EscapeForJs(place.Address);
                        var safeCategory = EscapeForJs(place.MainCategory);
                        
                        // Horaires simulés (vous pourriez les récupérer de vraies APIs)
                        var openingHours = GetSimulatedOpeningHours(place.MainCategory);
                        
                        markers += $@"
                            var placeIcon{placeIndex} = L.divIcon({{
                                className: 'place-marker',
                                html: '<div onclick=""selectPlace({{id: \\'{place.Id}\\', name: \\'{safeName}\\', category: \\'{safeCategory}\\', address: \\'{safeAddress}\\', description: \\'{safeDescription}\\', distance: \\'{place.FormattedDistance}\\', hours: \\'{openingHours}\\', lat: {placeLat}, lon: {placeLon}}})"" style=""background: {color}; border: 2px solid white; border-radius: 50%; width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; cursor: pointer; box-shadow: 0 2px 6px rgba(0,0,0,0.3); transition: transform 0.2s;"" onmouseover=""this.style.transform=\\\"scale(1.2)\\\";"" onmouseout=""this.style.transform=\\\"scale(1)\\\";""><span style=""font-size: 12px;"">{emoji}</span></div>',
                                iconSize: [20, 20],
                                iconAnchor: [10, 10]
                            }});
                            
                            var marker{placeIndex} = L.marker([{placeLat}, {placeLon}], {{icon: placeIcon{placeIndex}}})
                             .addTo(map);
                        ";
                        placeIndex++;
                    }
                }
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=yes, initial-scale=1.0, maximum-scale=3.0, minimum-scale=0.5'>
    <title>TravelBuddy - Carte Interactive</title>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body {{ 
            margin: 0; 
            padding: 0; 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            touch-action: manipulation;
        }}
        #map {{ 
            height: 100vh; 
            width: 100%; 
            touch-action: pan-x pan-y;
        }}
        .user-marker, .place-marker {{
            background: transparent !important;
            border: none !important;
        }}
        @keyframes pulse {{
            0% {{ box-shadow: 0 0 0 0 rgba(0,122,255,0.7); }}
            70% {{ box-shadow: 0 0 0 10px rgba(0,122,255,0); }}
            100% {{ box-shadow: 0 0 0 0 rgba(0,122,255,0); }}
        }}
        .leaflet-popup-content-wrapper {{
            border-radius: 12px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.15);
        }}
        .leaflet-popup-content {{
            margin: 16px;
            line-height: 1.4;
        }}
        .leaflet-control-zoom {{
            border: none !important;
            box-shadow: 0 2px 10px rgba(0,0,0,0.2) !important;
        }}
        .leaflet-control-zoom a {{
            background-color: white !important;
            color: #333 !important;
            border: none !important;
            width: 36px !important;
            height: 36px !important;
            line-height: 36px !important;
            font-size: 18px !important;
            font-weight: bold !important;
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        // Carte avec zoom tactile complet
        var map = L.map('map', {{
            center: [{userLat}, {userLon}],
            zoom: 15,
            zoomControl: true,
            scrollWheelZoom: true,
            doubleClickZoom: true,
            boxZoom: true,
            dragging: true,
            touchZoom: true,
            minZoom: 8,
            maxZoom: 19
        }});
        
        // Tiles OpenStreetMap
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19,
            subdomains: 'abc'
        }}).addTo(map);
        
        // Fonction de sélection de lieu (communication avec MAUI)
        function selectPlace(place) {{
            try {{
                console.log('Lieu sélectionné:', place);
                
                // Envoyer les données à MAUI via postMessage
                if (window.chrome && window.chrome.webview) {{
                    // Edge WebView2
                    window.chrome.webview.postMessage(JSON.stringify({{
                        type: 'placeSelected',
                        data: place
                    }}));
                }} else if (window.external && window.external.notify) {{
                    // Android WebView
                    window.external.notify(JSON.stringify({{
                        type: 'placeSelected',
                        data: place
                    }}));
                }} else {{
                    // Fallback - URL navigation
                    window.location.href = 'travelbuddy://placeSelected?data=' + encodeURIComponent(JSON.stringify(place));
                }}
                
                // Centrer la carte sur le lieu
                map.setView([place.lat, place.lon], 17, {{animate: true}});
                
            }} catch(e) {{
                console.error('Erreur sélection lieu:', e);
            }}
        }}
        
        // Ajouter les marqueurs
        {markers}
        
        // Gestion des événements tactiles améliorée
        map.on('zoomend', function() {{
            console.log('Zoom niveau:', map.getZoom());
        }});
        
        map.on('moveend', function() {{
            var center = map.getCenter();
            console.log('Carte déplacée vers:', center.lat, center.lng);
        }});
        
        // Désactiver le menu contextuel sur mobile
        map.on('contextmenu', function(e) {{
            e.originalEvent.preventDefault();
        }});
        
        console.log('Carte interactive chargée avec zoom tactile');
    </script>
</body>
</html>";
        }

        private async void OnNavigationClicked(object? sender, EventArgs e)
        {
            if (_selectedPlace?.Location == null || _userLocation == null) return;

            try
            {
                // Ouvrir Google Maps pour navigation
                var googleMapsUrl = $"https://www.google.com/maps/dir/{_userLocation.Latitude},{_userLocation.Longitude}/{_selectedPlace.Location.Latitude},{_selectedPlace.Location.Longitude}";
                
                await Launcher.OpenAsync(googleMapsUrl);
                _statusLabel.Text = "🧭 Navigation ouverte dans Google Maps";
            }
            catch (Exception ex)
            {
                await ShowAlert("Erreur", $"Impossible d'ouvrir la navigation: {ex.Message}");
            }
        }

        private async void OnTakePhotoClicked(object? sender, EventArgs e)
        {
            if (_selectedPlace == null) return;

            try
            {
                _statusLabel.Text = "📸 Ouverture de l'appareil photo...";

                var cameraPermission = await Permissions.RequestAsync<Permissions.Camera>();
                if (cameraPermission != PermissionStatus.Granted)
                {
                    await ShowAlert("Permission requise", "L'accès à l'appareil photo est nécessaire.");
                    return;
                }

                var photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = $"Photo de {_selectedPlace.Name}"
                });

                if (photo != null)
                {
                    // Sauvegarder la photo
                    var savedPhoto = await SavePlacePhoto(photo, _selectedPlace);
                    
                    if (savedPhoto != null)
                    {
                        // Ajouter à la galerie du lieu
                        if (!_placePhotos.ContainsKey(_selectedPlace.Id))
                            _placePhotos[_selectedPlace.Id] = new List<PlacePhoto>();
                        
                        _placePhotos[_selectedPlace.Id].Add(savedPhoto);
                        
                        // Mettre à jour l'interface
                        UpdatePhotosDisplay(_selectedPlace);
                        _statusLabel.Text = $"✅ Photo ajoutée à la galerie de {_selectedPlace.Name}";
                        
                        await ShowAlert("Photo sauvegardée", $"Votre photo de {_selectedPlace.Name} a été ajoutée à la galerie !");
                    }
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur photo: {ex.Message}";
                await ShowAlert("Erreur", $"Impossible de prendre la photo: {ex.Message}");
            }
        }

        private async void OnViewPhotosClicked(object? sender, EventArgs e)
        {
            if (_selectedPlace == null) return;

            try
            {
                if (_placePhotos.ContainsKey(_selectedPlace.Id) && _placePhotos[_selectedPlace.Id].Any())
                {
                    _photosScrollView.IsVisible = !_photosScrollView.IsVisible;
                    _viewPhotosButton.Text = _photosScrollView.IsVisible ? "🔼 Masquer photos" : $"🖼️ Voir photos ({_placePhotos[_selectedPlace.Id].Count})";
                }
                else
                {
                    await ShowAlert("Pas de photos", $"Aucune photo n'a encore été prise pour {_selectedPlace.Name}. Soyez le premier à partager !");
                }
            }
            catch (Exception ex)
            {
                await ShowAlert("Erreur", $"Erreur affichage photos: {ex.Message}");
            }
        }

        private async Task<PlacePhoto?> SavePlacePhoto(FileResult photo, Place place)
        {
            try
            {
                // Créer le dossier pour les photos de lieux
                var photosDirectory = Path.Combine(FileSystem.AppDataDirectory, "PlacePhotos", place.Id);
                Directory.CreateDirectory(photosDirectory);

                var fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine(photosDirectory, fileName);

                // Copier la photo
                using var sourceStream = await photo.OpenReadAsync();
                using var destinationStream = File.Create(filePath);
                await sourceStream.CopyToAsync(destinationStream);

                return new PlacePhoto
                {
                    FilePath = filePath,
                    PlaceId = place.Id,
                    PlaceName = place.Name,
                    Timestamp = DateTime.Now,
                    Location = _userLocation
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur sauvegarde photo: {ex.Message}");
                return null;
            }
        }

        private void ShowPlaceDetails(Place place)
        {
            try
            {
                _selectedPlace = place;
                
                // Contenu des détails
                _placeDetailsContent.Children.Clear();
                
                // Nom et catégorie
                _placeDetailsContent.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Children =
                    {
                        new Label
                        {
                            Text = GetPlaceEmoji(place),
                            FontSize = 32,
                            VerticalOptions = LayoutOptions.Center
                        },
                        new StackLayout
                        {
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            Children =
                            {
                                new Label
                                {
                                    Text = place.Name,
                                    FontSize = 18,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Colors.Black
                                },
                                new Label
                                {
                                    Text = place.MainCategory,
                                    FontSize = 14,
                                    TextColor = Colors.Blue,
                                    FontAttributes = FontAttributes.Bold
                                }
                            }
                        },
                        new Frame
                        {
                            BackgroundColor = Colors.LightGreen,
                            BorderColor = Colors.Green,
                            CornerRadius = 12,
                            Padding = new Thickness(8, 4),
                            Content = new Label
                            {
                                Text = place.FormattedDistance,
                                FontSize = 12,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.DarkGreen
                            }
                        }
                    }
                });
                
                // Adresse
                _placeDetailsContent.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Children =
                    {
                        new Label { Text = "📍", FontSize = 16 },
                        new Label
                        {
                            Text = place.Address,
                            FontSize = 13,
                            TextColor = Colors.Gray,
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        }
                    }
                });
                
                // Horaires
                var openingHours = GetSimulatedOpeningHours(place.MainCategory);
                _placeDetailsContent.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Children =
                    {
                        new Label { Text = "🕐", FontSize = 16 },
                        new Label
                        {
                            Text = openingHours,
                            FontSize = 13,
                            TextColor = Colors.Gray,
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        }
                    }
                });
                
                // Description si disponible
                if (!string.IsNullOrEmpty(place.Description))
                {
                    var cleanDescription = place.Description.Replace("[RÉEL]", "").Replace("[DÉMO]", "").Trim();
                    if (!string.IsNullOrEmpty(cleanDescription))
                    {
                        _placeDetailsContent.Children.Add(new Label
                        {
                            Text = cleanDescription,
                            FontSize = 13,
                            TextColor = Colors.Black,
                            Margin = new Thickness(0, 8, 0, 0)
                        });
                    }
                }
                
                // Mettre à jour l'affichage des photos
                UpdatePhotosDisplay(place);
                
                // Afficher le panel
                _placeDetailsPanel.IsVisible = true;
                _statusLabel.Text = $"📍 {place.Name} • {place.FormattedDistance}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur affichage détails: {ex.Message}");
            }
        }

        private void UpdatePhotosDisplay(Place place)
        {
            try
            {
                _photosContainer.Children.Clear();
                
                if (_placePhotos.ContainsKey(place.Id))
                {
                    var photos = _placePhotos[place.Id];
                    _viewPhotosButton.Text = $"🖼️ Voir photos ({photos.Count})";
                    
                    foreach (var photo in photos.Take(5)) // Max 5 photos dans l'aperçu
                    {
                        var photoFrame = new Frame
                        {
                            WidthRequest = 70,
                            HeightRequest = 70,
                            CornerRadius = 8,
                            Padding = 2,
                            BackgroundColor = Colors.LightGray,
                            Content = new Image
                            {
                                Source = ImageSource.FromFile(photo.FilePath),
                                Aspect = Aspect.AspectFill
                            }
                        };
                        
                        var tapGesture = new TapGestureRecognizer();
                        tapGesture.Tapped += async (s, e) => await ShowFullPhoto(photo);
                        photoFrame.GestureRecognizers.Add(tapGesture);
                        
                        _photosContainer.Children.Add(photoFrame);
                    }
                    
                    if (photos.Count > 0)
                    {
                        _photosScrollView.IsVisible = true;
                    }
                }
                else
                {
                    _viewPhotosButton.Text = "🖼️ Voir photos (0)";
                    _photosScrollView.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur affichage photos: {ex.Message}");
            }
        }

        private async Task ShowFullPhoto(PlacePhoto photo)
        {
            try
            {
                var photoPage = new ContentPage
                {
                    Title = photo.PlaceName,
                    Content = new StackLayout
                    {
                        Children =
                        {
                            new ScrollView
                            {
                                Content = new Image
                                {
                                    Source = ImageSource.FromFile(photo.FilePath),
                                    Aspect = Aspect.AspectFit
                                }
                            },
                            new Label