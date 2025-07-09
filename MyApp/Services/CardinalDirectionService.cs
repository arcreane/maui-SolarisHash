using MyApp.Models;

namespace MyApp.Services
{
    public interface ICardinalDirectionService
    {
        List<CardinalGroup> GroupPlacesByCardinalDirection(List<Place> places, double userLat, double userLon);
        string GetCardinalDirection(double bearing);
    }

    public class CardinalGroup
    {
        public string Direction { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public List<Place> Places { get; set; } = new List<Place>();
        public int Count => Places.Count;
    }

    public class CardinalDirectionService : ICardinalDirectionService
    {
        public List<CardinalGroup> GroupPlacesByCardinalDirection(List<Place> places, double userLat, double userLon)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🧭 Groupement cardinal: {places.Count} lieux à {userLat:F4}, {userLon:F4}");

                var groups = new Dictionary<string, CardinalGroup>
                {
                    ["Nord"] = new CardinalGroup { Direction = "Nord", Emoji = "⬆️" },
                    ["Nord-Est"] = new CardinalGroup { Direction = "Nord-Est", Emoji = "↗️" },
                    ["Est"] = new CardinalGroup { Direction = "Est", Emoji = "➡️" },
                    ["Sud-Est"] = new CardinalGroup { Direction = "Sud-Est", Emoji = "↘️" },
                    ["Sud"] = new CardinalGroup { Direction = "Sud", Emoji = "⬇️" },
                    ["Sud-Ouest"] = new CardinalGroup { Direction = "Sud-Ouest", Emoji = "↙️" },
                    ["Ouest"] = new CardinalGroup { Direction = "Ouest", Emoji = "⬅️" },
                    ["Nord-Ouest"] = new CardinalGroup { Direction = "Nord-Ouest", Emoji = "↖️" }
                };

                foreach (var place in places)
                {
                    if (place.Location != null)
                    {
                        var bearing = CalculateBearing(userLat, userLon, place.Location.Latitude, place.Location.Longitude);
                        var direction = GetCardinalDirection(bearing);
                        
                        if (groups.ContainsKey(direction))
                        {
                            groups[direction].Places.Add(place);
                            System.Diagnostics.Debug.WriteLine($"🧭 {place.Name} → {direction} ({bearing:F0}°)");
                        }
                    }
                }

                var result = groups.Values
                    .Where(g => g.Count > 0)
                    .OrderBy(g => GetDirectionOrder(g.Direction))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🧭 Résultat: {result.Count} directions avec lieux");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur groupement cardinal: {ex.Message}");
                return new List<CardinalGroup>();
            }
        }

        public string GetCardinalDirection(double bearing)
        {
            var normalizedBearing = NormalizeBearing(bearing);
            
            return normalizedBearing switch
            {
                >= 337.5 or < 22.5 => "Nord",
                >= 22.5 and < 67.5 => "Nord-Est",
                >= 67.5 and < 112.5 => "Est",
                >= 112.5 and < 157.5 => "Sud-Est",
                >= 157.5 and < 202.5 => "Sud",
                >= 202.5 and < 247.5 => "Sud-Ouest",
                >= 247.5 and < 292.5 => "Ouest",
                >= 292.5 and < 337.5 => "Nord-Ouest",
                _ => "Nord"
            };
        }

        private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRadians(lon2 - lon1);
            var lat1Rad = ToRadians(lat1);
            var lat2Rad = ToRadians(lat2);

            var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
            var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

            var bearing = Math.Atan2(y, x);
            return NormalizeBearing(ToDegrees(bearing));
        }

        private double NormalizeBearing(double bearing)
        {
            while (bearing < 0) bearing += 360;
            while (bearing >= 360) bearing -= 360;
            return bearing;
        }

        private int GetDirectionOrder(string direction)
        {
            return direction switch
            {
                "Nord" => 0,
                "Nord-Est" => 1,
                "Est" => 2,
                "Sud-Est" => 3,
                "Sud" => 4,
                "Sud-Ouest" => 5,
                "Ouest" => 6,
                "Nord-Ouest" => 7,
                _ => 8
            };
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
        private static double ToDegrees(double radians) => radians * 180 / Math.PI;
    }
}