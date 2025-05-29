using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TESTDIP.ViewModel
{
    public class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Этот метод не используется в нашем случае, но требуется интерфейсом
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0.0;

            string stringValue = value.ToString();

            // Заменяем запятые на точки для корректного парсинга
            stringValue = stringValue.Replace(",", ".")
                                    .Replace("mg", "", StringComparison.OrdinalIgnoreCase)
                                    .Replace("мг", "", StringComparison.OrdinalIgnoreCase)
                                    .Trim();

            if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                // Округляем до 3 знаков после запятой
                return Math.Round(result, 3);
            }

            return 0.0;
        }
    }
}