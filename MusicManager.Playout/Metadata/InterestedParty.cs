using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using static MusicManager.Playout.Metadata.CountryListConfig;

namespace MusicManager.Playout.Metadata
{
    public class InterestedParty 
    {
        public static readonly Regex IsniRegex = new Regex("^[0-9]{15}[0-9xX]{1}$");

        /// <summary>
        /// A class to group all interested party role constants together.
        /// </summary>
        public static class Roles
        {
            /// <summary>
            /// The interested party role constant for an adaptor.
            /// </summary>
            public const string Adaptor = "adaptor";

            /// <summary>
            /// The interested party role constant for an arranger.
            /// </summary>
            public const string Arranger = "arranger";

            /// <summary>
            /// The interested party role constant for an administrator
            /// </summary>
            public const string Administrator = "administrator";

            /// <summary>
            /// The interested party role constant for a lyricist.
            /// </summary>
            public const string Lyricist = "lyricist";

            /// <summary>
            /// The interested party role constant for a composer.
            /// </summary>
            public const string Composer = "composer";

            /// <summary>
            /// The interested party role constant for a composer / lyricist.
            /// </summary>
            public const string ComposerLyricist = "composer_lyricist";

            /// <summary>
            /// The interested party role constant for a performer.
            /// </summary>
            public const string Performer = "performer";

            /// <summary>
            /// The interested party role constant for a publisher.
            /// </summary>
            public const string Publisher = "publisher";

            /// <summary>
            /// The interested party role constant for an original publisher.
            /// </summary>
            public const string OriginalPublisher = "original_publisher";

            /// <summary>
            /// The interested party role constant for a label.
            /// </summary>
            public const string Label = "record_label";

            /// <summary>
            /// The interested party role constant for a translator.
            /// </summary>
            public const string Translator = "translator";

            /// <summary>
            /// The interested party role constant for a sub-lyricist.
            /// </summary>
            public const string SubLyricist = "sub_lyricist";

            /// <summary>
            /// The interested party role constant for a sub-adaptor.
            /// </summary>
            public const string SubAdaptor = "sub_adaptor";

            /// <summary>
            /// The interested party role constant for a sub-arranger.
            /// </summary>
            public const string SubArranger = "sub_arranger";

            /// <summary>
            /// The interested party role constant for a sub-publisher.
            /// </summary>
            public const string SubPublisher = "sub_publisher";

            /// <summary>
            /// The interested party role constant for a custom role.
            /// </summary>
            public const string Custom = "custom";

            /// <summary>
            /// An array enumerating all interested party types.
            /// </summary>
            public static readonly string[] All =
            {
                Adaptor, Arranger, Administrator, Lyricist,
                Composer, ComposerLyricist, Label,
                Performer, Publisher, OriginalPublisher,
                Translator, SubAdaptor, SubArranger, SubLyricist,
                SubPublisher
            };

            public static readonly string[] MusicManagerAll =
            {
                Composer, ComposerLyricist, Lyricist, Publisher,
                Performer, Label, Adaptor, Arranger, Translator
            };

            public static readonly string[] MusicManagerAdmin =
            {
                Administrator, OriginalPublisher, SubAdaptor,
                SubArranger, SubLyricist, SubPublisher
            };

            public static string GetGlobalizationKey(string role)
            {
                switch (role)
                {
                    case Lyricist:
                        return "IpRole_Lyricist";
                    case Adaptor:
                        return "IpRole_Adaptor";
                    case Administrator:
                        return "IpRole_Administrator";
                    case Arranger:
                        return "IpRole_Arranger";
                    case Composer:
                        return "IpRole_Composer";
                    case ComposerLyricist:
                        return "IpRole_Composer_Lyricist";
                    case Publisher:
                        return "IpRole_Publisher";
                    case OriginalPublisher:
                        return "IpRole_Original_Publisher";
                    case Performer:
                        return "IpRole_Performer";
                    case Label:
                        return "IpRole_Label";
                    case SubLyricist:
                        return "IpRole_Sub_Lyricist";
                    case SubAdaptor:
                        return "IpRole_Sub_Adaptor";
                    case SubArranger:
                        return "IpRole_Sub_Arranger";
                    case SubPublisher:
                        return "IpRole_Sub_Publisher";
                    case Translator:
                        return "IpRole_Translator";
                    default:
                        return "IpRole_Unknown";
                }
            }            
        }       
    }

    public sealed class InterestedPartyCountry
    {
        public IpNonSupersetCountryListStandard Standard { get; set; }
        public string Value { get; set; }
    }
}
