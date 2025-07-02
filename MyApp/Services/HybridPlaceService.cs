using MyApp.Models;

namespace MyApp.Services
{
    public class HybridPlaceService : IPlaceService
    {
        private readonly OverpassService _overpassService;
        private readonly EnhancedMockPlaceService _fallbackService;

        public HybridPlaceService(HttpClient httpClient)
        {
            _overpassService = new OverpassService(httpClient);
            _fallbackService = new EnhancedMockPlaceService();
        }

        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string? query = null, int radius = 1000, int limit = 20)
        {
            Console.WriteLine($"🔄 HybridService: Tentative avec API Overpass d'abord...");
            
            try
            {
                // Essayer d'abord l'API Overpass (vraies données)
                var overpassResults = await _overpassService.GetNearbyPlacesAsync(latitude, longitude, query, radius, limit);
                
                if (overpassResults.Any())
                {
                    Console.WriteLine($"✅ API Overpass: {overpassResults.Count} lieux trouvés (VRAIES DONNÉES)");
                    
                    // Marquer les lieux comme provenant de l'API réelle
                    foreach (var place in overpassResults)
                    {
                        place.Description = $"[RÉEL] {place.Description}";
                    }
                    
                    return overpassResults;
                }
                else
                {
                    Console.WriteLine("⚠️ API Overpass: Aucun lieu trouvé, basculement vers fallback");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ API Overpass échouée: {ex.Message}, basculement vers fallback");
            }

            // Fallback vers les données de démonstration
            Console.WriteLine("🎭 Utilisation des données de démonstration");
            var fallbackResults = await _fallbackService.GetNearbyPlacesAsync(latitude, longitude, query, radius, limit);
            
            // Marquer clairement les lieux comme étant des données de test
            foreach (var place in fallbackResults)
            {
                place.Description = $"[DÉMO] {place.Description}";
            }
            
            return fallbackResults;
        }

        public async Task<Place?> GetPlaceDetailsAsync(string placeId)
        {
            try
            {
                // Essayer d'abord Overpass
                var overpassResult = await _overpassService.GetPlaceDetailsAsync(placeId);
                if (overpassResult != null)
                {
                    Console.WriteLine($"✅ Détails Overpass pour {placeId}");
                    return overpassResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Détails Overpass échoués: {ex.Message}");
            }

            // Fallback vers mock
            return await _fallbackService.GetPlaceDetailsAsync(placeId);
        }
    }
}