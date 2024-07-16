using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.PrsSearch.DataMatching
{
    public enum MatchAttribute
    {
        CatNo,
        Composer,
        Isrc,
        Iswc,
        Label,
        Performer,
        ProductTitle,
        Title,
        Tunecode,
        FileNameExtSysRef
    }

    public static class MatchAttributeExtensions
    {
        public static string ConvertToString(this MatchAttribute attr)
        {
            switch (attr)
            {
                case MatchAttribute.CatNo:
                    return "cat_no";
                case MatchAttribute.Composer:
                    return "composer";
                case MatchAttribute.Isrc:
                    return "isrc";
                case MatchAttribute.Iswc:
                    return "iswc";
                case MatchAttribute.Label:
                    return "label";
                case MatchAttribute.Performer:
                    return "performer";
                case MatchAttribute.ProductTitle:
                    return "product_title";
                case MatchAttribute.Title:
                    return "title";
                case MatchAttribute.Tunecode:
                    return "tunecode";
                case MatchAttribute.FileNameExtSysRef:
                    return "file_name_extsysref";
                default:
                    throw new ArgumentOutOfRangeException(nameof(attr), attr, "attribute not recognised");
            }
        }

        public static MatchAttribute Parse(string value)
        {
            switch (value)
            {
                case "cat_no":
                    return MatchAttribute.CatNo;
                case "composer":
                    return MatchAttribute.Composer;
                case "isrc":
                    return MatchAttribute.Isrc;
                case "iswc":
                    return MatchAttribute.Iswc;
                case "label":
                    return MatchAttribute.Label;
                case "performer":
                    return MatchAttribute.Performer;
                case "product_title":
                    return MatchAttribute.ProductTitle;
                case "title":
                    return MatchAttribute.Title;
                case "tunecode":
                    return MatchAttribute.Tunecode;
                case "file_name_extsysref":
                    return MatchAttribute.FileNameExtSysRef;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, "attribute string not recognised");
            }
        }
    }
}
