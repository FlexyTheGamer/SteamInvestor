using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;

namespace SteamInventoryAIR.Converters
{
    public class IntEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return targetType == typeof(Color) ? Color.FromArgb("#2A2E35") : false;

            // Check if we need to extract the comparison value from a comma-separated string
            string paramString = parameter.ToString();
            string compareValueStr = paramString;
            string selectedColorStr = "#CC2424";    // Default red
            string unselectedColorStr = "#2A2E35";  // Default dark gray

            // If parameter contains comma, it includes color values
            if (paramString.Contains(","))
            {
                var parts = paramString.Split(',');
                if (parts.Length >= 1) compareValueStr = parts[0];
                if (parts.Length >= 2) selectedColorStr = parts[1];
                if (parts.Length >= 3) unselectedColorStr = parts[2];
            }

            // Compare the values
            bool isEqual = false;
            if (int.TryParse(value.ToString(), out int intValue) &&
                int.TryParse(compareValueStr, out int compareValue))
            {
                isEqual = intValue == compareValue;
            }

            // Return the appropriate type
            if (targetType == typeof(Color))
            {
                return isEqual ? Color.FromArgb(selectedColorStr) : Color.FromArgb(unselectedColorStr);
            }

            return isEqual;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
