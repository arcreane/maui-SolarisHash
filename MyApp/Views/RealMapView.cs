using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MyApp.Models;
using MyApp.Services;
using Map = Microsoft.Maui.Controls.Maps.Map; // ✅ CORRECTION: Résoudre l'ambiguïté

namespace MyApp.Views
{
    public class RealMapView : ContentView
    {
        // ✅ CORRECTION: Enlever readonly pour pouvoir les assigner
        private Map _map;
        private Label _selectedPlaceInfo;
        private Label _statusLabel;
        private readonly Dictionary<Place, Pin> _placePins = new();
        
        private IEnumerable<Place>? _places;
        private Location? _userLocation;

        public static readonly BindableProperty PlacesProperty = BindableProperty.Create(
            nameof(Places), 
            typeof(IEnumerable<Place>), 
            typeof(RealMapView), 
            null, 
            propertyChanged: OnPlacesChanged);

        public IEnumerable<Place>? Places
        {
            get => (IEnumerable<Place>?)GetValue(PlacesProperty);
            set => SetValue(PlacesProperty, value);
        }

        public RealMapView()
        {
            CreateRealMap();
            LoadUserLocationAsync();
        }

        private void CreateRealMap()
        {
            // ✅ VRAIE CARTE avec Microsoft.Maui.Controls.Maps
            _map = new Map
            {
                HeightRequest = 400,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                MapType = MapType.Street, // Carte routière réelle
                IsScrollEnabled = true,
                IsZoomEnabled = true,
                IsShowingUser = true // Afficher la position utilisateur
            };

            // Événements de la carte
            _map.MapClicked += OnMapClicked;

            // Label de statut
            _statusLabel = new Label
            {
                Text = "🗺️ Chargement de la carte réelle...",
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

            var streetButton = new Button
            {
                Text = "🛣️ Route",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12
            };
            streetButton.Clicked += (s, e) => _map.MapType = MapType.Street;
            Grid.SetColumn(streetButton, 1);
            controlsGrid.Children.Add(streetButton);

            var satelliteButton = new Button
            {
                Text = "🛰️ Satellite",
                BackgroundColor = Colors.Orange,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12
            };
            satelliteButton.Clicked += (s, e) => _map.MapType = MapType.Satellite;
            Grid.SetColumn(satelliteButton, 2);
            controlsGrid.Children.Add(satelliteButton);

            var hybridButton = new Button
            {
                Text = "🗺️ Hybride",
                BackgroundColor = Colors.Purple,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 12
            };
            hybridButton.Clicked += (s, e) => _map.MapType = MapType.Hybrid;
            Grid.SetColumn(hybridButton, 3);
            controlsGrid.Children.Add(hybridButton);

            // Layout principal
            var mainLayout = new StackLayout
            {
                Spacing = 8,
                Padding = 12,
                Children =
                {
                    new Label
                    {
                        Text = "🗺️ Carte Réelle Interactive",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.DarkBlue
                    },
                    _statusLabel,
                    _map, // ✅ VRAIE CARTE ICI
                    controlsGrid,
                    _selectedPlaceInfo,
                    new Label
                    {
                        Text = "💡 Carte réelle avec données OpenStreetMap • Zoom avec pincement • Déplacement tactile",
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
            if (bindable is RealMapView mapView && newValue is IEnumerable<Place> places)
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
                        // ✅ CENTRER LA VRAIE CARTE sur la position utilisateur
                        var mapSpan = MapSpan.FromCenterAndRadius(
                            new Microsoft.Maui.Devices.Sensors.Location(_userLocation.Latitude, _userLocation.Longitude),
                            Distance.FromKilometers(2) // Rayon de 2km
                        );
                        _map.MoveToRegion(mapSpan);
                        
                        _statusLabel.Text = $"✅ Carte centrée sur {_userLocation.Latitude:F4}, {_userLocation.Longitude:F4}";
                    }
                    else
                    {
                        // Position par défaut : Paris
                        var parisSpan = MapSpan.FromCenterAndRadius(
                            new Microsoft.Maui.Devices.Sensors.Location(48.8566, 2.3522),
                            Distance.FromKilometers(5)
                        );
                        _map.MoveToRegion(parisSpan);
                        _statusLabel.Text = "⚠️ Position non disponible - Carte centrée sur Paris";
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

        private void UpdateMapWithPlaces(IEnumerable<Place> places)
        {
            try
            {
                _places = places;
                
                // ✅ NETTOYER les anciens pins de la VRAIE CARTE
                foreach (var pin in _placePins.Values)
                {
                    _map.Pins.Remove(pin);
                }
                _placePins.Clear();

                if (!places.Any()) 
                {
                    _statusLabel.Text = "⚠️ Aucun lieu à afficher sur la carte";
                    return;
                }

                // ✅ AJOUTER les lieux comme VRAIS PINS sur la carte
                int pinsAdded = 0;
                foreach (var place in places.Take(50)) // Jusqu'à 50 pins
                {
                    if (place.Location != null)
                    {
                        CreateRealPin(place);
                        pinsAdded++;
                    }
                }

                _statusLabel.Text = $"✅ {pinsAdded} lieux affichés sur la carte réelle";
                Console.WriteLine($"🗺️ Carte réelle mise à jour avec {pinsAdded} pins");
                
                // ✅ AJUSTER la vue pour inclure tous les pins
                if (pinsAdded > 0)
                {
                    AdjustMapViewToIncludeAllPins();
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur carte: {ex.Message}";
                Console.WriteLine($"❌ Erreur mise à jour carte: {ex.Message}");
            }
        }

        private void CreateRealPin(Place place)
        {
            try
            {
                // ✅ CRÉER un VRAI PIN sur la carte
                var pin = new Pin
                {
                    Label = place.Name,
                    Address = place.Address,
                    Type = PinType.Place,
                    Location = new Microsoft.Maui.Devices.Sensors.Location(
                        place.Location.Latitude, 
                        place.Location.Longitude)
                };

                // Personnaliser l'apparence selon la catégorie
                var category = place.MainCategory.ToLower();
                if (category.Contains("restaurant") || category.Contains("café"))
                {
                    pin.Type = PinType.Place; // Orange par défaut
                }

                // Événement de clic sur le pin
                pin.MarkerClicked += (s, e) => OnPinClicked(place, e);

                // ✅ AJOUTER le pin à la VRAIE CARTE
                _map.Pins.Add(pin);
                _placePins[place] = pin;

                Console.WriteLine($"📌 Pin réel créé pour {place.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur création pin pour {place.Name}: {ex.Message}");
            }
        }

        private void OnPinClicked(Place place, PinClickedEventArgs e)
        {
            try
            {
                e.HideInfoWindow = false; // Laisser l'info window s'afficher

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

                Console.WriteLine($"🎯 Pin réel sélectionné: {place.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur sélection pin: {ex.Message}");
            }
        }

        private void OnMapClicked(object? sender, MapClickedEventArgs e)
        {
            // Réinitialiser l'info quand on clique sur la carte
            _selectedPlaceInfo.Text = "📍 Cliquez sur un pin pour voir les détails";
            _selectedPlaceInfo.BackgroundColor = Color.FromRgb(240, 248, 255);
        }

        private void OnCenterClicked(object? sender, EventArgs e)
        {
            if (_userLocation != null)
            {
                var mapSpan = MapSpan.FromCenterAndRadius(
                    new Microsoft.Maui.Devices.Sensors.Location(_userLocation.Latitude, _userLocation.Longitude),
                    Distance.FromKilometers(2)
                );
                _map.MoveToRegion(mapSpan);
                _statusLabel.Text = "🎯 Carte centrée sur votre position";
            }
            else
            {
                _statusLabel.Text = "⚠️ Position non disponible pour centrage";
            }
        }

        private void AdjustMapViewToIncludeAllPins()
        {
            try
            {
                if (!_placePins.Any()) return;

                var pins = _placePins.Keys.Where(p => p.Location != null).ToList();
                if (!pins.Any()) return;

                var minLat = pins.Min(p => p.Location!.Latitude);
                var maxLat = pins.Max(p => p.Location!.Latitude);
                var minLon = pins.Min(p => p.Location!.Longitude);
                var maxLon = pins.Max(p => p.Location!.Longitude);

                var centerLat = (minLat + maxLat) / 2;
                var centerLon = (minLon + maxLon) / 2;

                var latDelta = Math.Max(maxLat - minLat, 0.01) * 1.2; // Marge de 20%
                var lonDelta = Math.Max(maxLon - minLon, 0.01) * 1.2;

                var distance = Distance.FromKilometers(Math.Max(latDelta, lonDelta) * 111); // Approximation

                var mapSpan = MapSpan.FromCenterAndRadius(
                    new Microsoft.Maui.Devices.Sensors.Location(centerLat, centerLon),
                    distance
                );

                _map.MoveToRegion(mapSpan);
                Console.WriteLine($"🗺️ Carte ajustée pour inclure tous les pins");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur ajustement vue: {ex.Message}");
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