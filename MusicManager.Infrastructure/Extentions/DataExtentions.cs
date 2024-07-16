using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Infrastructure.Extentions
{
    public static class DataExtentions
    {
        public static string SearchInputParamFormat(this string val)
        {
            if (!string.IsNullOrEmpty(val))
            {
                return val.Trim().Replace("'", "''").ToLower();
            }
            else
            {
                return val;
            }
        }

        public static string GetDateOnly(this DateTime? val)
        {
            if (val != null)
            {
                return val?.ToString("yyyy-MM-dd");
            }
            else
            {
                return "";
            }
        }
    }
}
