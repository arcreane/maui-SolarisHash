using System;
using Newtonsoft.Json;

namespace MyApp.Models
{
    public class Place
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public PlaceLocation Location { get; set; }

        [JsonProperty("categories")]
        public List<PlaceCategory> Categories { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("distance")]
        public int Distance { get; set; }

        public string FormattedDistance => $"{Distance}m";

        public string MainCategory => Categories?.FirstOrDefault()?.Name ?? "Lieu";

        public string Address => Location?.FormattedAddress ?? "Adresse inconnue";

        public string PhotoUrl { get; set; }
    }

    public class PlaceLocation
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("lat")]
        public double Latitude { get; set; }

        [JsonProperty("lng")]
        public double Longitude { get; set; }

        [JsonProperty("formattedAddress")]
        public string FormattedAddress { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }
    }

    public class PlaceCategory
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public CategoryIcon Icon { get; set; }
    }

    public class CategoryIcon
    {
        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("suffix")]
        public string Suffix { get; set; }

        public string GetIconUrl(int size = 64)
        {
            return $"{Prefix}{size}{Suffix}";
        }
    }
}
