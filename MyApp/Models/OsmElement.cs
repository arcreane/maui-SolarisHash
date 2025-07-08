using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyApp.Models
{
    // Réponse complète de l'API Overpass
    public class OsmResponse
    {
        [JsonProperty("version")]
        public double Version { get; set; }

        [JsonProperty("generator")]
        public string Generator { get; set; } = string.Empty; // ✅ Corrigé

        [JsonProperty("elements")]
        public List<OsmElement> Elements { get; set; } = new List<OsmElement>();
    }

    // Élément OSM (Node, Way, ou Relation)
    public class OsmElement
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty; // ✅ Corrigé

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("lat")]
        public double? Latitude { get; set; }

        [JsonProperty("lon")]
        public double? Longitude { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        [JsonProperty("nodes")]
        public List<long> Nodes { get; set; } = new List<long>();

        [JsonProperty("geometry")]
        public List<OsmGeometry> Geometry { get; set; } = new List<OsmGeometry>();

        // Propriétés calculées pour faciliter l'usage
        public string Name => GetTag("name") ?? GetTag("name:fr") ?? GetTag("name:en") ?? "Lieu sans nom";
        
        public string? Tourism => GetTag("tourism"); // ✅ Nullable
        public string? Amenity => GetTag("amenity"); // ✅ Nullable
        public string? Shop => GetTag("shop"); // ✅ Nullable
        public string? Historic => GetTag("historic"); // ✅ Nullable
        public string? Leisure => GetTag("leisure"); // ✅ Nullable
        
        public string Description => GetTag("description") ?? GetTag("description:fr") ?? string.Empty;
        public string Website => GetTag("website") ?? GetTag("contact:website") ?? string.Empty;
        public string Phone => GetTag("phone") ?? GetTag("contact:phone") ?? string.Empty;
        public string OpeningHours => GetTag("opening_hours") ?? string.Empty;
        
        public string Address => BuildAddress();
        public string MainCategory => DetermineMainCategory();

        private string? GetTag(string key) // ✅ Nullable return
        {
            return Tags.ContainsKey(key) ? Tags[key] : null;
        }

        private string BuildAddress()
        {
            var addressParts = new List<string>();
            
            var houseNumber = GetTag("addr:housenumber");
            var street = GetTag("addr:street");
            var city = GetTag("addr:city");
            var postcode = GetTag("addr:postcode");

            if (!string.IsNullOrEmpty(houseNumber) && !string.IsNullOrEmpty(street))
            {
                addressParts.Add($"{houseNumber} {street}");
            }
            else if (!string.IsNullOrEmpty(street))
            {
                addressParts.Add(street);
            }

            if (!string.IsNullOrEmpty(postcode) && !string.IsNullOrEmpty(city))
            {
                addressParts.Add($"{postcode} {city}");
            }
            else if (!string.IsNullOrEmpty(city))
            {
                addressParts.Add(city);
            }

            return string.Join(", ", addressParts);
        }

        private string DetermineMainCategory()
        {
            // Priorité des catégories
            if (!string.IsNullOrEmpty(Tourism))
            {
                return TranslateTourismCategory(Tourism);
            }
            
            if (!string.IsNullOrEmpty(Amenity))
            {
                return TranslateAmenityCategory(Amenity);
            }
            
            if (!string.IsNullOrEmpty(Historic))
            {
                return "Monument historique";
            }
            
            if (!string.IsNullOrEmpty(Leisure))
            {
                return TranslateLeisureCategory(Leisure);
            }
            
            if (!string.IsNullOrEmpty(Shop))
            {
                return "Commerce";
            }

            return "Lieu d'intérêt";
        }

        private string TranslateTourismCategory(string tourism)
        {
            return tourism switch
            {
                "attraction" => "Attraction",
                "museum" => "Musée",
                "monument" => "Monument",
                "artwork" => "Œuvre d'art",
                "viewpoint" => "Point de vue",
                "gallery" => "Galerie",
                "theme_park" => "Parc d'attractions",
                "zoo" => "Zoo",
                "aquarium" => "Aquarium",
                "hotel" => "Hôtel",
                "hostel" => "Auberge",
                "guest_house" => "Maison d'hôtes",
                "information" => "Information touristique",
                _ => "Tourisme"
            };
        }

        private string TranslateAmenityCategory(string amenity)
        {
            return amenity switch
            {
                "restaurant" => "Restaurant",
                "cafe" => "Café",
                "bar" => "Bar",
                "pub" => "Pub",
                "fast_food" => "Restauration rapide",
                "food_court" => "Court de restauration",
                "ice_cream" => "Glacier",
                "pharmacy" => "Pharmacie",
                "hospital" => "Hôpital",
                "bank" => "Banque",
                "atm" => "Distributeur",
                "fuel" => "Station-service",
                "parking" => "Parking",
                "toilets" => "Toilettes",
                "library" => "Bibliothèque",
                "theatre" => "Théâtre",
                "cinema" => "Cinéma",
                "place_of_worship" => "Lieu de culte",
                _ => "Service"
            };
        }

        private string TranslateLeisureCategory(string leisure)
        {
            return leisure switch
            {
                "park" => "Parc",
                "garden" => "Jardin",
                "playground" => "Aire de jeux",
                "sports_centre" => "Centre sportif",
                "swimming_pool" => "Piscine",
                "golf_course" => "Golf",
                "marina" => "Marina",
                "beach_resort" => "Station balnéaire",
                _ => "Loisirs"
            };
        }
    }

    // Géométrie pour les Ways et Relations
    public class OsmGeometry
    {
        [JsonProperty("lat")]
        public double Latitude { get; set; }

        [JsonProperty("lon")]
        public double Longitude { get; set; }
    }
}