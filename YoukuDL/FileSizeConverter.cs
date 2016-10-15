using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace YoukuDL
{
    [ValueConversion(typeof(int), typeof(string))]
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String retVal = String.Empty;

            if (value != null && (value is int? || value is int))
            {
                int val = (int)value;

                if(val/1024<1)
                {
                    retVal = val % 1024 + " Byte";
                }
                else if(val/(1024*1024)<1)
                {
                    retVal = (val / (double)1024).ToString("f2") + " KB";
                }
                else if(val/(1024 * 1024 * 1024)<1)
                {
                    retVal = (val / (double)(1024*1024)).ToString("f2") + " MB";
                }
                else
                {
                    retVal = (val / (double)(1024 * 1024*1024)).ToString("f2") + " GB";
                }
            }

            return retVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
