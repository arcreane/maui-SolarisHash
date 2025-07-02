using Microsoft.Maui.Devices.Sensors;

namespace MyApp.Services
{
    public interface ICompassService
    {
        event EventHandler<CompassReadingEventArgs> CompassChanged;
        bool IsSupported { get; }
        bool IsListening { get; }
        Task StartAsync();
        Task StopAsync();
        double GetBearingToLocation(double targetLat, double targetLon, double currentLat, double currentLon);
    }

    public class CompassReadingEventArgs : EventArgs
    {
        public double HeadingMagneticNorth { get; set; }
        public double HeadingTrueNorth { get; set; }
        public double Accuracy { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CompassService : ICompassService
    {
        private bool _isListening;
        private double _currentHeading;

        public event EventHandler<CompassReadingEventArgs>? CompassChanged;

        public bool IsSupported => 
            Accelerometer.Default.IsSupported && 
            Magnetometer.Default.IsSupported;

        public bool IsListening => _isListening;

        public async Task StartAsync()
        {
            if (!IsSupported)
            {
                throw new NotSupportedException("Compass not supported on this device");
            }

            if (_isListening)
                return;

            try
            {
                // Démarrer l'accéléromètre
                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                }

                // Démarrer le magnétomètre
                if (Magnetometer.Default.IsSupported)
                {
                    Magnetometer.Default.ReadingChanged += OnMagnetometerReadingChanged;
                    Magnetometer.Default.Start(SensorSpeed.UI);
                }

                _isListening = true;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start compass: {ex.Message}", ex);
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
                    Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
                }

                if (Magnetometer.Default.IsSupported)
                {
                    Magnetometer.Default.Stop();
                    Magnetometer.Default.ReadingChanged -= OnMagnetometerReadingChanged;
                }

                _isListening = false;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping compass: {ex.Message}");
            }
        }

        private Vector3 _lastAcceleration;
        private Vector3 _lastMagnetometer;

        private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            _lastAcceleration = new Vector3(e.Reading.Acceleration.X, e.Reading.Acceleration.Y, e.Reading.Acceleration.Z);
            CalculateHeading();
        }

        private void OnMagnetometerReadingChanged(object? sender, MagnetometerChangedEventArgs e)
        {
            _lastMagnetometer = new Vector3(e.Reading.MagneticField.X, e.Reading.MagneticField.Y, e.Reading.MagneticField.Z);
            CalculateHeading();
        }

        private void CalculateHeading()
        {
            if (_lastAcceleration == default || _lastMagnetometer == default)
                return;

            try
            {
                // Normaliser l'accélération
                var gravity = Vector3.Normalize(_lastAcceleration);
                
                // Calculer l'est magnétique
                var east = Vector3.Cross(_lastMagnetometer, gravity);
                east = Vector3.Normalize(east);
                
                // Calculer le nord magnétique
                var north = Vector3.Cross(gravity, east);
                north = Vector3.Normalize(north);
                
                // Calculer l'angle de cap (heading)
                var heading = Math.Atan2(east.X, north.X) * (180.0 / Math.PI);
                
                // Normaliser entre 0 et 360
                if (heading < 0)
                    heading += 360;
                
                // Filtrer les petites variations
                if (Math.Abs(heading - _currentHeading) > 2)
                {
                    _currentHeading = heading;
                    
                    CompassChanged?.Invoke(this, new CompassReadingEventArgs
                    {
                        HeadingMagneticNorth = heading,
                        HeadingTrueNorth = heading,
                        Accuracy = 5.0,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating heading: {ex.Message}");
            }
        }

        public double GetBearingToLocation(double targetLat, double targetLon, double currentLat, double currentLon)
        {
            var lat1Rad = ToRadians(currentLat);
            var lat2Rad = ToRadians(targetLat);
            var deltaLonRad = ToRadians(targetLon - currentLon);

            var y = Math.Sin(deltaLonRad) * Math.Cos(lat2Rad);
            var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(deltaLonRad);

            var bearingRad = Math.Atan2(y, x);
            var bearingDeg = ToDegrees(bearingRad);

            return (bearingDeg + 360) % 360;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
        private static double ToDegrees(double radians) => radians * 180 / Math.PI;
    }

    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        public static Vector3 Normalize(Vector3 vector)
        {
            var length = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
            if (length == 0) return new Vector3(0, 0, 0);
            
            return new Vector3(
                vector.X / length,
                vector.Y / length,
                vector.Z / length
            );
        }

        public static bool operator ==(Vector3 a, Vector3 b)
        {
            return Math.Abs(a.X - b.X) < 0.0001f && 
                   Math.Abs(a.Y - b.Y) < 0.0001f && 
                   Math.Abs(a.Z - b.Z) < 0.0001f;
        }

        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public override bool Equals(object? obj) => obj is Vector3 other && this == other;

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}