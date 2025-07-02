using System.Globalization;

namespace MyApp.Converters
{
    public class CategoryToEmojiConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                return category.ToLower() switch
                {
                    var c when c.Contains("restaurant") => "🍽️",
                    var c when c.Contains("café") => "☕",
                    var c when c.Contains("cafe") => "☕",
                    var c when c.Contains("musée") => "🏛️",
                    var c when c.Contains("museum") => "🏛️",
                    var c when c.Contains("monument") => "🏛️",
                    var c when c.Contains("historique") => "🏛️",
                    var c when c.Contains("parc") => "🌳",
                    var c when c.Contains("park") => "🌳",
                    var c when c.Contains("jardin") => "🌺",
                    var c when c.Contains("église") => "⛪",
                    var c when c.Contains("hôtel") => "🏨",
                    var c when c.Contains("hotel") => "🏨",
                    var c when c.Contains("commerce") => "🛒",
                    var c when c.Contains("shop") => "🛒",
                    var c when c.Contains("bank") => "🏦",
                    var c when c.Contains("banque") => "🏦",
                    var c when c.Contains("pharmacy") => "💊",
                    var c when c.Contains("pharmacie") => "💊",
                    var c when c.Contains("hospital") => "🏥",
                    var c when c.Contains("hôpital") => "🏥",
                    var c when c.Contains("université") => "🎓",
                    var c when c.Contains("university") => "🎓",
                    var c when c.Contains("entreprise") => "🏢",
                    var c when c.Contains("tourisme") => "🗺️",
                    var c when c.Contains("tourism") => "🗺️",
                    var c when c.Contains("port") => "⚓",
                    var c when c.Contains("quartier") => "🏘️",
                    _ => "📍"
                };
            }
            
            return "📍";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}