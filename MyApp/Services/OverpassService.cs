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
        private const string BaseUrl = "https://overpass-api.de/api/interpreter";
        
        // URLs alternatives en cas de surcharge
        private readonly List<string> _alternativeUrls = new()
        {
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass-api.de/api/interpreter",
            "https://lz4.overpass-api.de/api/interpreter"
        };

        public OverpassService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string query = null, int radius = 1000, int limit = 20)
        {
            try
            {
                var overpassQuery = BuildOverpassQuery(latitude, longitude, radius, query, limit);
                
                foreach (var url in _alternativeUrls)
                {
                    try
                    {
                        var osmResponse = await ExecuteOverpassQuery(url, overpassQuery);
                        if (osmResponse?.Elements?.Any() == true)
                        {
                            var places = osmResponse.Elements
                                .Where(e => IsValidPlace(e))
                                .Select(e => Place.FromOsmElement(e, latitude, longitude))
                                .Where(p => p.Location != null)
                                .OrderBy(p => p.Distance)
                                .Take(limit)
                                .ToList();

                            Console.WriteLine($"✅ Trouvé {places.Count} lieux via {url}");
                            return places;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Erreur avec {url}: {ex.Message}");
                        continue;
                    }
                }

                Console.WriteLine("❌ Aucun serveur Overpass disponible");
                return new List<Place>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur générale dans GetNearbyPlacesAsync: {ex.Message}");
                return new List<Place>();
            }
        }

        public async Task<Place> GetPlaceDetailsAsync(string placeId)
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

                foreach (var url in _alternativeUrls)
                {
                    try
                    {
                        var osmResponse = await ExecuteOverpassQuery(url, query);
                        var element = osmResponse?.Elements?.FirstOrDefault();
                        
                        if (element != null)
                        {
                            // Pour les détails, on utilise une position par défaut
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

        private async Task<OsmResponse> ExecuteOverpassQuery(string url, string query)
        {
            var content = new StringContent(query, Encoding.UTF8, "text/plain");
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<OsmResponse>(json);
            }

            throw new HttpRequestException($"Erreur HTTP {response.StatusCode}: {response.ReasonPhrase}");
        }

        private string BuildOverpassQuery(double latitude, double longitude, int radius, string query = null, int limit = 20)
        {
            var queryBuilder = new StringBuilder();
            queryBuilder.AppendLine($"[out:json][timeout:25];");
            queryBuilder.AppendLine("(");

            // Lieux touristiques
            queryBuilder.AppendLine($"  node[\"tourism\"](around:{radius},{latitude},{longitude});");
            queryBuilder.AppendLine($"  way[\"tourism\"](around:{radius},{latitude},{longitude});");

            // Monuments et sites historiques
            queryBuilder.AppendLine($"  node[\"historic\"](around:{radius},{latitude},{longitude});");
            queryBuilder.AppendLine($"  way[\"historic\"](around:{radius},{latitude},{longitude});");

            // Services et commerces utiles
            queryBuilder.AppendLine($"  node[\"amenity\"~\"^(restaurant|cafe|bar|pub|fast_food|museum|theatre|cinema|library|hospital|pharmacy|bank|atm|fuel|parking|toilets)$\"](around:{radius},{latitude},{longitude});");
            queryBuilder.AppendLine($"  way[\"amenity\"~\"^(restaurant|cafe|bar|pub|fast_food|museum|theatre|cinema|library|hospital|pharmacy|bank|atm|fuel|parking|toilets)$\"](around:{radius},{latitude},{longitude});");

            // Loisirs
            queryBuilder.AppendLine($"  node[\"leisure\"~\"^(park|garden|playground|sports_centre|swimming_pool|golf_course|marina)$\"](around:{radius},{latitude},{longitude});");
            queryBuilder.AppendLine($"  way[\"leisure\"~\"^(park|garden|playground|sports_centre|swimming_pool|golf_course|marina)$\"](around:{radius},{latitude},{longitude});");

            // Commerces intéressants
            queryBuilder.AppendLine($"  node[\"shop\"~\"^(mall|supermarket|convenience|books|clothes|electronics|gifts|jewelry|art)$\"](around:{radius},{latitude},{longitude});");
            queryBuilder.AppendLine($"  way[\"shop\"~\"^(mall|supermarket|convenience|books|clothes|electronics|gifts|jewelry|art)$\"](around:{radius},{latitude},{longitude});");

            // Si une recherche spécifique est demandée
            if (!string.IsNullOrWhiteSpace(query))
            {
                var escapedQuery = query.Replace("\"", "\\\"");
                queryBuilder.AppendLine($"  node[\"name\"~\"{escapedQuery}\",i](around:{radius},{latitude},{longitude});");
                queryBuilder.AppendLine($"  way[\"name\"~\"{escapedQuery}\",i](around:{radius},{latitude},{longitude});");
            }

            queryBuilder.AppendLine(");");
            queryBuilder.AppendLine("out geom;");

            var finalQuery = queryBuilder.ToString();
            Console.WriteLine($"🔍 Requête Overpass: {finalQuery}");
            
            return finalQuery;
        }

        private bool IsValidPlace(OsmElement element)
        {
            // Vérifier qu'on a au moins un nom
            if (string.IsNullOrWhiteSpace(element.Name) || element.Name == "Lieu sans nom")
            {
                return false;
            }

            // Vérifier qu'on a des coordonnées
            if (!element.Latitude.HasValue || !element.Longitude.HasValue)
            {
                if (element.Geometry?.Any() != true)
                {
                    return false;
                }
            }

            // Vérifier qu'on a au moins une catégorie intéressante
            var hasInterestingCategory = !string.IsNullOrEmpty(element.Tourism) ||
                                       !string.IsNullOrEmpty(element.Amenity) ||
                                       !string.IsNullOrEmpty(element.Historic) ||
                                       !string.IsNullOrEmpty(element.Leisure) ||
                                       !string.IsNullOrEmpty(element.Shop);

            return hasInterestingCategory;
        }
    }
}