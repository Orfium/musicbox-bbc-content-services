using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Elasticsearch.Util
{
    public static class StringExtensions
    {
        private static readonly string[] _reserved = new[]
            {"+", "-", "&", "|", "!", "(", ")", "{", "}", "[", "]", "^", "\"", "~", "*", "?", ":", "\\", "/"};

        public static string EscapeQuery(this string s)
        {
            if (s == null)
                return null;

            foreach (var res in _reserved)
                s = s.Replace(res, " ");

            return s;
        }

        public static string GetValueFromObject(this object obj, string key)
        {
            try
            {
                var convertedObj = JsonConvert.DeserializeObject<dynamic>(obj.ToString());
                var value = Convert.ToString(convertedObj[key]);
                return value;
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public static string ReplaceSpecialCodes(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str.Replace("&amp;", "&").Replace("&quot;","\"").Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static List<string> SplitByComma(this string str)
        {
            List<string> _list = new List<string>();
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            else {
                if (str.Contains(","))
                {
                    _list = str.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                else {
                    _list.Add(str);
                }                
            }
            return _list;
        }

        public static string ListToCommaSeaparatedString(this List<string> list)
        {
            if (list == null || list?.Count() == 0)
                return "";

            string str = "";
            foreach (var item in list)
            {
                str += item + ",";
            }
            return str.Trim();
        }

        public static string ReverseSpecialCodes(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str.Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static double? StringToDouble(this string val)
        {
            double douVal = 0;
            if (double.TryParse(val, out douVal))
                return douVal;

            return null;
        }

    }
}
