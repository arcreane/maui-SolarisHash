using MyApp.Models;

namespace MyApp.Services
{
    /// <summary>
    /// Service de démonstration avec des données factices
    /// À utiliser en attendant la configuration de l'API Foursquare
    /// </summary>
    public class MockPlaceService : IPlaceService
    {
        public async Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string query = null, int radius = 1000, int limit = 20)
        {
            // Simulation d'un délai réseau
            await Task.Delay(1500);

            var mockPlaces = new List<Place>
            {
                new Place
                {
                    Id = "1",
                    Name = "Tour Eiffel",
                    Description = "Monument emblématique de Paris, construite en 1889 pour l'Exposition universelle.",
                    Distance = 250,
                    Location = new PlaceLocation
                    {
                        Latitude = latitude + 0.001,
                        Longitude = longitude + 0.001,
                        Address = "Champ de Mars, 5 Avenue Anatole France",
                        FormattedAddress = "Champ de Mars, 5 Avenue Anatole France, 75007 Paris",
                        City = "Paris",
                        Country = "France"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "monument", Name = "Monuments" }
                    },
                    PhotoUrl = "https://images.unsplash.com/photo-1543349689-9a4d426bee8e?w=400"
                },
                new Place
                {
                    Id = "2",
                    Name = "Café de Flore",
                    Description = "Café parisien historique, lieu de rencontre des intellectuels.",
                    Distance = 420,
                    Location = new PlaceLocation
                    {
                        Latitude = latitude - 0.002,
                        Longitude = longitude + 0.001,
                        Address = "172 Boulevard Saint-Germain",
                        FormattedAddress = "172 Boulevard Saint-Germain, 75006 Paris",
                        City = "Paris",
                        Country = "France"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "restaurant", Name = "Restaurants" }
                    },
                    PhotoUrl = "https://images.unsplash.com/photo-1554118811-1e0d58224f24?w=400"
                },
                new Place
                {
                    Id = "3",
                    Name = "Musée du Louvre",
                    Description = "Le plus grand musée d'art du monde et un monument historique à Paris.",
                    Distance = 680,
                    Location = new PlaceLocation
                    {
                        Latitude = latitude + 0.003,
                        Longitude = longitude - 0.002,
                        Address = "Rue de Rivoli",
                        FormattedAddress = "Rue de Rivoli, 75001 Paris",
                        City = "Paris",
                        Country = "France"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "museum", Name = "Musées" }
                    },
                    PhotoUrl = "https://images.unsplash.com/photo-1566139975810-0373b16ffa79?w=400"
                },
                new Place
                {
                    Id = "4",
                    Name = "Jardin du Luxembourg",
                    Description = "L'un des plus beaux parcs de Paris, parfait pour une promenade.",
                    Distance = 890,
                    Location = new PlaceLocation
                    {
                        Latitude = latitude - 0.001,
                        Longitude = longitude - 0.003,
                        Address = "Rue de Médicis",
                        FormattedAddress = "Rue de Médicis, 75006 Paris",
                        City = "Paris",
                        Country = "France"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "park", Name = "Parcs" }
                    },
                    PhotoUrl = "https://images.unsplash.com/photo-1524721696987-b9527df9e512?w=400"
                },
                new Place
                {
                    Id = "5",
                    Name = "Hôtel des Invalides",
                    Description = "Complexe de bâtiments contenant des musées et monuments relatifs à l'histoire militaire de la France.",
                    Distance = 1200,
                    Location = new PlaceLocation
                    {
                        Latitude = latitude + 0.002,
                        Longitude = longitude + 0.003,
                        Address = "129 Rue de Grenelle",
                        FormattedAddress = "129 Rue de Grenelle, 75007 Paris",
                        City = "Paris",
                        Country = "France"
                    },
                    Categories = new List<PlaceCategory>
                    {
                        new PlaceCategory { Id = "monument", Name = "Monuments" }
                    },
                    PhotoUrl = "https://images.unsplash.com/photo-1471623432079-b009d30b6729?w=400"
                }
            };

            // Filtrer par requête si fournie
            if (!string.IsNullOrWhiteSpace(query))
            {
                mockPlaces = mockPlaces.Where(p => 
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    p.Categories?.Any(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) == true
                ).ToList();
            }

            return mockPlaces.Take(limit).ToList();
        }

        public async Task<Place> GetPlaceDetailsAsync(string placeId)
        {
            // Simulation d'un délai réseau
            await Task.Delay(500);

            var places = await GetNearbyPlacesAsync(48.8566, 2.3522); // Coordonnées de Paris
            return places.FirstOrDefault(p => p.Id == placeId);
        }
    }
}