using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MusicManager.Playout.Metadata
{
    public sealed class CountryListConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum IpNonSupersetCountryListStandard
        {
            [EnumMember(Value = "iso")]
            ISO
        }

        public IpNonSupersetCountryListStandard Standard { get; set; }

        /// <summary>
        /// Determines whether "Worldwide" is an available option.
        /// </summary>
        public bool HasWorldwide { get; set; }
    }
}
