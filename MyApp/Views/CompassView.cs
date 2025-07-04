using MyApp.Services;

namespace MyApp.Views
{
    public class CompassView : ContentView
    {
        private readonly ICompassService? _compassService;
        private readonly Label _statusLabel;
        private readonly Label _directionLabel;
        private readonly Label _degreesLabel;
        private readonly Button _startButton;
        private readonly Button _stopButton;
        private readonly Frame _compassFrame;
        private readonly Grid _compassNeedle;

        public CompassView()
        {
            try
            {
                _compassService = GetService<ICompassService>();
            }
            catch
            {
                _compassService = null;
            }

            // Créer l'interface programmatiquement
            _statusLabel = new Label
            {
                Text = "Boussole inactive",
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center
            };

            _directionLabel = new Label
            {
                Text = "N",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            };

            _degreesLabel = new Label
            {
                Text = "0°",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            };

            _startButton = new Button
            {
                Text = "▶️ Démarrer",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 8
            };
            _startButton.Clicked += OnStartClicked;

            _stopButton = new Button
            {
                Text = "⏹️ Arrêter",
                BackgroundColor = Colors.Red,
                TextColor = Colors.White,
                CornerRadius = 8,
                IsEnabled = false
            };
            _stopButton.Clicked += OnStopClicked;

            // Boussole simplifiée
            _compassNeedle = new Grid
            {
                HeightRequest = 100,
                WidthRequest = 100,
                BackgroundColor = Colors.LightGray
            };

            var northLabel = new Label
            {
                Text = "🧭",
                FontSize = 24,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            _compassNeedle.Children.Add(northLabel);

            _compassFrame = new Frame
            {
                Content = _compassNeedle,
                BackgroundColor = Colors.White,
                BorderColor = Colors.Gray,
                CornerRadius = 50,
                HasShadow = true,
                HeightRequest = 120,
                WidthRequest = 120,
                HorizontalOptions = LayoutOptions.Center
            };

            // Layout principal
            var mainLayout = new StackLayout
            {
                Spacing = 16,
                Padding = 16,
                Children =
                {
                    new Label
                    {
                        Text = "🧭 Boussole",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    _compassFrame,
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        },
                        Children =
                        {
                            new StackLayout
                            {
                                Children =
                                {
                                    new Label { Text = "Direction", FontSize = 12, HorizontalTextAlignment = TextAlignment.Center },
                                    _directionLabel
                                }
                            }.Column(0),
                            new StackLayout
                            {
                                Children =
                                {
                                    new Label { Text = "Degrés", FontSize = 12, HorizontalTextAlignment = TextAlignment.Center },
                                    _degreesLabel
                                }
                            }.Column(1)
                        }
                    },
                    _statusLabel,
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 16,
                        Children = { _startButton, _stopButton }
                    }
                }
            };

            Content = new Frame
            {
                Content = mainLayout,
                BackgroundColor = Colors.White,
                BorderColor = Colors.LightGray,
                CornerRadius = 12,
                Padding = 16,
                HasShadow = true
            };

            if (_compassService != null)
            {
                _compassService.CompassChanged += OnCompassChanged;
            }

            UpdateCompassSupport();
        }

        private void UpdateCompassSupport()
        {
            if (_compassService?.IsSupported != true)
            {
                _statusLabel.Text = "⚠️ Boussole non supportée";
                _statusLabel.TextColor = Colors.Orange;
                _startButton.IsEnabled = false;
            }
            else
            {
                _statusLabel.Text = "Boussole prête";
                _statusLabel.TextColor = Colors.Gray;
            }
        }

        private async void OnStartClicked(object? sender, EventArgs e)
        {
            if (_compassService == null) return;

            try
            {
                await _compassService.StartAsync();
                
                _startButton.IsEnabled = false;
                _stopButton.IsEnabled = true;
                _statusLabel.Text = "🧭 Boussole active";
                _statusLabel.TextColor = Colors.Green;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
                
                await Application.Current?.MainPage?.DisplayAlert(
                    "Erreur Boussole", 
                    $"Impossible de démarrer la boussole:\n{ex.Message}", 
                    "OK");
            }
        }

        private async void OnStopClicked(object? sender, EventArgs e)
        {
            if (_compassService == null) return;

            try
            {
                await _compassService.StopAsync();
                
                _startButton.IsEnabled = true;
                _stopButton.IsEnabled = false;
                _statusLabel.Text = "⏹️ Boussole arrêtée";
                _statusLabel.TextColor = Colors.Gray;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
            }
        }

        private void OnCompassChanged(object? sender, CompassReadingEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateCompassUI(e.HeadingMagneticNorth);
            });
        }

        private void UpdateCompassUI(double heading)
        {
            // Rotation de l'aiguille
            _compassNeedle.Rotation = -heading;

            // Mise à jour des labels
            _degreesLabel.Text = $"{heading:F0}°";
            _directionLabel.Text = GetCardinalDirection(heading);
        }

        private string GetCardinalDirection(double heading)
        {
            heading = (heading + 360) % 360;

            return heading switch
            {
                >= 337.5 or < 22.5 => "N",
                >= 22.5 and < 67.5 => "NE",
                >= 67.5 and < 112.5 => "E",
                >= 112.5 and < 157.5 => "SE",
                >= 157.5 and < 202.5 => "S",
                >= 202.5 and < 247.5 => "SO",
                >= 247.5 and < 292.5 => "O",
                >= 292.5 and < 337.5 => "NO",
                _ => "N"
            };
        }

        public double GetBearingToPlace(double targetLat, double targetLon, double currentLat, double currentLon)
        {
            return _compassService?.GetBearingToLocation(targetLat, targetLon, currentLat, currentLon) ?? 0;
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            
            if (Handler == null && _compassService != null)
            {
                _compassService.CompassChanged -= OnCompassChanged;
                
                if (_compassService.IsListening)
                {
                    Task.Run(async () => await _compassService.StopAsync());
                }
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

    // Extensions pour simplifier Grid
    public static class GridExtensions
    {
        public static T Column<T>(this T view, int column) where T : View
        {
            Grid.SetColumn(view, column);
            return view;
        }
    }
}