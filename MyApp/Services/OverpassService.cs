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
        
        // URLs Overpass avec priorité
        private readonly List<string> _overpassUrls = new()
        {
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass-api.de/api/interpreter", 
            "https://lz4.overpass-api.de/api/interpreter"
        };

        public OverpassService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TravelBuddy/1.0 (Educational Purpose)");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string? query = null, int radius = 1000, int limit = 20)
        {
            try
            {
                Console.WriteLine($"🌐 RECHERCHE PRÉCISE: {latitude:F6}, {longitude:F6} (rayon: {radius}m, limite: {limit})");
                
                // Construire une requête complète et précise
                var overpassQuery = BuildComprehensiveQuery(latitude, longitude, radius, query);
                
                foreach (var url in _overpassUrls)
                {
                    try
                    {
                        Console.WriteLine($"🔗 Tentative avec {GetServerName(url)}...");
                        
                        var osmResponse = await ExecuteOverpassQueryWithRetry(url, overpassQuery);
                        
                        if (osmResponse?.Elements?.Any() == true)
                        {
                            Console.WriteLine($"📦 Éléments bruts reçus: {osmResponse.Elements.Count}");
                            
                            var places = osmResponse.Elements
                                .Where(e => IsValidPlace(e))
                                .Select(e => Place.FromOsmElement(e, latitude, longitude))
                                .Where(p => p.Location != null && !string.IsNullOrEmpty(p.Name))
                                .Where(p => IsWithinRadius(p, latitude, longitude, radius)) // Vérification double du rayon
                                .OrderBy(p => p.Distance)
                                .Take(limit)
                                .ToList();

                            Console.WriteLine($"✅ {places.Count} lieux valides trouvés avec {GetServerName(url)}");
                            
                            // Log des premiers résultats pour debug
                            foreach (var place in places.Take(5))
                            {
                                Console.WriteLine($"  📍 {place.Name} - {place.FormattedDistance} - {place.Location.Latitude:F4}, {place.Location.Longitude:F4}");
                            }
                            
                            return places;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ {GetServerName(url)}: Réponse vide");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Erreur {GetServerName(url)}: {ex.Message}");
                        continue;
                    }
                    
                    await Task.Delay(1000);
                }

                Console.WriteLine("❌ Tous les serveurs Overpass ont échoué");
                return new List<Place>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Erreur générale: {ex.Message}");
                return new List<Place>();
            }
        }

        private string BuildComprehensiveQuery(double latitude, double longitude, int radius, string? query = null)
        {
            var queryBuilder = new StringBuilder();
            queryBuilder.AppendLine($"[out:json][timeout:45];");
            queryBuilder.AppendLine("(");

            // Requête spécifique si recherche par nom
            if (!string.IsNullOrWhiteSpace(query))
            {
                var escapedQuery = query.Replace("\"", "\\\"");
                queryBuilder.AppendLine($"  node[\"name\"~\"{escapedQuery}\",i](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"name\"~\"{escapedQuery}\",i](around:{radius},{latitude},{longitude});");
            }
            else
            {
                // Requête complète pour tous les types de lieux
                
                // 1. Lieux touristiques
                queryBuilder.AppendLine($"  node[\"tourism\"](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"tourism\"](around:{radius},{latitude},{longitude});");
                
                // 2. Monuments historiques
                queryBuilder.AppendLine($"  node[\"historic\"](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"historic\"](around:{radius},{latitude},{longitude});");
                
                // 3. Restaurants et cafés
                queryBuilder.AppendLine($"  node[\"amenity\"~\"^(restaurant|cafe|bar|pub|fast_food|food_court)$\"](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"amenity\"~\"^(restaurant|cafe|bar|pub|fast_food)$\"](around:{radius},{latitude},{longitude});");
                
                // 4. Services publics
                queryBuilder.AppendLine($"  node[\"amenity\"~\"^(hospital|pharmacy|bank|atm|post_office|police|fire_station)$\"](around:{radius},{latitude},{longitude});");
                
                // 5. Éducation et culture
                queryBuilder.AppendLine($"  node[\"amenity\"~\"^(school|university|college|library|theatre|cinema)$\"](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"amenity\"~\"^(school|university|college|library|theatre)$\"](around:{radius},{latitude},{longitude});");
                
                // 6. Loisirs et parcs
                queryBuilder.AppendLine($"  node[\"leisure\"~\"^(park|garden|playground|sports_centre|swimming_pool|golf_course)$\"](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"leisure\"~\"^(park|garden|sports_centre|golf_course)$\"](around:{radius},{latitude},{longitude});");
                
                // 7. Commerces
                queryBuilder.AppendLine($"  node[\"shop\"~\"^(supermarket|mall|department_store|bakery|butcher|clothes|book|pharmacy)$\"](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"shop\"~\"^(supermarket|mall|department_store)$\"](around:{radius},{latitude},{longitude});");
                
                // 8. Transport
                queryBuilder.AppendLine($"  node[\"public_transport\"~\"^(station|stop_position)$\"](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  node[\"railway\"~\"^(station)$\"](around:{radius},{latitude},{longitude});");
                
                // 9. Lieux de culte
                queryBuilder.AppendLine($"  node[\"amenity\"=\"place_of_worship\"](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"amenity\"=\"place_of_worship\"](around:{radius},{latitude},{longitude});");
                
                // 10. Hébergement
                queryBuilder.AppendLine($"  node[\"tourism\"~\"^(hotel|hostel|guest_house|motel)$\"](around:{radius},{latitude},{longitude});");
            }

            queryBuilder.AppendLine(");");
            queryBuilder.AppendLine("out center geom;");

            var finalQuery = queryBuilder.ToString();
            Console.WriteLine($"🔍 Requête Overpass construite pour {latitude:F4}, {longitude:F4}");
            
            return finalQuery;
        }

        private async Task<OsmResponse?> ExecuteOverpassQueryWithRetry(string url, string query, int maxRetries = 2)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var content = new StringContent(query, Encoding.UTF8, "text/plain");
                    using var response = await _httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            continue;
                        }
                        
                        var osmResponse = JsonConvert.DeserializeObject<OsmResponse>(json);
                        return osmResponse;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine($"⏳ Rate limit, attente {attempt * 3}s...");
                        await Task.Delay(attempt * 3000);
                    }
                    else
                    {
                        Console.WriteLine($"❌ HTTP {response.StatusCode}: {response.ReasonPhrase}");
                    }
                }
                catch (TaskCanceledException) when (attempt < maxRetries)
                {
                    Console.WriteLine($"⏱️ Timeout tentative {attempt}, retry...");
                    await Task.Delay(2000 * attempt);
                }
                catch (HttpRequestException) when (attempt < maxRetries)
                {
                    Console.WriteLine($"🌐 Erreur réseau tentative {attempt}, retry...");
                    await Task.Delay(2000 * attempt);
                }
            }
            
            throw new HttpRequestException($"Impossible de joindre {GetServerName(url)} après {maxRetries} tentatives");
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
[out:json][timeout:20];
(
  node({osmId});
  way({osmId});
  relation({osmId});
);
out geom;";

                foreach (var url in _overpassUrls.Take(2))
                {
                    try
                    {
                        var osmResponse = await ExecuteOverpassQueryWithRetry(url, query, 1);
                        var element = osmResponse?.Elements?.FirstOrDefault();
                        
                        if (element != null && IsValidPlace(element))
                        {
                            return Place.FromOsmElement(element, 0, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Erreur détails avec {GetServerName(url)}: {ex.Message}");
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

        private bool IsValidPlace(OsmElement element)
        {
            // Nom obligatoire et non générique
            if (string.IsNullOrWhiteSpace(element.Name) || 
                element.Name == "Lieu sans nom" ||
                element.Name.Length < 2)
                return false;

            // Position obligatoire (node avec lat/lon OU way/relation avec geometry)
            bool hasValidPosition = false;
            
            if (element.Latitude.HasValue && element.Longitude.HasValue)
            {
                hasValidPosition = true;
            }
            else if (element.Geometry?.Any() == true)
            {
                hasValidPosition = true;
            }
            
            if (!hasValidPosition)
                return false;

            // Au moins une catégorie intéressante
            var hasCategory = !string.IsNullOrEmpty(element.Tourism) ||
                             !string.IsNullOrEmpty(element.Amenity) ||
                             !string.IsNullOrEmpty(element.Historic) ||
                             !string.IsNullOrEmpty(element.Leisure) ||
                             !string.IsNullOrEmpty(element.Shop) ||
                             element.Tags.ContainsKey("public_transport") ||
                             element.Tags.ContainsKey("railway");

            // Exclure les noms trop génériques ou techniques
            var genericNames = new[] { 
                "parking", "wc", "toilettes", "stop", "arrêt", "node", "way", 
                "point", "unnamed", "noname", "sans nom", "????", "???" 
            };
            
            if (genericNames.Any(g => element.Name.ToLower().Contains(g)))
                return false;

            return hasCategory;
        }

        private bool IsWithinRadius(Place place, double centerLat, double centerLon, int radiusMeters)
        {
            if (place.Location == null) return false;
            
            var distance = CalculateDistance(centerLat, centerLon, 
                place.Location.Latitude, place.Location.Longitude);
            
            // Ajouter une petite marge pour compenser les approximations
            return distance <= (radiusMeters + 100);
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371000; // Rayon de la Terre en mètres
            
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

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
    }
}