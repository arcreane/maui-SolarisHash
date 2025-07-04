using MyApp.Models;

namespace MyApp.Services
{
    /// <summary>
    /// Service qui utilise UNIQUEMENT de vraies données via Overpass API
    /// Aucune donnée fictive ou de démonstration
    /// </summary>
    public class RealDataOnlyService : IPlaceService
    {
        private readonly OverpassService _overpassService;

        public RealDataOnlyService(HttpClient httpClient)
        {
            _overpassService = new OverpassService(httpClient);
        }

        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string? query = null, int radius = 1000, int limit = 20)
        {
            Console.WriteLine($"🌍 VRAIES DONNÉES UNIQUEMENT: Recherche près de {latitude:F6}, {longitude:F6}");
            
            try
            {
                // Utiliser uniquement l'API Overpass (vraies données OpenStreetMap)
                var places = await _overpassService.GetNearbyPlacesAsync(latitude, longitude, query, radius, limit);
                
                if (places.Any())
                {
                    // Marquer comme données réelles
                    foreach (var place in places)
                    {
                        // Nettoyer les marqueurs de test s'il y en a
                        if (place.Description.StartsWith("[RÉEL]") || place.Description.StartsWith("[DÉMO]"))
                        {
                            place.Description = place.Description.Substring(place.Description.IndexOf("]") + 1).Trim();
                        }
                        
                        // Ajouter un marqueur discret pour indiquer que ce sont de vraies données
                        if (!string.IsNullOrEmpty(place.Description))
                        {
                            place.Description = $"📍 {place.Description}";
                        }
                        else
                        {
                            place.Description = "Données OpenStreetMap en temps réel";
                        }
                    }
                    
                    Console.WriteLine($"✅ {places.Count} VRAIS lieux trouvés dans OpenStreetMap");
                    return places;
                }
                else
                {
                    Console.WriteLine("⚠️ Aucun lieu trouvé dans OpenStreetMap pour cette zone");
                    
                    // Au lieu de fallback fictif, retourner message informatif
                    return new List<Place>
                    {
                        new Place
                        {
                            Id = "no_data_found",
                            Name = "Aucun lieu référencé dans cette zone",
                            Description = "OpenStreetMap ne contient pas de données pour cette localisation. Essayez une zone plus urbaine ou augmentez le rayon de recherche.",
                            Distance = 0,
                            Location = new PlaceLocation
                            {
                                Latitude = latitude,
                                Longitude = longitude,
                                Address = "Zone sans données",
                                FormattedAddress = $"Coordonnées: {latitude:F4}, {longitude:F4}",
                                City = "Zone inconnue",
                                Country = "N/A"
                            },
                            Categories = new List<PlaceCategory>
                            {
                                new PlaceCategory { Id = "info", Name = "Information" }
                            },
                            PhotoUrl = string.Empty
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur API OpenStreetMap: {ex.Message}");
                
                // Message d'erreur informatif au lieu de données fictives
                return new List<Place>
                {
                    new Place
                    {
                        Id = "service_error",
                        Name = "Service temporairement indisponible",
                        Description = $"Impossible d'accéder aux données OpenStreetMap: {ex.Message}. Vérifiez votre connexion internet et réessayez.",
                        Distance = 0,
                        Location = new PlaceLocation
                        {
                            Latitude = latitude,
                            Longitude = longitude,
                            Address = "Service indisponible",
                            FormattedAddress = $"Position: {latitude:F4}, {longitude:F4}",
                            City = "N/A",
                            Country = "N/A"
                        },
                        Categories = new List<PlaceCategory>
                        {
                            new PlaceCategory { Id = "error", Name = "Erreur de service" }
                        },
                        PhotoUrl = string.Empty
                    }
                };
            }
        }

        public async Task<Place?> GetPlaceDetailsAsync(string placeId)
        {
            // Utiliser uniquement l'API Overpass pour les détails
            return await _overpassService.GetPlaceDetailsAsync(placeId);
        }
    }
}