using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dinmore.Uwp.Helpers
{
    public static class Settings
    {
        public static bool GetBool(string key)
        {
            return (ApplicationData.Current.LocalSettings.Values[key] != null) ?
                (bool)ApplicationData.Current.LocalSettings.Values[key] :
                false;
        }

        public static string GetString(string key)
        {
            return (ApplicationData.Current.LocalSettings.Values[key] != null) ?
                ApplicationData.Current.LocalSettings.Values[key].ToString() :
                null;
        }

        public static void Set(string key, string value)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }

        public static void Set(string key, bool value)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }
    }
}
