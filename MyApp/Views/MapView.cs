using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views
{
    public class MapView : ContentView
    {
        private readonly ILocationService? _locationService;
        private Location? _currentLocation;
        private readonly StackLayout _placesContainer;

        public static readonly BindableProperty PlacesProperty = BindableProperty.Create(
            nameof(Places), 
            typeof(IEnumerable<Place>), 
            typeof(MapView), 
            null, 
            propertyChanged: OnPlacesChanged);

        public IEnumerable<Place>? Places
        {
            get => (IEnumerable<Place>?)GetValue(PlacesProperty);
            set => SetValue(PlacesProperty, value);
        }

        public MapView()
        {
            try
            {
                _locationService = GetService<ILocationService>();
            }
            catch
            {
                _locationService = null;
            }

            _placesContainer = new StackLayout
            {
                Spacing = 8,
                Padding = 16
            };

            var scrollView = new ScrollView
            {
                Content = _placesContainer,
                HeightRequest = 300
            };

            // Layout avec boutons de contrôle
            var controlButtons = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 10,
                Children =
                {
                    new Button
                    {
                        Text = "📍 Ma position",
                        FontSize = 12,
                        Padding = new Thickness(8, 4),
                        Command = new Command(async () => await LoadCurrentLocationAsync())
                    },
                    new Button
                    {
                        Text = "🗺️ Vue liste",
                        FontSize = 12,
                        Padding = new Thickness(8, 4),
                        Command = new Command(() => ShowPlacesList())
                    }
                }
            };

            var mainLayout = new StackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = "🗺️ Carte des lieux (Mode liste)",
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    controlButtons,
                    new Frame
                    {
                        Content = scrollView,
                        BackgroundColor = Colors.LightGray,
                        BorderColor = Colors.Gray,
                        CornerRadius = 8,
                        Padding = 8
                    }
                }
            };

            Content = new Frame
            {
                Content = mainLayout,
                BackgroundColor = Colors.White,
                BorderColor = Colors.LightGray,
                CornerRadius = 12,
                Padding = 12,
                HasShadow = true
            };

            LoadCurrentLocationAsync();
        }

        private static void OnPlacesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MapView mapView && newValue is IEnumerable<Place> places)
            {
                mapView.UpdatePlacesList(places);
            }
        }

        private void UpdatePlacesList(IEnumerable<Place> places)
        {
            try
            {
                _placesContainer.Children.Clear();

                // Ajouter la position actuelle
                if (_currentLocation != null)
                {
                    var currentLocationView = new Frame
                    {
                        BackgroundColor = Colors.Blue,
                        Padding = 8,
                        CornerRadius = 6,
                        Content = new Label
                        {
                            Text = $"📍 Ma position: {_currentLocation.Latitude:F4}, {_currentLocation.Longitude:F4}",
                            TextColor = Colors.White,
                            FontSize = 12,
                            FontAttributes = FontAttributes.Bold
                        }
                    };
                    _placesContainer.Children.Add(currentLocationView);
                }

                // Ajouter les lieux
                foreach (var place in places.Take(10))
                {
                    if (place.Location != null)
                    {
                        var placeView = CreatePlaceView(place);
                        _placesContainer.Children.Add(placeView);
                    }
                }

                if (!places.Any())
                {
                    _placesContainer.Children.Add(new Label
                    {
                        Text = "Aucun lieu à afficher",
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.Gray,
                        FontAttributes = FontAttributes.Italic
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur mise à jour liste: {ex.Message}");
            }
        }

        private Frame CreatePlaceView(Place place)
        {
            var directionIcon = GetDirectionIcon(place);
            
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var iconLabel = new Label
            {
                Text = directionIcon,
                FontSize = 20,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(iconLabel, 0);
            grid.Children.Add(iconLabel);

            var infoStack = new StackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = place.Name,
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = $"{place.MainCategory} • {place.FormattedDistance}",
                        FontSize = 12,
                        TextColor = Colors.Gray
                    }
                }
            };
            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            var coordsLabel = new Label
            {
                Text = $"{place.Location.Latitude:F3}, {place.Location.Longitude:F3}",
                FontSize = 10,
                TextColor = Colors.DarkGray,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(coordsLabel, 2);
            grid.Children.Add(coordsLabel);

            return new Frame
            {
                BackgroundColor = Colors.White,
                BorderColor = Colors.LightBlue,
                CornerRadius = 8,
                Padding = 12,
                Margin = new Thickness(0, 4),
                Content = grid
            };
        }

        private string GetDirectionIcon(Place place)
        {
            if (_currentLocation == null || place.Location == null) return "📍";

            var bearing = CalculateBearing(_currentLocation.Latitude, _currentLocation.Longitude,
                place.Location.Latitude, place.Location.Longitude);

            return bearing switch
            {
                >= 337.5 or < 22.5 => "⬆️",
                >= 22.5 and < 67.5 => "↗️",
                >= 67.5 and < 112.5 => "➡️",
                >= 112.5 and < 157.5 => "↘️",
                >= 157.5 and < 202.5 => "⬇️",
                >= 202.5 and < 247.5 => "↙️",
                >= 247.5 and < 292.5 => "⬅️",
                >= 292.5 and < 337.5 => "↖️",
                _ => "📍"
            };
        }

        private async Task LoadCurrentLocationAsync()
        {
            try
            {
                if (_locationService != null)
                {
                    var location = await _locationService.GetCurrentLocationAsync();
                    if (location != null)
                    {
                        _currentLocation = location;
                        UpdatePlacesList(Places ?? new List<Place>());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur localisation: {ex.Message}");
            }
        }

        private void ShowPlacesList()
        {
            if (Places != null)
            {
                UpdatePlacesList(Places);
            }
        }

        private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRadians(lon2 - lon1);
            var lat1Rad = ToRadians(lat1);
            var lat2Rad = ToRadians(lat2);

            var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
            var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

            var bearing = Math.Atan2(y, x);
            var bearingDeg = ToDegrees(bearing);
            
            return (bearingDeg + 360) % 360;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
        private static double ToDegrees(double radians) => radians * 180 / Math.PI;

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