using Microsoft.Maui.Devices.Sensors;

namespace MyApp.Services
{
    public interface ISamsungSensorService
    {
        Task<string> DiagnosticSensorsAsync();
        Task<bool> TestAccelerometerAsync();
        Task<bool> TestMagnetometerAsync();
        Task<bool> TestGyroscopeAsync();
        Task StartSensorsAsync(); // ✅ Méthode ajoutée
        Task StopSensorsAsync();  // ✅ Méthode ajoutée
        event EventHandler<SensorDataEventArgs> SensorDataChanged;
    }

    public class SensorDataEventArgs : EventArgs
    {
        public double Heading { get; set; }
        public string DirectionName { get; set; } = string.Empty;
        public bool IsWorking { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class SamsungSensorService : ISamsungSensorService
    {
        public event EventHandler<SensorDataEventArgs>? SensorDataChanged;
        
        private bool _isListening = false;
        private double _currentHeading = 0;
        private System.Numerics.Vector3 _lastAcceleration;
        private System.Numerics.Vector3 _lastMagnetometer;

        public async Task<string> DiagnosticSensorsAsync()
        {
            var diagnostic = new List<string>();
            diagnostic.Add("🔍 DIAGNOSTIC CAPTEURS SAMSUNG");
            diagnostic.Add("================================");

            // Test disponibilité des capteurs
            diagnostic.Add($"📱 Device: {DeviceInfo.Model} ({DeviceInfo.Manufacturer})");
            diagnostic.Add($"🤖 Android: {DeviceInfo.VersionString}");
            diagnostic.Add("");

            // Test Accéléromètre
            try
            {
                var accelSupported = Accelerometer.Default.IsSupported;
                diagnostic.Add($"📐 Accéléromètre: {(accelSupported ? "✅ Supporté" : "❌ Non supporté")}");
                
                if (accelSupported)
                {
                    var accelWorking = await TestAccelerometerAsync();
                    diagnostic.Add($"   Test fonctionnel: {(accelWorking ? "✅ OK" : "❌ Échec")}");
                }
            }
            catch (Exception ex)
            {
                diagnostic.Add($"📐 Accéléromètre: ❌ Erreur - {ex.Message}");
            }

            // Test Magnétomètre
            try
            {
                var magSupported = Magnetometer.Default.IsSupported;
                diagnostic.Add($"🧲 Magnétomètre: {(magSupported ? "✅ Supporté" : "❌ Non supporté")}");
                
                if (magSupported)
                {
                    var magWorking = await TestMagnetometerAsync();
                    diagnostic.Add($"   Test fonctionnel: {(magWorking ? "✅ OK" : "❌ Échec")}");
                }
            }
            catch (Exception ex)
            {
                diagnostic.Add($"🧲 Magnétomètre: ❌ Erreur - {ex.Message}");
            }

            // Test Gyroscope
            try
            {
                var gyroSupported = Gyroscope.Default.IsSupported;
                diagnostic.Add($"🌀 Gyroscope: {(gyroSupported ? "✅ Supporté" : "❌ Non supporté")}");
                
                if (gyroSupported)
                {
                    var gyroWorking = await TestGyroscopeAsync();
                    diagnostic.Add($"   Test fonctionnel: {(gyroWorking ? "✅ OK" : "❌ Échec")}");
                }
            }
            catch (Exception ex)
            {
                diagnostic.Add($"🌀 Gyroscope: ❌ Erreur - {ex.Message}");
            }

            diagnostic.Add("");
            diagnostic.Add("💡 SOLUTIONS:");
            diagnostic.Add("- Vérifiez que les capteurs ne sont pas désactivés dans les paramètres");
            diagnostic.Add("- Redémarrez l'app si les capteurs ne répondent pas");
            diagnostic.Add("- Certains Samsung nécessitent d'activer 'Capteurs haute précision'");

            return string.Join("\n", diagnostic);
        }

        public async Task<bool> TestAccelerometerAsync()
        {
            try
            {
                if (!Accelerometer.Default.IsSupported)
                    return false;

                bool dataReceived = false;
                var tcs = new TaskCompletionSource<bool>();

                void OnAccelerometerChanged(object? sender, AccelerometerChangedEventArgs e)
                {
                    dataReceived = true;
                    Console.WriteLine($"📐 Accéléromètre: X={e.Reading.Acceleration.X:F2}, Y={e.Reading.Acceleration.Y:F2}, Z={e.Reading.Acceleration.Z:F2}");
                    tcs.TrySetResult(true);
                }

                Accelerometer.Default.ReadingChanged += OnAccelerometerChanged;
                Accelerometer.Default.Start(SensorSpeed.UI);

                // Attendre max 5 secondes
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= OnAccelerometerChanged;

                return completedTask == tcs.Task && dataReceived;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test accéléromètre: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestMagnetometerAsync()
        {
            try
            {
                if (!Magnetometer.Default.IsSupported)
                    return false;

                bool dataReceived = false;
                var tcs = new TaskCompletionSource<bool>();

                void OnMagnetometerChanged(object? sender, MagnetometerChangedEventArgs e)
                {
                    dataReceived = true;
                    Console.WriteLine($"🧲 Magnétomètre: X={e.Reading.MagneticField.X:F2}, Y={e.Reading.MagneticField.Y:F2}, Z={e.Reading.MagneticField.Z:F2}");
                    tcs.TrySetResult(true);
                }

                Magnetometer.Default.ReadingChanged += OnMagnetometerChanged;
                Magnetometer.Default.Start(SensorSpeed.UI);

                // Attendre max 5 secondes
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                Magnetometer.Default.Stop();
                Magnetometer.Default.ReadingChanged -= OnMagnetometerChanged;

                return completedTask == tcs.Task && dataReceived;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test magnétomètre: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestGyroscopeAsync()
        {
            try
            {
                if (!Gyroscope.Default.IsSupported)
                    return false;

                bool dataReceived = false;
                var tcs = new TaskCompletionSource<bool>();

                void OnGyroscopeChanged(object? sender, GyroscopeChangedEventArgs e)
                {
                    dataReceived = true;
                    Console.WriteLine($"🌀 Gyroscope: X={e.Reading.AngularVelocity.X:F2}, Y={e.Reading.AngularVelocity.Y:F2}, Z={e.Reading.AngularVelocity.Z:F2}");
                    tcs.TrySetResult(true);
                }

                Gyroscope.Default.ReadingChanged += OnGyroscopeChanged;
                Gyroscope.Default.Start(SensorSpeed.UI);

                // Attendre max 5 secondes
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                Gyroscope.Default.Stop();
                Gyroscope.Default.ReadingChanged -= OnGyroscopeChanged;

                return completedTask == tcs.Task && dataReceived;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test gyroscope: {ex.Message}");
                return false;
            }
        }

        // ✅ MÉTHODE AJOUTÉE: StartSensorsAsync
        public async Task StartSensorsAsync()
        {
            if (_isListening) return;

            try
            {
                Console.WriteLine("🚀 Démarrage capteurs Samsung...");

                // Démarrer accéléromètre
                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.ReadingChanged += OnAccelerometerChanged;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                    Console.WriteLine("✅ Accéléromètre démarré");
                }

                // Démarrer magnétomètre
                if (Magnetometer.Default.IsSupported)
                {
                    Magnetometer.Default.ReadingChanged += OnMagnetometerChanged;
                    Magnetometer.Default.Start(SensorSpeed.UI);
                    Console.WriteLine("✅ Magnétomètre démarré");
                }

                _isListening = true;
                
                // Envoyer un événement initial pour indiquer que les capteurs sont actifs
                SensorDataChanged?.Invoke(this, new SensorDataEventArgs
                {
                    Heading = _currentHeading,
                    DirectionName = GetDirectionName(_currentHeading),
                    IsWorking = true
                });
                
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur démarrage capteurs: {ex.Message}");
                SensorDataChanged?.Invoke(this, new SensorDataEventArgs
                {
                    IsWorking = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        // ✅ MÉTHODE AJOUTÉE: StopSensorsAsync
        public async Task StopSensorsAsync()
        {
            if (!_isListening) return;

            try
            {
                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= OnAccelerometerChanged;
                }

                if (Magnetometer.Default.IsSupported)
                {
                    Magnetometer.Default.Stop();
                    Magnetometer.Default.ReadingChanged -= OnMagnetometerChanged;
                }

                _isListening = false;
                Console.WriteLine("🛑 Capteurs arrêtés");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur arrêt capteurs: {ex.Message}");
            }
        }

        private void OnAccelerometerChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            _lastAcceleration = new System.Numerics.Vector3(
                e.Reading.Acceleration.X, 
                e.Reading.Acceleration.Y, 
                e.Reading.Acceleration.Z);
            CalculateHeading();
        }

        private void OnMagnetometerChanged(object? sender, MagnetometerChangedEventArgs e)
        {
            _lastMagnetometer = new System.Numerics.Vector3(
                e.Reading.MagneticField.X, 
                e.Reading.MagneticField.Y, 
                e.Reading.MagneticField.Z);
            CalculateHeading();
        }

        private void CalculateHeading()
        {
            if (_lastAcceleration == default || _lastMagnetometer == default)
                return;

            try
            {
                // Calcul simplifié de la boussole
                var heading = Math.Atan2(_lastMagnetometer.Y, _lastMagnetometer.X) * (180.0 / Math.PI);
                
                // Normaliser entre 0 et 360
                if (heading < 0) heading += 360;
                
                // Filtrer les variations trop petites
                if (Math.Abs(heading - _currentHeading) > 5)
                {
                    _currentHeading = heading;
                    
                    SensorDataChanged?.Invoke(this, new SensorDataEventArgs
                    {
                        Heading = heading,
                        DirectionName = GetDirectionName(heading),
                        IsWorking = true
                    });
                    
                    Console.WriteLine($"🧭 Direction: {GetDirectionName(heading)} ({heading:F0}°)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur calcul boussole: {ex.Message}");
            }
        }

        private string GetDirectionName(double heading)
        {
            return heading switch
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
    }
}