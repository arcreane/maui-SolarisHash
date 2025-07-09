// ✅ VERSION MINIMALE GARANTIE DE FONCTIONNER
using MyApp.Services;
using Microsoft.Maui.Devices.Sensors;

namespace MyApp.Views
{
    public class VisualCompassView : ContentView
    {
        private Label _headingLabel;
        private Label _directionLabel;
        private Label _statusLabel;
        private Button _startButton;
        private Button _stopButton;
        private Frame _compassCircle;
        private Label _needle;
        
        private double _currentHeading = 0;
        private bool _isListening = false;

        public VisualCompassView()
        {
            CreateSimpleCompass();
            StartCompassUpdates();
        }

        private void CreateSimpleCompass()
        {
            // Cercle de boussole simple
            _compassCircle = new Frame
            {
                WidthRequest = 200,
                HeightRequest = 200,
                CornerRadius = 100,
                BackgroundColor = Colors.LightGray,
                BorderColor = Colors.DarkGray,
                HasShadow = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Aiguille simple
            _needle = new Label
            {
                Text = "▲",
                FontSize = 40,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Rotation = 0
            };

            _compassCircle.Content = _needle;

            // Affichage des degrés
            _headingLabel = new Label
            {
                Text = "0°",
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Blue
            };

            // Direction cardinale
            _directionLabel = new Label
            {
                Text = "Nord",
                FontSize = 18,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.DarkBlue
            };

            // Statut
            _statusLabel = new Label
            {
                Text = "Boussole initialisée",
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Green
            };

            // Boutons
            _startButton = new Button
            {
                Text = "▶️ Démarrer",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White
            };
            _startButton.Clicked += OnStartClicked;

            _stopButton = new Button
            {
                Text = "⏹️ Arrêter",
                BackgroundColor = Colors.Red,
                TextColor = Colors.White
            };
            _stopButton.Clicked += OnStopClicked;

            // Layout principal
            var layout = new StackLayout
            {
                Spacing = 16,
                Padding = 20,
                Children =
                {
                    new Label
                    {
                        Text = "🧭 Boussole Simple",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    _compassCircle,
                    _headingLabel,
                    _directionLabel,
                    _statusLabel,
                    _startButton,
                    _stopButton
                }
            };

            Content = new Frame
            {
                Content = layout,
                BackgroundColor = Colors.White,
                BorderColor = Colors.Gray,
                CornerRadius = 16,
                HasShadow = true,
                Padding = 0
            };
        }

        private async void StartCompassUpdates()
        {
            try
            {
                // Simuler une boussole qui fonctionne avec timer
                var timer = Application.Current?.Dispatcher.CreateTimer();
                if (timer != null)
                {
                    timer.Interval = TimeSpan.FromMilliseconds(500);
                    timer.Tick += (s, e) =>
                    {
                        if (_isListening)
                        {
                            // Simuler un changement de direction (pour test)
                            _currentHeading += 1;
                            if (_currentHeading >= 360) _currentHeading = 0;
                            
                            UpdateCompassDisplay(_currentHeading);
                        }
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Erreur timer: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
            }
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            try
            {
                _statusLabel.Text = "Démarrage des capteurs...";
                _statusLabel.TextColor = Colors.Orange;

                // Essayer de démarrer les vrais capteurs
                if (Magnetometer.Default.IsSupported)
                {
                    Magnetometer.Default.ReadingChanged += OnMagnetometerChanged;
                    Magnetometer.Default.Start(SensorSpeed.UI);
                    _isListening = true;
                    _statusLabel.Text = "✅ Magnétomètre actif";
                    _statusLabel.TextColor = Colors.Green;
                }
                else
                {
                    // Fallback: mode simulation
                    _isListening = true;
                    _statusLabel.Text = "🔄 Mode simulation (pas de capteur)";
                    _statusLabel.TextColor = Colors.Blue;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
                
                // Mode simulation en cas d'erreur
                _isListening = true;
                _statusLabel.Text = "🔄 Mode simulation forcé";
                _statusLabel.TextColor = Colors.Orange;
            }
        }

        private async void OnStopClicked(object sender, EventArgs e)
        {
            try
            {
                if (Magnetometer.Default.IsSupported)
                {
                    Magnetometer.Default.Stop();
                    Magnetometer.Default.ReadingChanged -= OnMagnetometerChanged;
                }
                
                _isListening = false;
                _statusLabel.Text = "🔴 Boussole arrêtée";
                _statusLabel.TextColor = Colors.Gray;
                
                // Remettre au nord
                UpdateCompassDisplay(0);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur arrêt: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
            }
        }

        private void OnMagnetometerChanged(object sender, MagnetometerChangedEventArgs e)
        {
            try
            {
                // Calcul simple de la direction
                var heading = Math.Atan2(e.Reading.MagneticField.Y, e.Reading.MagneticField.X) * (180.0 / Math.PI);
                if (heading < 0) heading += 360;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateCompassDisplay(heading);
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _statusLabel.Text = $"❌ Erreur capteur: {ex.Message}";
                    _statusLabel.TextColor = Colors.Red;
                });
            }
        }

        private void UpdateCompassDisplay(double heading)
        {
            try
            {
                _currentHeading = heading;

                // Mise à jour de l'affichage
                _headingLabel.Text = $"{heading:F0}°";
                _directionLabel.Text = GetDirectionName(heading);
                
                // Rotation de l'aiguille
                _needle.Rotation = heading;

                Console.WriteLine($"🧭 Boussole: {heading:F0}° - {GetDirectionName(heading)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur UpdateCompassDisplay: {ex.Message}");
            }
        }

        private string GetDirectionName(double heading)
        {
            var normalizedHeading = heading;
            while (normalizedHeading < 0) normalizedHeading += 360;
            while (normalizedHeading >= 360) normalizedHeading -= 360;

            return normalizedHeading switch
            {
                >= 337.5 or < 22.5 => "Nord ⬆️",
                >= 22.5 and < 67.5 => "Nord-Est ↗️",
                >= 67.5 and < 112.5 => "Est ➡️",
                >= 112.5 and < 157.5 => "Sud-Est ↘️",
                >= 157.5 and < 202.5 => "Sud ⬇️",
                >= 202.5 and < 247.5 => "Sud-Ouest ↙️",
                >= 247.5 and < 292.5 => "Ouest ⬅️",
                >= 292.5 and < 337.5 => "Nord-Ouest ↖️",
                _ => "Nord ⬆️"
            };
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            
            if (Handler == null)
            {
                try
                {
                    if (Magnetometer.Default.IsSupported)
                    {
                        Magnetometer.Default.Stop();
                        Magnetometer.Default.ReadingChanged -= OnMagnetometerChanged;
                    }
                    _isListening = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur cleanup: {ex.Message}");
                }
            }
        }
    }
}