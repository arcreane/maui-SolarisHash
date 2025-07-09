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
        private IDispatcherTimer? _simulationTimer; // ✅ CORRECTION

        public VisualCompassView()
        {
            CreateSimpleCompass();
            // ✅ CORRECTION: Démarrer automatiquement
            _ = Task.Run(async () => await StartCompassAsync());
        }

        private void CreateSimpleCompass()
        {
            // Cercle de boussole avec directions cardinales
            var compassGrid = new Grid
            {
                WidthRequest = 200,
                HeightRequest = 200,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Fond de la boussole
            _compassCircle = new Frame
            {
                WidthRequest = 200,
                HeightRequest = 200,
                CornerRadius = 100,
                BackgroundColor = Color.FromRgb(240, 240, 255),
                BorderColor = Colors.DarkBlue,
                HasShadow = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Points cardinaux
            var northLabel = new Label
            {
                Text = "N",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var southLabel = new Label
            {
                Text = "S",
                FontSize = 16,
                TextColor = Colors.Blue,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var eastLabel = new Label
            {
                Text = "E",
                FontSize = 16,
                TextColor = Colors.Green,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var westLabel = new Label
            {
                Text = "W",
                FontSize = 16,
                TextColor = Colors.Orange,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            // Aiguille de la boussole
            _needle = new Label
            {
                Text = "▲",
                FontSize = 30,
                TextColor = Colors.Red,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Rotation = 0
            };

            // Assembler la boussole
            compassGrid.Children.Add(_compassCircle);
            compassGrid.Children.Add(northLabel);
            compassGrid.Children.Add(southLabel);
            compassGrid.Children.Add(eastLabel);
            compassGrid.Children.Add(westLabel);
            compassGrid.Children.Add(_needle);

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
                Text = "Nord ⬆️",
                FontSize = 18,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.DarkBlue,
                FontAttributes = FontAttributes.Bold
            };

            // Statut
            _statusLabel = new Label
            {
                Text = "🧭 Initialisation de la boussole...",
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Green
            };

            // Boutons
            var buttonGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 8
            };

            _startButton = new Button
            {
                Text = "▶️ Démarrer",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 14
            };
            _startButton.Clicked += OnStartClicked;
            Grid.SetColumn(_startButton, 0);
            buttonGrid.Children.Add(_startButton);

            _stopButton = new Button
            {
                Text = "⏹️ Arrêter",
                BackgroundColor = Colors.Red,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 14
            };
            _stopButton.Clicked += OnStopClicked;
            Grid.SetColumn(_stopButton, 1);
            buttonGrid.Children.Add(_stopButton);

            // Layout principal
            var layout = new StackLayout
            {
                Spacing = 16,
                Padding = 20,
                Children =
                {
                    new Label
                    {
                        Text = "🧭 Boussole Interactive",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.DarkBlue
                    },
                    compassGrid,
                    _headingLabel,
                    _directionLabel,
                    _statusLabel,
                    buttonGrid,
                    new Frame
                    {
                        BackgroundColor = Color.FromRgb(255, 248, 220),
                        BorderColor = Colors.Orange,
                        CornerRadius = 8,
                        Padding = 8,
                        Content = new Label
                        {
                            Text = "💡 Pointez votre téléphone dans différentes directions pour voir la boussole bouger !",
                            FontSize = 12,
                            TextColor = Colors.DarkOrange,
                            HorizontalTextAlignment = TextAlignment.Center
                        }
                    }
                }
            };

            Content = layout;
        }

        // ✅ CORRECTION: Méthode async pour le démarrage
        private async Task StartCompassAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _statusLabel.Text = "🔍 Test des capteurs...";
                    _statusLabel.TextColor = Colors.Orange;
                });

                // Attendre un peu pour que l'UI soit prête
                await Task.Delay(1000);

                // Essayer les vrais capteurs
                if (Magnetometer.Default.IsSupported)
                {
                    await StartRealCompassAsync();
                }
                else
                {
                    await StartSimulationModeAsync();
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                    _statusLabel.TextColor = Colors.Red;
                });
                
                // Fallback sur simulation
                await StartSimulationModeAsync();
            }
        }

        private async Task StartRealCompassAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Magnetometer.Default.ReadingChanged += OnMagnetometerChanged;
                    Magnetometer.Default.Start(SensorSpeed.UI);
                    _isListening = true;
                    _statusLabel.Text = "✅ Magnétomètre actif";
                    _statusLabel.TextColor = Colors.Green;
                });

                System.Diagnostics.Debug.WriteLine("🧭 Magnétomètre démarré");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur magnétomètre: {ex.Message}");
                await StartSimulationModeAsync();
            }
        }

        private async Task StartSimulationModeAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _isListening = true;
                    _statusLabel.Text = "🔄 Mode simulation (boussole virtuelle)";
                    _statusLabel.TextColor = Colors.Blue;

                    // ✅ CORRECTION: Utiliser Dispatcher.CreateTimer correctement
                    _simulationTimer = Dispatcher.CreateTimer();
                    _simulationTimer.Interval = TimeSpan.FromMilliseconds(1000);
                    _simulationTimer.Tick += OnSimulationTick;
                    _simulationTimer.Start();
                });

                System.Diagnostics.Debug.WriteLine("🧭 Mode simulation démarré");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur simulation: {ex.Message}");
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _statusLabel.Text = $"❌ Erreur complète: {ex.Message}";
                    _statusLabel.TextColor = Colors.Red;
                });
            }
        }

        private void OnSimulationTick(object? sender, EventArgs e)
        {
            if (_isListening)
            {
                // Simulation d'une rotation lente
                _currentHeading += 5;
                if (_currentHeading >= 360) _currentHeading = 0;
                
                UpdateCompassDisplay(_currentHeading);
            }
        }

        private async void OnStartClicked(object? sender, EventArgs e)
        {
            if (!_isListening)
            {
                await StartCompassAsync();
            }
        }

        private async void OnStopClicked(object? sender, EventArgs e)
        {
            try
            {
                if (Magnetometer.Default.IsSupported)
                {
                    Magnetometer.Default.Stop();
                    Magnetometer.Default.ReadingChanged -= OnMagnetometerChanged;
                }
                
                _simulationTimer?.Stop();
                _simulationTimer = null;
                
                _isListening = false;
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _statusLabel.Text = "🔴 Boussole arrêtée";
                    _statusLabel.TextColor = Colors.Gray;
                    
                    // Remettre au nord
                    UpdateCompassDisplay(0);
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _statusLabel.Text = $"❌ Erreur arrêt: {ex.Message}";
                    _statusLabel.TextColor = Colors.Red;
                });
            }
        }

        private void OnMagnetometerChanged(object? sender, MagnetometerChangedEventArgs e)
        {
            try
            {
                // Calcul de la direction magnétique
                var heading = Math.Atan2(e.Reading.MagneticField.Y, e.Reading.MagneticField.X) * (180.0 / Math.PI);
                if (heading < 0) heading += 360;

                // ✅ CORRECTION: Toujours utiliser MainThread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateCompassDisplay(heading);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur capteur: {ex.Message}");
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

                System.Diagnostics.Debug.WriteLine($"🧭 Boussole mise à jour: {heading:F0}° - {GetDirectionName(heading)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur UpdateCompassDisplay: {ex.Message}");
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

        // ✅ AJOUT: Nettoyage proper
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
                    
                    _simulationTimer?.Stop();
                    _simulationTimer = null;
                    _isListening = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur cleanup: {ex.Message}");
                }
            }
        }
    }
}