using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MyApp.Models;
using System.Linq;

namespace MyApp.Services
{
    public class FoursquarePlaceService : IPlaceService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.foursquare.com/v3/";
        private const string ApiKey = "YOUR_FOURSQUARE_API_KEY"; // Remplacez par votre clé API Foursquare
        
        public FoursquarePlaceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", ApiKey);
        }

        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string query = null, int radius = 1000, int limit = 20)
        {
            try
            {
                // Construction de la requête
                var requestUrl = $"places/search?ll={latitude},{longitude}&radius={radius}&limit={limit}";
                
                // Ajouter le terme de recherche s'il existe
                if (!string.IsNullOrWhiteSpace(query))
                {
                    requestUrl += $"&query={Uri.EscapeDataString(query)}";
                }

                // Exécution de la requête
                var response = await _httpClient.GetAsync(requestUrl);
                
                // Vérification de la réponse
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);
                    
                    // Extraction des résultats
                    var results = json["results"]?.ToObject<List<Place>>() ?? new List<Place>();
                    
                    // Récupération des photos pour chaque lieu (dans un cas réel, on ferait une autre requête)
                    foreach (var place in results)
                    {
                        // Pour éviter trop d'appels API, on utilise une URL générique pour les photos
                        // Dans une application réelle, on utiliserait un autre endpoint pour récupérer les photos
                        place.PhotoUrl = "https://fastly.4sqi.net/img/general/612x612/655020_WT3qs5u7YFinLGgcXu7gKGPe6w8vENWlT5ZP31n6O-Y.jpg";
                    }
                    
                    return results;
                }
                
                Console.WriteLine($"Erreur API: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return new List<Place>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans GetNearbyPlacesAsync: {ex.Message}");
                return new List<Place>();
            }
        }

        public async Task<Place> GetPlaceDetailsAsync(string placeId)
        {
            try
            {
                // Exécution de la requête
                var response = await _httpClient.GetAsync($"places/{placeId}");
                
                // Vérification de la réponse
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Place>(content);
                }
                
                Console.WriteLine($"Erreur API: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans GetPlaceDetailsAsync: {ex.Message}");
                return null;
            }
        }
    }
}