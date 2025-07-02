using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MyApp.Models;
using Newtonsoft.Json;

namespace MyApp.Services
{
    public class OverpassService : IPlaceService
    {
        private readonly HttpClient _httpClient;
        
        // URLs alternatives avec priorité
        private readonly List<string> _overpassUrls = new()
        {
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass-api.de/api/interpreter",
            "https://lz4.overpass-api.de/api/interpreter"
        };

        public OverpassService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(45); // Timeout plus long
            
            // Headers pour éviter les blocages
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TravelBuddy/1.0 (Mobile App)");
        }

        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string query = null, int radius = 1000, int limit = 20)
        {
            try
            {
                Console.WriteLine($"🌐 Recherche de lieux près de {latitude:F6}, {longitude:F6} (rayon: {radius}m)");
                
                var overpassQuery = BuildOverpassQuery(latitude, longitude, radius, query, limit);
                
                foreach (var url in _overpassUrls)
                {
                    try
                    {
                        Console.WriteLine($"🔗 Tentative avec {url}...");
                        
                        var osmResponse = await ExecuteOverpassQueryWithRetry(url, overpassQuery);
                        
                        if (osmResponse?.Elements?.Any() == true)
                        {
                            var places = osmResponse.Elements
                                .Where(e => IsValidPlace(e))
                                .Select(e => Place.FromOsmElement(e, latitude, longitude))
                                .Where(p => p.Location != null)
                                .OrderBy(p => p.Distance)
                                .Take(limit)
                                .ToList();

                            Console.WriteLine($"✅ Succès avec {url}: {places.Count} lieux trouvés");
                            return places;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ {url}: Réponse vide ou invalide");
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                        Console.WriteLine($"⏱️ Timeout avec {url}: {ex.Message}");
                        continue;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"🌐 Erreur réseau avec {url}: {ex.Message}");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Erreur inattendue avec {url}: {ex.Message}");
                        continue;
                    }
                    
                    // Pause entre les tentatives
                    await Task.Delay(1000);
                }

                Console.WriteLine("❌ Tous les serveurs Overpass ont échoué");
                
                // Retourner des données de fallback au lieu d'une liste vide
                return GetFallbackPlaces(latitude, longitude, query);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Erreur générale dans GetNearbyPlacesAsync: {ex.Message}");
                return GetFallbackPlaces(latitude, longitude, query);
            }
        }

        private async Task<OsmResponse?> ExecuteOverpassQueryWithRetry(string url, string query, int maxRetries = 2)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"🔄 Tentative {attempt}/{maxRetries} pour {url}");
                    
                    using var content = new StringContent(query, Encoding.UTF8, "text/plain");
                    using var response = await _httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            Console.WriteLine($"⚠️ Réponse vide de {url}");
                            continue;
                        }
                        
                        var osmResponse = JsonConvert.DeserializeObject<OsmResponse>(json);
                        Console.WriteLine($"✅ Réponse valide de {url}: {osmResponse?.Elements?.Count ?? 0} éléments");
                        return osmResponse;
                    }
                    else
                    {
                        Console.WriteLine($"❌ HTTP {response.StatusCode} de {url}: {response.ReasonPhrase}");
                    }
                }
                catch (TaskCanceledException) when (attempt < maxRetries)
                {
                    Console.WriteLine($"⏱️ Timeout tentative {attempt}, retry dans 2s...");
                    await Task.Delay(2000);
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"❌ Erreur tentative {attempt}: {ex.Message}, retry dans 2s...");
                    await Task.Delay(2000);
                }
            }
            
            throw new HttpRequestException($"Impossible de joindre {url} après {maxRetries} tentatives");
        }

        public async Task<Place?> GetPlaceDetailsAsync(string placeId)
        {
            try
            {
                if (!long.TryParse(placeId, out long osmId))
                {
                    return null;
                }

                var query = $@"
                [out:json][timeout:15];
                (
                  node({osmId});
                  way({osmId});
                );
                out geom;";

                foreach (var url in _overpassUrls)
                {
                    try
                    {
                        var osmResponse = await ExecuteOverpassQueryWithRetry(url, query, 1);
                        var element = osmResponse?.Elements?.FirstOrDefault();
                        
                        if (element != null)
                        {
                            return Place.FromOsmElement(element, 0, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Erreur détails avec {url}: {ex.Message}");
                        continue;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur dans GetPlaceDetailsAsync: {ex.Message}");
                return null;
            }
        }

        private string BuildOverpassQuery(double latitude, double longitude, int radius, string? query = null, int limit = 20)
        {
            var queryBuilder = new StringBuilder();
            queryBuilder.AppendLine($"[out:json][timeout:30];"); // Timeout plus long
            queryBuilder.AppendLine("(");

            // Requête simplifiée pour réduire la charge
            queryBuilder.AppendLine($"  node[\"tourism\"](around:{radius},{latitude},{longitude});");
            queryBuilder.AppendLine($"  node[\"amenity\"~\"^(restaurant|cafe|museum|pharmacy|bank|hospital)$\"](around:{radius},{latitude},{longitude});");
            queryBuilder.AppendLine($"  node[\"historic\"](around:{radius},{latitude},{longitude});");
            queryBuilder.AppendLine($"  node[\"leisure\"~\"^(park|garden)$\"](around:{radius},{latitude},{longitude});");

            // Recherche spécifique si demandée
            if (!string.IsNullOrWhiteSpace(query))
            {
                var escapedQuery = query.Replace("\"", "\\\"");
                queryBuilder.AppendLine($"  node[\"name\"~\"{escapedQuery}\",i](around:{radius},{latitude},{longitude});");
            }

            queryBuilder.AppendLine(");");
            queryBuilder.AppendLine("out geom;");

            var finalQuery = queryBuilder.ToString();
            Console.WriteLine($"🔍 Requête Overpass simplifiée: {finalQuery.Replace("\n", " ").Replace("  ", " ")}");
            
            return finalQuery;
        }

        private bool IsValidPlace(OsmElement element)
        {
            if (string.IsNullOrWhiteSpace(element.Name) || element.Name == "Lieu sans nom")
                return false;

            if (!element.Latitude.HasValue || !element.Longitude.HasValue)
            {
                if (element.Geometry?.Any() != true)
                    return false;
            }

            var hasInterestingCategory = !string.IsNullOrEmpty(element.Tourism) ||
                                       !string.IsNullOrEmpty(element.Amenity) ||
                                       !string.IsNullOrEmpty(element.Historic) ||
                                       !string.IsNullOrEmpty(element.Leisure) ||
                                       !string.IsNullOrEmpty(element.Shop);

            return hasInterestingCategory;
        }

        private List<Place> GetFallbackPlaces(double latitude, double longitude, string? query)
        {
            Console.WriteLine("🆘 Utilisation des données de fallback");
            
            // Données de démonstration basées sur la position
            var fallbackPlaces = new List<Place>
            {
                new Place
                {
                    Id = "fallback_1",
                    Name = $"Lieu touristique près de {latitude:F2}°, {longitude:F2}°",
                    Description = "Données de démonstration - API Overpass indisponible",
                    Distance = 100,
                    Location = new PlaceLocation
                    {
                        Latitude = latitude + 0.001,
                        Longitude = longitude + 0.001,
                        Address = "Position approximative",
                        FormattedAddress = $"Près de {latitude:F4}, {longitude:F4}",
                        City = "Ville inconnue",
                        Country = "Pays inconnu"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "demo", Name = "Démonstration" }
                    },
                    PhotoUrl = "https://via.placeholder.com/400x300/FF6B6B/FFFFFF?text=Demo"
                }
            };

            if (!string.IsNullOrEmpty(query))
            {
                fallbackPlaces[0].Name = $"Recherche '{query}' - Service indisponible";
            }

            return fallbackPlaces;
        }
    }
}