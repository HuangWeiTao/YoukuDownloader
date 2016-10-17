using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace YoukuDL.UIConverter
{
    [ValueConversion(typeof(double), typeof(string))]
    public class PercentConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String retVal = String.Empty;

            if (value != null && value is double)
            {
                return ((double)value).ToString("P1");
            }

            return retVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
