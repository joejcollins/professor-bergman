using System;
using Windows.UI.Xaml.Data;

namespace Dinmore.Uwp
{
    public class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;

            DateTime dateTime = DateTime.Parse(value.ToString());
            return dateTime.ToString("HH:mm:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
