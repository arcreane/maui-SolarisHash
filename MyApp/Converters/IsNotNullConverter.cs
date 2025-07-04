using System.Globalization;
using System.Collections;

namespace MyApp.Converters
{
    public class IsNotNullConverter : IValueConverter
    {
        public static readonly IsNotNullConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return false;
            
            if (value is string str)
                return !string.IsNullOrWhiteSpace(str);
            
            if (value is int count)
                return count > 0;

            if (value is ICollection collection)
                return collection.Count > 0;
                
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertedBoolConverter : IValueConverter
    {
        public static readonly InvertedBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            if (value is int intValue)
                return intValue == 0;

            if (value is ICollection collection)
                return collection.Count == 0;
            
            return value == null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            
            return true;
        }
    }

    public class BoolToTextConverter : IValueConverter
    {
        public static readonly BoolToTextConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "🛑 Désactiver orientation" : "🧭 Activer orientation";
            }
            return "🧭 Activer orientation";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public static readonly BoolToColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? Colors.Red : Colors.Green;
            }
            return Colors.Green;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}