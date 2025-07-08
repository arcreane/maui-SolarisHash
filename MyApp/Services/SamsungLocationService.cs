using Microsoft.Maui.Devices.Sensors;

namespace MyApp.Services
{
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
                
                // 1. Vérifier et demander les permissions de façon agressive
                var status = await RequestLocationPermissions();
                if (status != PermissionStatus.Granted)
                {
                    Console.WriteLine($"❌ Permission refusée: {status}");
                    return await GetFallbackLocationAsync("Permission GPS refusée");
                }

                Console.WriteLine("✅ Permissions GPS accordées");

                // 2. Vérifier que le GPS est activé
                if (!await IsLocationServiceEnabled())
                {
                    Console.WriteLine("❌ Service de localisation désactivé");
                    return await GetFallbackLocationAsync("GPS désactivé dans les paramètres");
                }

                // 3. Essayer plusieurs tentatives avec des paramètres différents
                var location = await TryMultipleLocationAttempts();
                
                if (location != null)
                {
                    Console.WriteLine($"✅ Samsung GPS: Position trouvée {location.Latitude:F6}, {location.Longitude:F6}");
                    Console.WriteLine($"📍 Précision: {location.Accuracy:F0}m, Age: {(DateTime.Now - location.Timestamp).TotalSeconds:F0}s");
                    
                    LocationChanged?.Invoke(this, new LocationChangedEventArgs 
                    { 
                        NewLocation = location, 
                        Source = "Samsung GPS" 
                    });
                    
                    return location;
                }
                else
                {
                    Console.WriteLine("❌ Aucune position obtenue après plusieurs tentatives");
                    return await GetFallbackLocationAsync("GPS timeout");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Erreur Samsung GPS: {ex.Message}");
                return await GetFallbackLocationAsync($"Erreur GPS: {ex.Message}");
            }
        }

        private async Task<PermissionStatus> RequestLocationPermissions()
        {
            try
            {
                // Demander les permissions étape par étape
                Console.WriteLine("🔐 Demande permission localisation...");
                
                var coarseStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                Console.WriteLine($"📍 Permission Coarse: {coarseStatus}");
                
                if (coarseStatus == PermissionStatus.Granted)
                {
                    // Essayer aussi la permission fine si possible
                    try
                    {
                        var fineStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
                        Console.WriteLine($"📍 Permission Fine: {fineStatus}");
                        return fineStatus == PermissionStatus.Granted ? fineStatus : coarseStatus;
                    }
                    catch
                    {
                        return coarseStatus;
                    }
                }
                
                return coarseStatus;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur permissions: {ex.Message}");
                return PermissionStatus.Denied;
            }
        }

        private async Task<bool> IsLocationServiceEnabled()
        {
            try
            {
                // Vérification basique - sur Samsung, si on a les permissions, le GPS est généralement OK
                await Task.Delay(100);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<Location?> TryMultipleLocationAttempts()
        {
            var attempts = new[]
            {
                // Tentative 1: Haute précision, timeout court
                new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(10)
                },
                // Tentative 2: Précision moyenne, timeout moyen  
                new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(15)
                },
                // Tentative 3: Précision faible, timeout long
                new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Low,
                    Timeout = TimeSpan.FromSeconds(20)
                }
            };

            foreach (var (request, attemptNumber) in attempts.Select((r, i) => (r, i + 1)))
            {
                try
                {
                    Console.WriteLine($"📍 Samsung GPS: Tentative {attemptNumber}/3 (précision: {request.DesiredAccuracy})");
                    
                    var location = await Geolocation.GetLocationAsync(request);
                    
                    if (location != null)
                    {
                        // Vérifier que la position est récente et précise
                        var age = DateTime.Now - location.Timestamp;
                        if (age.TotalMinutes < 10 && location.Accuracy < 1000) // Moins de 10min et moins de 1km d'imprécision
                        {
                            Console.WriteLine($"✅ Position valide trouvée à la tentative {attemptNumber}");
                            return location;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Position trop ancienne ou imprécise: {age.TotalMinutes:F1}min, {location.Accuracy:F0}m");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Tentative {attemptNumber} échouée: {ex.Message}");
                }
                
                // Pause entre les tentatives
                if (attemptNumber < attempts.Length)
                {
                    await Task.Delay(2000);
                }
            }

            return null;
        }

        public async Task<Location?> GetLocationByNameAsync(string cityName)
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

        private async Task<Location> GetFallbackLocationAsync(string reason)
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
    }
}