using MyApp.Services;

namespace MyApp.Views
{
    public class VisualCompassView : ContentView
    {
        private readonly Grid _compassContainer;
        private readonly Frame _compassFrame;
        private readonly Label _northLabel;
        private readonly Label _eastLabel;
        private readonly Label _southLabel;
        private readonly Label _westLabel;
        private readonly Label _centerDot;
        private readonly Label _needle;
        private readonly Label _degreesLabel;
        private readonly Label _directionLabel;
        private readonly Label _statusLabel;

        private double _currentHeading = 0;
        private ISamsungSensorService? _sensorService;

        public VisualCompassView()
        {
            try
            {
                _sensorService = GetService<ISamsungSensorService>();
            }
            catch
            {
                _sensorService = null;
            }

            // Container principal
            _compassContainer = new Grid
            {
                HeightRequest = 280,
                WidthRequest = 280,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Cadre de la boussole
            _compassFrame = new Frame
            {
                HeightRequest = 240,
                WidthRequest = 240,
                CornerRadius = 120,
                BackgroundColor = Colors.White,
                BorderColor = Colors.Gray,
                HasShadow = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Points cardinaux
            _northLabel = new Label
            {
                Text = "N",
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 10, 0, 0)
            };

            _eastLabel = new Label
            {
                Text = "E",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };

            _southLabel = new Label
            {
                Text = "S",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _westLabel = new Label
            {
                Text = "O",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(15, 0, 0, 0)
            };

            // Centre de la boussole
            _centerDot = new Label
            {
                Text = "⊙",
                FontSize = 16,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Aiguille de la boussole
            _needle = new Label
            {
                Text = "🧭",
                FontSize = 32,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Rotation = 0
            };

            // Assembler la boussole
            var compassContent = new Grid();
            compassContent.Children.Add(_northLabel);
            compassContent.Children.Add(_eastLabel);
            compassContent.Children.Add(_southLabel);
            compassContent.Children.Add(_westLabel);
            compassContent.Children.Add(_centerDot);
            compassContent.Children.Add(_needle);

            _compassFrame.Content = compassContent;
            _compassContainer.Children.Add(_compassFrame);

            // Informations textuelles
            _degreesLabel = new Label
            {
                Text = "0°",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Blue
            };

            _directionLabel = new Label
            {
                Text = "Nord ⬆️",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.DarkBlue
            };

            _statusLabel = new Label
            {
                Text = "🔴 Boussole inactive",
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Gray
            };

            // Boutons de contrôle
            var startButton = new Button
            {
                Text = "▶️ Activer Boussole",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 16,
                Margin = new Thickness(0, 10, 0, 0)
            };
            startButton.Clicked += OnStartClicked;

            var stopButton = new Button
            {
                Text = "⏹️ Désactiver",
                BackgroundColor = Colors.Red,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 16,
                Margin = new Thickness(0, 5, 0, 0)
            };
            stopButton.Clicked += OnStopClicked;

            var calibrateButton = new Button
            {
                Text = "🔄 Calibrer (faire des 8)",
                BackgroundColor = Colors.Orange,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 14,
                Margin = new Thickness(0, 5, 0, 0)
            };
            calibrateButton.Clicked += OnCalibrateClicked;

            // Layout principal
            var mainLayout = new StackLayout
            {
                Spacing = 16,
                Padding = 20,
                Children =
                {
                    new Label
                    {
                        Text = "🧭 Boussole Samsung",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    _compassContainer,
                    _degreesLabel,
                    _directionLabel,
                    _statusLabel,
                    startButton,
                    stopButton,
                    calibrateButton,
                    new Frame
                    {
                        BackgroundColor = Colors.LightYellow,
                        BorderColor = Colors.Orange,
                        CornerRadius = 8,
                        Padding = 12,
                        Content = new StackLayout
                        {
                            Children =
                            {
                                new Label
                                {
                                    Text = "💡 Instructions Samsung:",
                                    FontSize = 14,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Colors.DarkOrange
                                },
                                new Label
                                {
                                    Text = "1. Activez la boussole\n2. Tenez le téléphone à plat\n3. Tournez lentement pour voir l'aiguille bouger\n4. Si ça ne marche pas, calibrez en faisant des '8'",
                                    FontSize = 12,
                                    TextColor = Colors.Black
                                }
                            }
                        }
                    }
                }
            };

            Content = new Frame
            {
                Content = mainLayout,
                BackgroundColor = Colors.White,
                BorderColor = Colors.LightGray,
                CornerRadius = 16,
                Padding = 0,
                HasShadow = true
            };

            // S'abonner aux changements de capteurs
            if (_sensorService != null)
            {
                _sensorService.SensorDataChanged += OnSensorDataChanged;
            }
        }

        private async void OnStartClicked(object? sender, EventArgs e)
        {
            if (_sensorService == null)
            {
                _statusLabel.Text = "❌ Service de capteurs non disponible";
                _statusLabel.TextColor = Colors.Red;
                return;
            }

            try
            {
                _statusLabel.Text = "🔄 Démarrage de la boussole...";
                _statusLabel.TextColor = Colors.Orange;

                await _sensorService.StartSensorsAsync();
                
                _statusLabel.Text = "🟢 Boussole active - Tournez votre téléphone !";
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
            if (_sensorService == null) return;

            try
            {
                await _sensorService.StopSensorsAsync();
                
                _statusLabel.Text = "🔴 Boussole désactivée";
                _statusLabel.TextColor = Colors.Gray;
                
                // Remettre l'aiguille au nord
                _needle.Rotation = 0;
                _degreesLabel.Text = "0°";
                _directionLabel.Text = "Nord ⬆️";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur arrêt: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
            }
        }

        private async void OnCalibrateClicked(object? sender, EventArgs e)
        {
            await Application.Current?.MainPage?.DisplayAlert(
                "Calibrage Samsung", 
                "Pour calibrer votre boussole Samsung:\n\n" +
                "1. Tenez le téléphone et faites des mouvements en forme de '8' dans l'air\n" +
                "2. Répétez plusieurs fois\n" +
                "3. Éloignez-vous des objets métalliques\n" +
                "4. Réessayez la boussole\n\n" +
                "Certains Samsung ont aussi un calibrage automatique dans Paramètres → Avancé → Capteurs.", 
                "Compris");
        }

        private void OnSensorDataChanged(object? sender, SensorDataEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (e.IsWorking)
                {
                    UpdateCompassDisplay(e.Heading, e.DirectionName);
                }
                else
                {
                    _statusLabel.Text = $"❌ Capteurs: {e.ErrorMessage}";
                    _statusLabel.TextColor = Colors.Red;
                }
            });
        }

        private void UpdateCompassDisplay(double heading, string directionName)
        {
            try
            {
                _currentHeading = heading;
                
                // Rotation de l'aiguille (inverse car l'aiguille pointe vers le nord)
                _needle.Rotation = -heading;
                
                // Mise à jour des textes
                _degreesLabel.Text = $"{heading:F0}°";
                _directionLabel.Text = directionName;
                
                // Animation fluide
                var rotateAnimation = new Animation(
                    v => _needle.Rotation = v,
                    _needle.Rotation,
                    -heading,
                    Easing.SinOut
                );
                
                rotateAnimation.Commit(_needle, "CompassRotation", 16, 500);
                
                Console.WriteLine($"🧭 Boussole mise à jour: {directionName} ({heading:F0}°)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur mise à jour boussole: {ex.Message}");
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            
            if (Handler == null && _sensorService != null)
            {
                _sensorService.SensorDataChanged -= OnSensorDataChanged;
                
                Task.Run(async () =>
                {
                    try
                    {
                        await _sensorService.StopSensorsAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Erreur arrêt capteurs: {ex.Message}");
                    }
                });
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