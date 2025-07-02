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
        public Location NewLocation { get; set; }
        public string Source { get; set; } // "GPS", "Network", "Manual", "City"
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
            ["nancy"] = (48.6921, 6.1844),
            ["argenteuil"] = (48.9474, 2.2514),
            ["montreuil"] = (48.8634, 2.4456),
            ["roubaix"] = (50.6942, 3.1746),
            ["tourcoing"] = (50.7236, 3.1606),
            ["dunkerque"] = (51.0342, 2.3770),
            ["avignon"] = (43.9493, 4.8059),
            ["créteil"] = (48.7904, 2.4551),
            ["poitiers"] = (46.5802, 0.3404),
            ["courbevoie"] = (48.8977, 2.2547),
            ["versailles"] = (48.8014, 2.1301),
            ["colombes"] = (48.9225, 2.2581),
            ["fort-de-france"] = (14.6037, -61.0594),
            ["aulnay-sous-bois"] = (48.9346, 2.4969),
            ["asnières-sur-seine"] = (48.9145, 2.2847),
            ["rueil-malmaison"] = (48.8784, 2.1942),
            ["pau"] = (43.2965, -0.3706),
            ["aubervilliers"] = (48.9146, 2.3836),
            ["champigny-sur-marne"] = (48.8169, 2.5145),
            ["antibes"] = (43.5808, 7.1251),
            ["la rochelle"] = (46.1603, -1.1511),
            ["cannes"] = (43.5528, 7.0174),
            ["boulogne-billancourt"] = (48.8346, 2.2402),
            ["calais"] = (50.9581, 1.8503),
            ["drancy"] = (48.9245, 2.4453),
            ["ajaccio"] = (41.9194, 8.7389),
            ["mérignac"] = (44.8404, -0.6463),
            ["saint-maur-des-fossés"] = (48.8114, 2.4875),
            ["noisy-le-grand"] = (48.8433, 2.5531),
            ["colmar"] = (48.0794, 7.3581),
            ["bourges"] = (47.0840, 2.3964),
            ["la seyne-sur-mer"] = (43.1014, 5.8842),
            ["quimper"] = (47.9960, -4.1093),
            ["valence"] = (44.9267, 4.8916),
            ["pessac"] = (44.8063, -0.6306),
            ["ivry-sur-seine"] = (48.8139, 2.3848),
            ["cergy"] = (49.0354, 2.0772),
            ["antony"] = (48.7548, 2.2975),
            ["troyes"] = (48.2973, 4.0744),
            ["issy-les-moulineaux"] = (48.8239, 2.2747),
            ["montauban"] = (44.0218, 1.3528),
            ["bourget"] = (48.9344, 2.4283),
            ["lorient"] = (47.7482, -3.3650),
            ["sarcelles"] = (48.9978, 2.3705),
            ["saint-nazaire"] = (47.2861, -2.2127),
            ["vénissieux"] = (45.7053, 4.8914),
            ["clichy"] = (48.9045, 2.3058),
            ["corbeil-essonnes"] = (48.6098, 2.4813),
            ["bayonne"] = (43.4832, -1.4752),
            ["draguignan"] = (43.5383, 6.4678),
            ["meudon"] = (48.8136, 2.2364),
            ["saint-ouen"] = (48.9045, 2.3347),
            ["saint-quentin"] = (49.8476, 3.2803),
            ["châteauroux"] = (46.8081, 1.6914),
            ["charleville-mézières"] = (49.7711, 4.7197),
            ["laval"] = (48.0698, -0.7667),
            ["albi"] = (43.9298, 2.1480),
            ["sartrouville"] = (48.9386, 2.1608),
            ["châlons-en-champagne"] = (48.9567, 4.3634),
            ["massy"] = (48.7308, 2.2747),
            ["sevran"] = (48.9406, 2.5331),
            ["les sables-d'olonne"] = (46.4961, -1.7825),
            ["vincennes"] = (48.8481, 2.4386),
            ["châtellerault"] = (46.8175, 0.5464),
            ["salon-de-provence"] = (43.6419, 5.0981),
            ["sète"] = (43.4031, 3.6947),
            ["roanne"] = (46.0344, 4.0686),
            ["franconville"] = (48.9897, 2.2281),
            ["livry-gargan"] = (48.9169, 2.5331),
            ["choisy-le-roi"] = (48.7619, 2.4069),
            ["castres"] = (43.6053, 2.2400),
            ["brive-la-gaillarde"] = (45.1581, 1.5339),
            ["villejuif"] = (48.7886, 2.3661),
            ["cagnes-sur-mer"] = (43.6636, 7.1481)
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