using Microsoft.Maui.Devices.Sensors;

namespace MyApp.Services
{
    // ✅ CORRECTION: Interface unifiée dans le bon namespace
    public interface ILocationService
    {
        Task<Location?> GetCurrentLocationAsync();
        Task<Location?> GetLocationByNameAsync(string cityName);
        bool IsEmulator { get; }
        event EventHandler<LocationChangedEventArgs> LocationChanged;
    }

    public class LocationChangedEventArgs : EventArgs
    {
        public required Location NewLocation { get; set; }
        public required string Source { get; set; }
    }

    // ✅ CORRECTION: Implémentation correcte de l'interface
    public class SamsungLocationService : ILocationService
    {
        public event EventHandler<LocationChangedEventArgs>? LocationChanged;
        
        // Villes françaises comme fallback
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
            ["lille"] = (50.6292, 3.0573)
        };

        public bool IsEmulator => DeviceInfo.DeviceType == DeviceType.Virtual;

        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                Console.WriteLine($"📍 Samsung GPS: Tentative de géolocalisation...");
                
                // ✅ SÉCURITÉ: Toujours retourner une position par défaut immédiatement
                var fallbackLocation = new Location(48.8566, 2.3522); // Paris
                
                // ✅ PROTECTION: Essayer les permissions avec timeout
                var permissionTask = RequestLocationPermissionsWithTimeout();
                var status = await permissionTask;
                
                if (status != PermissionStatus.Granted)
                {
                    Console.WriteLine($"❌ Permission refusée: {status}");
                    return await GetFallbackLocationAsync("Permission GPS refusée");
                }

                Console.WriteLine("✅ Permissions GPS accordées");

                // ✅ PROTECTION: Essayer la géolocalisation avec timeout court
                var locationTask = TryGetLocationWithTimeout();
                var location = await locationTask;
                
                if (location != null)
                {
                    Console.WriteLine($"✅ Samsung GPS: Position trouvée {location.Latitude:F6}, {location.Longitude:F6}");
                    
                    LocationChanged?.Invoke(this, new LocationChangedEventArgs 
                    { 
                        NewLocation = location, 
                        Source = "Samsung GPS" 
                    });
                    
                    return location;
                }
                else
                {
                    Console.WriteLine("❌ Aucune position obtenue, utilisation fallback");
                    return await GetFallbackLocationAsync("GPS timeout");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Erreur Samsung GPS: {ex.Message}");
                return await GetFallbackLocationAsync($"Erreur GPS: {ex.Message}");
            }
        }

        // ✅ NOUVEAU: Permissions avec timeout pour éviter les blocages
        private async Task<PermissionStatus> RequestLocationPermissionsWithTimeout()
        {
            try
            {
                Console.WriteLine("🔐 Demande permission avec timeout...");
                
                // Timeout de 10 secondes pour les permissions
                var permissionTask = Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                var timeoutTask = Task.Delay(10000);
                
                var completedTask = await Task.WhenAny(permissionTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("⏰ Timeout permissions - fallback");
                    return PermissionStatus.Denied;
                }
                
                var result = await permissionTask;
                Console.WriteLine($"📍 Permission result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur permissions: {ex.Message}");
                return PermissionStatus.Denied;
            }
        }

        // ✅ NOUVEAU: Géolocalisation avec timeout pour éviter les blocages
        private async Task<Location?> TryGetLocationWithTimeout()
        {
            try
            {
                Console.WriteLine("📍 Géolocalisation avec timeout...");
                
                // Essai rapide avec timeout de 8 secondes
                var locationTask = Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(5) // Timeout court
                });
                
                var timeoutTask = Task.Delay(8000); // Timeout de sécurité
                
                var completedTask = await Task.WhenAny(locationTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("⏰ Timeout géolocalisation");
                    return null;
                }
                
                var location = await locationTask;
                
                if (location != null)
                {
                    // Vérifier que la position est raisonnable
                    var age = DateTime.Now - location.Timestamp;
                    if (age.TotalMinutes < 30 && location.Accuracy < 5000) // Moins de 30min et moins de 5km d'imprécision
                    {
                        Console.WriteLine($"✅ Position valide: {location.Latitude:F6}, {location.Longitude:F6}");
                        return location;
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Position trop ancienne ou imprécise: {age.TotalMinutes:F1}min, {location.Accuracy:F0}m");
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur géolocalisation: {ex.Message}");
                return null;
            }
        }

        public async Task<Location?> GetLocationByNameAsync(string cityName)
        {
            try
            {
                await Task.Delay(100);

                var normalizedName = cityName.ToLower()
                    .Replace("é", "e").Replace("è", "e").Replace("ê", "e")
                    .Replace("à", "a").Replace("ù", "u").Replace("ô", "o")
                    .Replace("î", "i").Replace("ç", "c")
                    .Replace("-", " ").Replace("saint ", "saint-")
                    .Trim();

                if (_cityCoordinates.TryGetValue(normalizedName, out var coordinates))
                {
                    var location = new Location(coordinates.Lat, coordinates.Lon);
                    Console.WriteLine($"🏙️ Ville trouvée: {cityName} -> {coordinates.Lat:F4}, {coordinates.Lon:F4}");
                    
                    LocationChanged?.Invoke(this, new LocationChangedEventArgs 
                    { 
                        NewLocation = location, 
                        Source = "City Search" 
                    });
                    
                    return location;
                }

                Console.WriteLine($"❌ Ville non trouvée: {cityName}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur recherche ville: {ex.Message}");
                return null;
            }
        }

        private async Task<Location> GetFallbackLocationAsync(string reason)
        {
            try
            {
                Console.WriteLine($"🆘 Fallback géolocalisation: {reason}");
                
                // Position par défaut: Paris (plutôt qu'une ville aléatoire)
                var fallbackLocation = new Location(48.8566, 2.3522);
                Console.WriteLine("🏛️ Position par défaut: Paris (Champs-Élysées)");
                
                LocationChanged?.Invoke(this, new LocationChangedEventArgs 
                { 
                    NewLocation = fallbackLocation, 
                    Source = $"Fallback ({reason})" 
                });
                
                await Task.Delay(100);
                return fallbackLocation;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur fallback: {ex.Message}");
                return new Location(48.8566, 2.3522); // Paris en cas d'erreur totale
            }
        }
    }
}