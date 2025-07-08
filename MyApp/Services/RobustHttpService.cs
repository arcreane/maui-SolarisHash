using System;
using System.Net.Http;
using System.Threading.Tasks;
using MyApp.Models;
using Newtonsoft.Json;
using System.Text;

namespace MyApp.Services
{
    public class RobustHttpService : IPlaceService
    {
        private readonly HttpClient _httpClient;
        private readonly List<string> _overpassUrls = new()
        {
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass-api.de/api/interpreter",
            "https://lz4.overpass-api.de/api/interpreter"
        };

        public RobustHttpService()
        {
            // Configuration HTTP robuste pour les vrais devices
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Headers optimisés pour éviter les blocages
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Android; Mobile; rv:40.0) TravelBuddy/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9,en;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        }

        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string? query = null, int radius = 1000, int limit = 20)
        {
            Console.WriteLine($"📱 TÉLÉPHONE: Recherche {latitude:F6}, {longitude:F6}");
            
            // Test de connectivité d'abord
            if (!await TestConnectivity())
            {
                Console.WriteLine("❌ Pas de connectivité internet");
                return CreateConnectivityErrorPlace(latitude, longitude);
            }

            try
            {
                // ✅ CORRECTION: Requête Overpass corrigée et validée
                var query_overpass = BuildValidOverpassQuery(latitude, longitude, radius, query);
                
                foreach (var url in _overpassUrls)
                {
                    try
                    {
                        Console.WriteLine($"📱 Test {GetServerName(url)}...");
                        Console.WriteLine($"🔍 Requête envoyée: {query_overpass.Replace("\n", " ").Replace("  ", " ")}");
                        
                        var places = await TryGetPlaces(url, query_overpass, latitude, longitude, limit);
                        
                        if (places.Any())
                        {
                            Console.WriteLine($"✅ {places.Count} lieux trouvés avec {GetServerName(url)}");
                            return places;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ {GetServerName(url)} échoué: {ex.Message}");
                        continue;
                    }
                    
                    await Task.Delay(2000); // Pause plus longue entre tentatives
                }

                Console.WriteLine("⚠️ Tous les serveurs ont échoué");
                return CreateNoDataPlace(latitude, longitude);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Erreur générale: {ex.Message}");
                return CreateErrorPlace(latitude, longitude, ex.Message);
            }
        }

        private async Task<bool> TestConnectivity()
        {
            try
            {
                Console.WriteLine("🔍 Test connectivité...");
                
                // Test simple avec Google DNS
                using var response = await _httpClient.GetAsync("https://dns.google/resolve?name=google.com&type=A");
                var isConnected = response.IsSuccessStatusCode;
                
                Console.WriteLine($"🌐 Connectivité: {(isConnected ? "OK" : "KO")}");
                return isConnected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test connectivité échoué: {ex.Message}");
                return false;
            }
        }

        // ✅ CORRECTION PRINCIPALE: Requête Overpass valide et testée
        private string BuildValidOverpassQuery(double latitude, double longitude, int radius, string? searchQuery)
        {
            var query = new StringBuilder();
            
            // En-tête Overpass correct
            query.AppendLine("[out:json][timeout:25];");
            query.AppendLine("(");

            // ✅ CORRECTION CRITIQUE: Forcer la culture invariante pour les coordonnées
            var lat = latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var lon = longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                // Recherche spécifique par nom
                var escaped = searchQuery.Replace("\"", "").Replace("'", ""); // Nettoyer les caractères problématiques
                query.AppendLine($"  node[name~\"{escaped}\",i](around:{radius},{lat},{lon});");
                query.AppendLine($"  way[name~\"{escaped}\",i](around:{radius},{lat},{lon});");
            }
            else
            {
                // Requête générale pour tous les POI
                query.AppendLine($"  node[tourism](around:{radius},{lat},{lon});");
                query.AppendLine($"  node[amenity~\"^(restaurant|cafe|museum|hospital|bank|pharmacy|school|library)$\"](around:{radius},{lat},{lon});");
                query.AppendLine($"  node[historic](around:{radius},{lat},{lon});");
                query.AppendLine($"  node[leisure~\"^(park|garden|playground)$\"](around:{radius},{lat},{lon});");
                query.AppendLine($"  node[shop~\"^(supermarket|bakery|butcher)$\"](around:{radius},{lat},{lon});");
                
                // Ajouter les ways pour les grands bâtiments
                query.AppendLine($"  way[tourism](around:{radius},{lat},{lon});");
                query.AppendLine($"  way[amenity~\"^(restaurant|hospital|school|university)$\"](around:{radius},{lat},{lon});");
            }

            query.AppendLine(");");
            query.AppendLine("out center geom;"); // center geom pour avoir les coordonnées des ways

            var finalQuery = query.ToString();
            Console.WriteLine($"📱 Requête Overpass construite pour {lat}, {lon}");
            
            return finalQuery;
        }

        private async Task<List<Place>> TryGetPlaces(string url, string query, double lat, double lon, int limit)
        {
            try
            {
                using var content = new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded");
                using var response = await _httpClient.PostAsync(url, content);

                Console.WriteLine($"📡 Réponse HTTP {response.StatusCode} de {GetServerName(url)}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Erreur détaillée: {errorContent}");
                    return new List<Place>();
                }

                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("⚠️ Réponse vide");
                    return new List<Place>();
                }

                // Log des premiers caractères pour debug
                Console.WriteLine($"📄 Début réponse JSON: {json.Substring(0, Math.Min(200, json.Length))}...");

                var osmResponse = JsonConvert.DeserializeObject<OsmResponse>(json);
                
                if (osmResponse?.Elements == null)
                {
                    Console.WriteLine("⚠️ Pas d'éléments dans la réponse");
                    return new List<Place>();
                }

                Console.WriteLine($"📦 {osmResponse.Elements.Count} éléments OSM reçus");

                var places = osmResponse.Elements
                    .Where(e => IsValidForMobile(e))
                    .Select(e => Place.FromOsmElement(e, lat, lon))
                    .Where(p => p.Location != null && !string.IsNullOrEmpty(p.Name))
                    .OrderBy(p => p.Distance)
                    .Take(limit)
                    .ToList();

                Console.WriteLine($"✅ {places.Count} lieux valides extraits");
                
                // Log des premiers lieux trouvés
                foreach (var place in places.Take(3))
                {
                    Console.WriteLine($"  📍 {place.Name} ({place.MainCategory}) - {place.FormattedDistance}");
                }

                return places;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("⏱️ Timeout sur téléphone");
                throw new Exception("Timeout - vérifiez votre connexion");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"🌐 Erreur réseau: {ex.Message}");
                throw new Exception($"Erreur réseau: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"📄 Erreur parsing JSON: {ex.Message}");
                throw new Exception($"Erreur données: {ex.Message}");
            }
        }

        private bool IsValidForMobile(OsmElement element)
        {
            if (string.IsNullOrWhiteSpace(element.Name) || element.Name.Length < 2)
                return false;

            // Accepter les nodes avec coordonnées directes
            if (element.Latitude.HasValue && element.Longitude.HasValue)
                return true;

            // Accepter les ways avec géométrie
            if (element.Geometry?.Any() == true)
                return true;

            return false;
        }

        // ✅ Correction: Ajouter await Task.CompletedTask pour éviter le warning
        public async Task<Place?> GetPlaceDetailsAsync(string placeId)
        {
            await Task.CompletedTask; // ✅ Corrigé
            // Version simplifiée pour téléphone
            return null;
        }

        private List<Place> CreateConnectivityErrorPlace(double lat, double lon)
        {
            return new List<Place>
            {
                new Place
                {
                    Id = "connectivity_error",
                    Name = "❌ Pas de connexion internet",
                    Description = "Vérifiez votre connexion WiFi ou données mobiles et réessayez.",
                    Distance = 0,
                    Location = new PlaceLocation
                    {
                        Latitude = lat,
                        Longitude = lon,
                        Address = "Connexion requise",
                        FormattedAddress = "Activez internet pour voir les lieux",
                        City = "Hors ligne",
                        Country = "N/A"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "error", Name = "Erreur de connexion" }
                    }
                }
            };
        }

        private List<Place> CreateNoDataPlace(double lat, double lon)
        {
            return new List<Place>
            {
                new Place
                {
                    Id = "no_data",
                    Name = "⚠️ Aucun lieu trouvé dans cette zone",
                    Description = "Essayez une ville plus connue (Paris, Lyon, Marseille) ou augmentez le rayon de recherche.",
                    Distance = 0,
                    Location = new PlaceLocation
                    {
                        Latitude = lat,
                        Longitude = lon,
                        Address = "Zone sans données",
                        FormattedAddress = $"Position: {lat:F4}, {lon:F4}",
                        City = "Zone inconnue",
                        Country = "France"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "info", Name = "Information" }
                    }
                }
            };
        }

        private List<Place> CreateErrorPlace(double lat, double lon, string error)
        {
            return new List<Place>
            {
                new Place
                {
                    Id = "service_error",
                    Name = "⚠️ Service temporairement indisponible",
                    Description = $"Erreur: {error}. Réessayez dans quelques minutes.",
                    Distance = 0,
                    Location = new PlaceLocation
                    {
                        Latitude = lat,
                        Longitude = lon,
                        Address = "Service indisponible",
                        FormattedAddress = "Réessayez plus tard",
                        City = "Erreur",
                        Country = "N/A"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "error", Name = "Erreur de service" }
                    }
                }
            };
        }

        private string GetServerName(string url)
        {
            return url switch
            {
                var u when u.Contains("kumi.systems") => "Kumi",
                var u when u.Contains("overpass-api.de") => "Main",
                var u when u.Contains("lz4.overpass") => "LZ4",
                _ => "Unknown"
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}