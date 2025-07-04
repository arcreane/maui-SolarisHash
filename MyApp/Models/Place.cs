using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MyApp.Models
{
    public class Place
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("location")]
        public PlaceLocation Location { get; set; } = new PlaceLocation();

        [JsonProperty("categories")]
        public List<PlaceCategory> Categories { get; set; } = new List<PlaceCategory>();

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("distance")]
        public int Distance { get; set; }

        // Nouvelles propriétés pour OSM
        public string Tourism { get; set; } = string.Empty;
        public string Amenity { get; set; } = string.Empty;
        public string Historic { get; set; } = string.Empty;
        public string Leisure { get; set; } = string.Empty;
        public string Shop { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string OpeningHours { get; set; } = string.Empty;
        public Dictionary<string, string> OsmTags { get; set; } = new Dictionary<string, string>();

        // Propriétés calculées
        public string FormattedDistance => Distance < 1000 
            ? $"{Distance}m" 
            : $"{Distance / 1000.0:F1}km";

        public string MainCategory => Categories?.FirstOrDefault()?.Name ?? DetermineMainCategory();

        public string Address => Location?.FormattedAddress ?? Location?.Address ?? "Adresse inconnue";

        public string PhotoUrl { get; set; } = string.Empty;

        // Méthode pour créer un Place à partir d'un OsmElement
        public static Place FromOsmElement(OsmElement osmElement, double userLat, double userLon)
        {
            var place = new Place
            {
                Id = osmElement.Id.ToString(),
                Name = osmElement.Name,
                Description = osmElement.Description,
                Tourism = osmElement.Tourism ?? string.Empty,
                Amenity = osmElement.Amenity ?? string.Empty,
                Historic = osmElement.Historic ?? string.Empty,
                Leisure = osmElement.Leisure ?? string.Empty,
                Shop = osmElement.Shop ?? string.Empty,
                Website = osmElement.Website ?? string.Empty,
                Phone = osmElement.Phone ?? string.Empty,
                OpeningHours = osmElement.OpeningHours ?? string.Empty,
                OsmTags = osmElement.Tags ?? new Dictionary<string, string>()
            };

            // Localisation
            if (osmElement.Latitude.HasValue && osmElement.Longitude.HasValue)
            {
                place.Location = new PlaceLocation
                {
                    Latitude = osmElement.Latitude.Value,
                    Longitude = osmElement.Longitude.Value,
                    Address = osmElement.Address,
                    FormattedAddress = osmElement.Address,
                    City = osmElement.Tags.ContainsKey("addr:city") ? osmElement.Tags["addr:city"] : "",
                    Country = osmElement.Tags.ContainsKey("addr:country") ? osmElement.Tags["addr:country"] : "France"
                };

                place.Distance = CalculateDistance(userLat, userLon, osmElement.Latitude.Value, osmElement.Longitude.Value);
            }
            else if (osmElement.Geometry?.Any() == true)
            {
                var firstPoint = osmElement.Geometry.First();
                place.Location = new PlaceLocation
                {
                    Latitude = firstPoint.Latitude,
                    Longitude = firstPoint.Longitude,
                    Address = osmElement.Address,
                    FormattedAddress = osmElement.Address,
                    City = osmElement.Tags.ContainsKey("addr:city") ? osmElement.Tags["addr:city"] : "",
                    Country = osmElement.Tags.ContainsKey("addr:country") ? osmElement.Tags["addr:country"] : "France"
                };

                place.Distance = CalculateDistance(userLat, userLon, firstPoint.Latitude, firstPoint.Longitude);
            }

            // Catégories
            place.Categories.Add(new PlaceCategory
            {
                Id = osmElement.MainCategory.ToLower().Replace(" ", "_"),
                Name = osmElement.MainCategory
            });

            // Pas d'image URL - laissé vide pour éviter les erreurs réseau
            place.PhotoUrl = string.Empty;

            return place;
        }

        private string DetermineMainCategory()
        {
            if (!string.IsNullOrEmpty(Tourism)) return "Tourisme";
            if (!string.IsNullOrEmpty(Amenity)) return "Service";
            if (!string.IsNullOrEmpty(Historic)) return "Monument";
            if (!string.IsNullOrEmpty(Leisure)) return "Loisirs";
            if (!string.IsNullOrEmpty(Shop)) return "Commerce";
            return "Lieu d'intérêt";
        }

        private static int CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371; // Rayon de la Terre en km

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = earthRadius * c;

            return (int)(distance * 1000); // Retourner en mètres
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }

    public class PlaceLocation
    {
        [JsonProperty("address")]
        public string Address { get; set; } = string.Empty;

        [JsonProperty("lat")]
        public double Latitude { get; set; }

        [JsonProperty("lng")]
        public double Longitude { get; set; }

        [JsonProperty("formattedAddress")]
        public string FormattedAddress { get; set; } = string.Empty;

        [JsonProperty("country")]
        public string Country { get; set; } = string.Empty;

        [JsonProperty("city")]
        public string City { get; set; } = string.Empty;
    }

    public class PlaceCategory
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("icon")]
        public CategoryIcon? Icon { get; set; }
    }

    public class CategoryIcon
    {
        [JsonProperty("prefix")]
        public string Prefix { get; set; } = string.Empty;

        [JsonProperty("suffix")]
        public string Suffix { get; set; } = string.Empty;

        public string GetIconUrl(int size = 64)
        {
            return $"{Prefix}{size}{Suffix}";
        }
    }
}