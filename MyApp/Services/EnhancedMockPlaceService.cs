using MyApp.Models;

namespace MyApp.Services
{
    public class EnhancedMockPlaceService : IPlaceService
    {
        private readonly Dictionary<string, List<Place>> _placesByRegion;

        public EnhancedMockPlaceService()
        {
            _placesByRegion = InitializePlaceData();
        }

        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string? query = null, int radius = 1000, int limit = 20)
        {
            await Task.Delay(1000); // Simulation délai réseau

            Console.WriteLine($"🎭 MockService: Recherche près de {latitude:F4}, {longitude:F4}");

            var region = DetermineRegion(latitude, longitude);
            Console.WriteLine($"🗺️ Région détectée: {region}");

            var places = _placesByRegion.GetValueOrDefault(region, _placesByRegion["default"]);

            // Ajuster les coordonnées et distances par rapport à la position
            var adjustedPlaces = places.Select(p => AdjustPlaceLocation(p, latitude, longitude)).ToList();

            // Filtrer par requête si fournie
            if (!string.IsNullOrWhiteSpace(query))
            {
                adjustedPlaces = adjustedPlaces.Where(p => 
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Categories.Any(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var result = adjustedPlaces.OrderBy(p => p.Distance).Take(limit).ToList();
            Console.WriteLine($"🎭 MockService: Retourne {result.Count} lieux pour {region}");

            return result;
        }

        public async Task<Place?> GetPlaceDetailsAsync(string placeId)
        {
            await Task.Delay(500);
            
            var allPlaces = _placesByRegion.Values.SelectMany(places => places);
            return allPlaces.FirstOrDefault(p => p.Id == placeId);
        }

        private string DetermineRegion(double latitude, double longitude)
        {
            // Paris et région parisienne
            if (latitude >= 48.5 && latitude <= 49.0 && longitude >= 2.0 && longitude <= 3.0)
                return "paris";
            
            // Lyon
            if (latitude >= 45.5 && latitude <= 46.0 && longitude >= 4.5 && longitude <= 5.0)
                return "lyon";
            
            // Marseille
            if (latitude >= 43.0 && latitude <= 43.5 && longitude >= 5.0 && longitude <= 5.5)
                return "marseille";
            
            // Nice/Côte d'Azur
            if (latitude >= 43.5 && latitude <= 44.0 && longitude >= 7.0 && longitude <= 7.5)
                return "nice";
            
            // Nord (Lille, Tourcoing, Roubaix)
            if (latitude >= 50.5 && latitude <= 51.0 && longitude >= 2.8 && longitude <= 3.5)
                return "nord";
            
            // Californie (pour les tests d'émulateur)
            if (latitude >= 37.0 && latitude <= 38.0 && longitude >= -123.0 && longitude <= -121.0)
                return "california";

            return "default";
        }

        private Place AdjustPlaceLocation(Place originalPlace, double userLat, double userLon)
        {
            // Créer une copie avec position ajustée
            var adjustedPlace = new Place
            {
                Id = originalPlace.Id,
                Name = originalPlace.Name,
                Description = originalPlace.Description,
                Categories = originalPlace.Categories,
                PhotoUrl = originalPlace.PhotoUrl,
                Tourism = originalPlace.Tourism,
                Amenity = originalPlace.Amenity,
                Historic = originalPlace.Historic,
                Leisure = originalPlace.Leisure,
                Shop = originalPlace.Shop,
                Website = originalPlace.Website,
                Phone = originalPlace.Phone,
                OpeningHours = originalPlace.OpeningHours,
                OsmTags = originalPlace.OsmTags
            };

            // Ajuster la position par rapport à l'utilisateur
            var random = new Random(originalPlace.Id.GetHashCode());
            var offsetLat = (random.NextDouble() - 0.5) * 0.02; // ±1km environ
            var offsetLon = (random.NextDouble() - 0.5) * 0.02;

            adjustedPlace.Location = new PlaceLocation
            {
                Latitude = userLat + offsetLat,
                Longitude = userLon + offsetLon,
                Address = originalPlace.Location.Address,
                FormattedAddress = originalPlace.Location.FormattedAddress,
                City = originalPlace.Location.City,
                Country = originalPlace.Location.Country
            };

            // Calculer la vraie distance
            adjustedPlace.Distance = CalculateDistance(userLat, userLon, 
                adjustedPlace.Location.Latitude, adjustedPlace.Location.Longitude);

            return adjustedPlace;
        }

        private static int CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return (int)(earthRadius * c * 1000);
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        private Dictionary<string, List<Place>> InitializePlaceData()
        {
            return new Dictionary<string, List<Place>>
            {
                ["paris"] = new List<Place>
                {
                    new Place { Id = "paris_1", Name = "Tour Eiffel", Description = "Monument emblématique de Paris", Categories = new List<PlaceCategory> { new() { Name = "Monument" } }, Location = new PlaceLocation { Address = "Champ de Mars", City = "Paris", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/FFD700/000000?text=Tour+Eiffel" },
                    new Place { Id = "paris_2", Name = "Musée du Louvre", Description = "Plus grand musée d'art du monde", Categories = new List<PlaceCategory> { new() { Name = "Musée" } }, Location = new PlaceLocation { Address = "Rue de Rivoli", City = "Paris", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/4169E1/FFFFFF?text=Louvre" },
                    new Place { Id = "paris_3", Name = "Café de Flore", Description = "Café historique de Saint-Germain", Categories = new List<PlaceCategory> { new() { Name = "Café" } }, Location = new PlaceLocation { Address = "172 Boulevard Saint-Germain", City = "Paris", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/8B4513/FFFFFF?text=Cafe" },
                    new Place { Id = "paris_4", Name = "Jardin du Luxembourg", Description = "Magnifique parc parisien", Categories = new List<PlaceCategory> { new() { Name = "Parc" } }, Location = new PlaceLocation { Address = "Rue de Médicis", City = "Paris", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/228B22/FFFFFF?text=Jardin" },
                    new Place { Id = "paris_5", Name = "Notre-Dame", Description = "Cathédrale gothique emblématique", Categories = new List<PlaceCategory> { new() { Name = "Monument" } }, Location = new PlaceLocation { Address = "6 Parvis Notre-Dame", City = "Paris", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/8B4513/FFFFFF?text=Notre-Dame" }
                },
                ["lyon"] = new List<Place>
                {
                    new Place { Id = "lyon_1", Name = "Basilique Notre-Dame de Fourvière", Description = "Basilique sur la colline de Fourvière", Categories = new List<PlaceCategory> { new() { Name = "Monument" } }, Location = new PlaceLocation { Address = "8 Place de Fourvière", City = "Lyon", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/DAA520/FFFFFF?text=Fourviere" },
                    new Place { Id = "lyon_2", Name = "Vieux Lyon", Description = "Quartier Renaissance", Categories = new List<PlaceCategory> { new() { Name = "Quartier historique" } }, Location = new PlaceLocation { Address = "Vieux Lyon", City = "Lyon", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/CD853F/FFFFFF?text=Vieux+Lyon" },
                    new Place { Id = "lyon_3", Name = "Parc de la Tête d'Or", Description = "Grand parc urbain avec zoo", Categories = new List<PlaceCategory> { new() { Name = "Parc" } }, Location = new PlaceLocation { Address = "Place Général Leclerc", City = "Lyon", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/32CD32/FFFFFF?text=Parc" },
                    new Place { Id = "lyon_4", Name = "Bouchon lyonnais", Description = "Restaurant traditionnel lyonnais", Categories = new List<PlaceCategory> { new() { Name = "Restaurant" } }, Location = new PlaceLocation { Address = "Rue Mercière", City = "Lyon", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/FF4500/FFFFFF?text=Bouchon" }
                },
                ["marseille"] = new List<Place>
                {
                    new Place { Id = "marseille_1", Name = "Notre-Dame de la Garde", Description = "Basilique sur la colline", Categories = new List<PlaceCategory> { new() { Name = "Monument" } }, Location = new PlaceLocation { Address = "Rue Fort du Sanctuaire", City = "Marseille", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/4682B4/FFFFFF?text=Garde" },
                    new Place { Id = "marseille_2", Name = "Vieux-Port", Description = "Port historique de Marseille", Categories = new List<PlaceCategory> { new() { Name = "Port" } }, Location = new PlaceLocation { Address = "Quai du Port", City = "Marseille", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/1E90FF/FFFFFF?text=Port" },
                    new Place { Id = "marseille_3", Name = "Calanques", Description = "Parc national des Calanques", Categories = new List<PlaceCategory> { new() { Name = "Parc naturel" } }, Location = new PlaceLocation { Address = "Calanques", City = "Marseille", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/20B2AA/FFFFFF?text=Calanques" }
                },
                ["nord"] = new List<Place>
                {
                    new Place { Id = "nord_1", Name = "Palais des Beaux-Arts", Description = "Musée d'art de Lille", Categories = new List<PlaceCategory> { new() { Name = "Musée" } }, Location = new PlaceLocation { Address = "Place de la République", City = "Lille", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/8A2BE2/FFFFFF?text=Musee" },
                    new Place { Id = "nord_2", Name = "Vieille Bourse", Description = "Monument historique lillois", Categories = new List<PlaceCategory> { new() { Name = "Monument" } }, Location = new PlaceLocation { Address = "Place du Général de Gaulle", City = "Lille", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/B8860B/FFFFFF?text=Bourse" },
                    new Place { Id = "nord_3", Name = "Parc de la Citadelle", Description = "Grand parc urbain de Lille", Categories = new List<PlaceCategory> { new() { Name = "Parc" } }, Location = new PlaceLocation { Address = "Avenue du 43e Régiment d'Infanterie", City = "Lille", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/228B22/FFFFFF?text=Parc" },
                    new Place { Id = "nord_4", Name = "Ch'ti Bistrot", Description = "Restaurant typique du Nord", Categories = new List<PlaceCategory> { new() { Name = "Restaurant" } }, Location = new PlaceLocation { Address = "Rue de Béthune", City = "Lille", Country = "France" }, PhotoUrl = "https://via.placeholder.com/400x300/FF6347/FFFFFF?text=Resto" }
                },
                ["california"] = new List<Place>
                {
                    new Place { Id = "ca_1", Name = "Googleplex", Description = "Siège social de Google", Categories = new List<PlaceCategory> { new() { Name = "Entreprise" } }, Location = new PlaceLocation { Address = "1600 Amphitheatre Parkway", City = "Mountain View", Country = "USA" }, PhotoUrl = "https://via.placeholder.com/400x300/4285F4/FFFFFF?text=Google" },
                    new Place { Id = "ca_2", Name = "Stanford University", Description = "Université prestigieuse", Categories = new List<PlaceCategory> { new() { Name = "Université" } }, Location = new PlaceLocation { Address = "450 Serra Mall", City = "Stanford", Country = "USA" }, PhotoUrl = "https://via.placeholder.com/400x300/8C1515/FFFFFF?text=Stanford" },
                    new Place { Id = "ca_3", Name = "Palo Alto Cafe", Description = "Café de la Silicon Valley", Categories = new List<PlaceCategory> { new() { Name = "Café" } }, Location = new PlaceLocation { Address = "University Ave", City = "Palo Alto", Country = "USA" }, PhotoUrl = "https://via.placeholder.com/400x300/D2691E/FFFFFF?text=Cafe" }
                },
                ["default"] = new List<Place>
                {
                    new Place { Id = "default_1", Name = "Lieu touristique local", Description = "Point d'intérêt de la région", Categories = new List<PlaceCategory> { new() { Name = "Tourisme" } }, Location = new PlaceLocation { Address = "Adresse locale", City = "Ville locale", Country = "Pays" }, PhotoUrl = "https://via.placeholder.com/400x300/808080/FFFFFF?text=Local" }
                }
            };
        }
    }
}