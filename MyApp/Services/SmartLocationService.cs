using Microsoft.Maui.Devices.Sensors;

namespace MyApp.Services
{
    public interface ILocationService
    {
        Task<Location?> GetCurrentLocationAsync();
        Task<Location?> GetLocationByNameAsync(string cityName);
        bool IsEmulator { get; }
        event EventHandler<LocationChangedEventArgs> LocationChanged;
    }

    public class LocationChangedEventArgs : EventArgs
    {
        public required Location NewLocation { get; set; } // ✅ Corrigé avec required
        public required string Source { get; set; } // ✅ Corrigé avec required
    }

    public class SmartLocationService : ILocationService
    {
        public event EventHandler<LocationChangedEventArgs>? LocationChanged;
        
        // Villes françaises populaires pour les tests
        private readonly Dictionary<string, (double Lat, double Lon)> _cityCoordinates = new()
        {
            ["paris"] = (48.8566, 2.3522),
            ["lyon"] = (45.7640, 4.8357),
            ["marseille"] = (43.2965, 5.3698),
            ["toulouse"] = (43.6047, 1.4442),
            ["nice"] = (43.7102, 7.2620),
            ["nantes"] = (47.2184, -1.5536),
            ["montpellier"] = (43.6110, 3.8767),
            ["strasbourg"] = (48.5734, 7.7521),
            ["bordeaux"] = (44.8378, -0.5792),
            ["lille"] = (50.6292, 3.0573),
            ["rennes"] = (48.1173, -1.6778),
            ["reims"] = (49.2583, 4.0317),
            ["le havre"] = (49.4944, 0.1079),
            ["saint-étienne"] = (45.4397, 4.3872),
            ["toulon"] = (43.1242, 5.9280),
            ["grenoble"] = (45.1885, 5.7245),
            ["dijon"] = (47.3220, 5.0415),
            ["angers"] = (47.4784, -0.5632),
            ["villeurbanne"] = (45.7665, 4.8795),
            ["nîmes"] = (43.8367, 4.3601),
            ["aix-en-provence"] = (43.5297, 5.4474),
            ["brest"] = (48.3905, -4.4860),
            ["le mans"] = (48.0061, 0.1996),
            ["amiens"] = (49.8941, 2.2957),
            ["tours"] = (47.3941, 0.6848),
            ["limoges"] = (45.8336, 1.2611),
            ["clermont-ferrand"] = (45.7772, 3.0870),
            ["besançon"] = (47.2381, 6.0244),
            ["orléans"] = (47.9029, 1.9093),
            ["metz"] = (49.1193, 6.1757),
            ["rouen"] = (49.4431, 1.0993),
            ["mulhouse"] = (47.7508, 7.3359),
            ["caen"] = (49.1829, -0.3707),
            ["nancy"] = (48.6921, 6.1844)
        };

        public bool IsEmulator => DeviceInfo.DeviceType == DeviceType.Virtual;

        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                Console.WriteLine($"📍 Tentative de géolocalisation... (Émulateur: {IsEmulator})");

                // Vérifier les permissions
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    Console.WriteLine("❌ Permission de géolocalisation refusée");
                    return await GetFallbackLocationAsync("Permission refusée");
                }

                // Essayer la géolocalisation réelle
                try
                {
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(10)
                    });

                    if (location != null)
                    {
                        Console.WriteLine($"✅ Géolocalisation réussie: {location.Latitude:F6}, {location.Longitude:F6}");
                        
                        // Vérifier si c'est une position d'émulateur connue
                        if (IsEmulatorLocation(location.Latitude, location.Longitude))
                        {
                            Console.WriteLine("🤖 Position d'émulateur détectée, utilisation d'une ville française aléatoire");
                            return await GetRandomFrenchCityAsync();
                        }

                        LocationChanged?.Invoke(this, new LocationChangedEventArgs 
                        { 
                            NewLocation = location, 
                            Source = "GPS" 
                        });
                        
                        return location;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur géolocalisation: {ex.Message}");
                }

                // Fallback pour émulateur ou erreur
                return await GetFallbackLocationAsync("Géolocalisation échouée");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Erreur générale géolocalisation: {ex.Message}");
                return await GetFallbackLocationAsync("Erreur générale");
            }
        }

        public async Task<Location?> GetLocationByNameAsync(string cityName)
        {
            await Task.Delay(100); // Simulation d'une recherche

            var normalizedName = cityName.ToLower()
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ê", "e")
                .Replace("à", "a")
                .Replace("ù", "u")
                .Replace("ô", "o")
                .Replace("î", "i")
                .Replace("ç", "c")
                .Replace("-", " ")
                .Replace("saint ", "saint-")
                .Replace("sur ", "sur-")
                .Replace("sous ", "sous-")
                .Replace("les ", "les-")
                .Replace("la ", "la-")
                .Replace("le ", "le-")
                .Trim();

            // Recherche exacte
            if (_cityCoordinates.TryGetValue(normalizedName, out var coordinates))
            {
                var location = new Location(coordinates.Lat, coordinates.Lon);
                Console.WriteLine($"🏙️ Ville trouvée: {cityName} -> {coordinates.Lat:F4}, {coordinates.Lon:F4}");
                
                LocationChanged?.Invoke(this, new LocationChangedEventArgs 
                { 
                    NewLocation = location, 
                    Source = "City" 
                });
                
                return location;
            }

            // Recherche partielle
            var partialMatch = _cityCoordinates.FirstOrDefault(kvp => 
                kvp.Key.Contains(normalizedName) || normalizedName.Contains(kvp.Key));

            if (!partialMatch.Equals(default(KeyValuePair<string, (double, double)>)))
            {
                var location = new Location(partialMatch.Value.Lat, partialMatch.Value.Lon);
                Console.WriteLine($"🏙️ Ville trouvée (partielle): {cityName} -> {partialMatch.Key} -> {partialMatch.Value.Lat:F4}, {partialMatch.Value.Lon:F4}");
                
                LocationChanged?.Invoke(this, new LocationChangedEventArgs 
                { 
                    NewLocation = location, 
                    Source = "City" 
                });
                
                return location;
            }

            Console.WriteLine($"❌ Ville non trouvée: {cityName}");
            return null;
        }

        private bool IsEmulatorLocation(double latitude, double longitude)
        {
            // Positions d'émulateur connues
            var emulatorLocations = new[]
            {
                (37.4220936, -122.084), // Google Campus (défaut Android)
                (37.7749, -122.4194),   // San Francisco
                (0.0, 0.0),             // Position par défaut
                (1.0, 1.0)              // Position de test
            };

            return emulatorLocations.Any(pos => 
                Math.Abs(latitude - pos.Item1) < 0.01 && 
                Math.Abs(longitude - pos.Item2) < 0.01);
        }

        private async Task<Location> GetRandomFrenchCityAsync()
        {
            var random = new Random();
            var cities = _cityCoordinates.Values.ToArray();
            var randomCity = cities[random.Next(cities.Length)];
            
            var cityName = _cityCoordinates.FirstOrDefault(kvp => 
                Math.Abs(kvp.Value.Lat - randomCity.Lat) < 0.0001 && 
                Math.Abs(kvp.Value.Lon - randomCity.Lon) < 0.0001).Key ?? "ville-inconnue";

            Console.WriteLine($"🎲 Ville française aléatoire: {cityName} ({randomCity.Lat:F4}, {randomCity.Lon:F4})");
            
            var location = new Location(randomCity.Lat, randomCity.Lon);
            
            LocationChanged?.Invoke(this, new LocationChangedEventArgs 
            { 
                NewLocation = location, 
                Source = "Random" 
            });
            
            await Task.Delay(100);
            return location;
        }

        private async Task<Location> GetFallbackLocationAsync(string reason)
        {
            Console.WriteLine($"🆘 Fallback géolocalisation: {reason}");
            
            if (IsEmulator)
            {
                return await GetRandomFrenchCityAsync();
            }
            
            // Position par défaut: Paris
            var fallbackLocation = new Location(48.8566, 2.3522);
            Console.WriteLine("🏛️ Position par défaut: Paris");
            
            LocationChanged?.Invoke(this, new LocationChangedEventArgs 
            { 
                NewLocation = fallbackLocation, 
                Source = "Fallback" 
            });
            
            return fallbackLocation;
        }
    }
}