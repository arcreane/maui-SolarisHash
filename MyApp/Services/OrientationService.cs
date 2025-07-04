using Microsoft.Maui.Devices.Sensors;
using MyApp.Models;

namespace MyApp.Services
{
    public interface IOrientationService
    {
        event EventHandler<OrientationChangedEventArgs> OrientationChanged;
        bool IsSupported { get; }
        bool IsListening { get; }
        Task StartAsync();
        Task StopAsync();
        List<Place> FilterPlacesByOrientation(List<Place> places, double userLat, double userLon, double tolerance = 45.0);
    }

    public class OrientationChangedEventArgs : EventArgs
    {
        public double Heading { get; set; } // Direction du téléphone en degrés (0 = Nord)
        public double Pitch { get; set; }   // Inclinaison vers l'avant/arrière
        public double Roll { get; set; }    // Inclinaison gauche/droite
        public DateTime Timestamp { get; set; }
        public string DirectionName { get; set; } = string.Empty;
    }

    public class OrientationService : IOrientationService
    {
        private bool _isListening;
        private double _currentHeading = 0;
        private readonly ICompassService? _compassService;

        public event EventHandler<OrientationChangedEventArgs>? OrientationChanged;

        public bool IsSupported => 
            Accelerometer.Default.IsSupported && 
            Magnetometer.Default.IsSupported &&
            Gyroscope.Default.IsSupported;

        public bool IsListening => _isListening;

        public OrientationService(ICompassService? compassService = null)
        {
            _compassService = compassService;
        }

        public async Task StartAsync()
        {
            if (!IsSupported)
            {
                throw new NotSupportedException("Orientation sensors not supported on this device");
            }

            if (_isListening)
                return;

            try
            {
                // Démarrer l'accéléromètre pour l'inclinaison
                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.ReadingChanged += OnAccelerometerChanged;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                }

                // Démarrer le gyroscope pour la rotation
                if (Gyroscope.Default.IsSupported)
                {
                    Gyroscope.Default.ReadingChanged += OnGyroscopeChanged;
                    Gyroscope.Default.Start(SensorSpeed.UI);
                }

                // Utiliser le service de boussole si disponible
                if (_compassService?.IsSupported == true)
                {
                    _compassService.CompassChanged += OnCompassChanged;
                    await _compassService.StartAsync();
                }

                _isListening = true;
                Console.WriteLine("🧭 Service d'orientation démarré");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start orientation service: {ex.Message}", ex);
            }
        }

        public async Task StopAsync()
        {
            if (!_isListening)
                return;

            try
            {
                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= OnAccelerometerChanged;
                }

                if (Gyroscope.Default.IsSupported)
                {
                    Gyroscope.Default.Stop();
                    Gyroscope.Default.ReadingChanged -= OnGyroscopeChanged;
                }

                if (_compassService?.IsListening == true)
                {
                    _compassService.CompassChanged -= OnCompassChanged;
                    await _compassService.StopAsync();
                }

                _isListening = false;
                Console.WriteLine("🛑 Service d'orientation arrêté");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur arrêt orientation: {ex.Message}");
            }
        }

        private System.Numerics.Vector3 _lastAcceleration;
        private System.Numerics.Vector3 _lastGyroscope;

        private void OnAccelerometerChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            _lastAcceleration = new System.Numerics.Vector3(e.Reading.Acceleration.X, e.Reading.Acceleration.Y, e.Reading.Acceleration.Z);
            UpdateOrientation();
        }

        private void OnGyroscopeChanged(object? sender, GyroscopeChangedEventArgs e)
        {
            _lastGyroscope = new System.Numerics.Vector3(e.Reading.AngularVelocity.X, e.Reading.AngularVelocity.Y, e.Reading.AngularVelocity.Z);
            UpdateOrientation();
        }

        private void OnCompassChanged(object? sender, CompassReadingEventArgs e)
        {
            _currentHeading = e.HeadingMagneticNorth;
            UpdateOrientation();
        }

        private void UpdateOrientation()
        {
            try
            {
                if (_lastAcceleration == default)
                    return;

                // Calculer l'inclinaison (pitch et roll)
                var pitch = Math.Atan2(_lastAcceleration.Y, 
                    Math.Sqrt(_lastAcceleration.X * _lastAcceleration.X + _lastAcceleration.Z * _lastAcceleration.Z)) * 180.0 / Math.PI;
                
                var roll = Math.Atan2(-_lastAcceleration.X, _lastAcceleration.Z) * 180.0 / Math.PI;

                // Utiliser la direction de la boussole
                var heading = _currentHeading;

                var args = new OrientationChangedEventArgs
                {
                    Heading = heading,
                    Pitch = pitch,
                    Roll = roll,
                    Timestamp = DateTime.Now,
                    DirectionName = GetDirectionName(heading)
                };

                OrientationChanged?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur calcul orientation: {ex.Message}");
            }
        }

        public List<Place> FilterPlacesByOrientation(List<Place> places, double userLat, double userLon, double tolerance = 45.0)
        {
            if (!places.Any()) return places;

            var filteredPlaces = new List<Place>();

            foreach (var place in places)
            {
                if (place.Location != null)
                {
                    // Calculer l'angle vers ce lieu
                    var bearingToPlace = CalculateBearing(userLat, userLon, 
                        place.Location.Latitude, place.Location.Longitude);

                    // Vérifier si le lieu est dans la direction du téléphone (±tolerance)
                    var angleDifference = Math.Abs(NormalizeAngle(bearingToPlace - _currentHeading));
                    
                    if (angleDifference <= tolerance || angleDifference >= (360 - tolerance))
                    {
                        filteredPlaces.Add(place);
                        Console.WriteLine($"🎯 Lieu dans la direction: {place.Name} (angle: {bearingToPlace:F0}°, diff: {angleDifference:F0}°)");
                    }
                }
            }

            Console.WriteLine($"🧭 Filtrage par orientation: {places.Count} -> {filteredPlaces.Count} lieux (direction: {_currentHeading:F0}°, tolérance: ±{tolerance}°)");
            return filteredPlaces.OrderBy(p => p.Distance).ToList();
        }

        private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRadians(lon2 - lon1);
            var lat1Rad = ToRadians(lat1);
            var lat2Rad = ToRadians(lat2);

            var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
            var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

            var bearing = Math.Atan2(y, x);
            return NormalizeAngle(ToDegrees(bearing));
        }

        private double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 360;
            while (angle >= 360) angle -= 360;
            return angle;
        }

        private string GetDirectionName(double heading)
        {
            var normalizedHeading = NormalizeAngle(heading);
            
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

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
        private static double ToDegrees(double radians) => radians * 180 / Math.PI;
    }
}