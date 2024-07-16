using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Playout.Build.MetadataMapping
{
    public class ContributorRole
    {
        public const string FeaturedArtist = "featured_artist";
        public const string Featuring = "featuring";
        public const string RemixArtist = "remix_artist";
        public const string Remixer = "remixer";
        public const string VersusArtist = "versus_artist";
        public const string Orchestra = "orchestra";
        public const string Conductor = "conductor";
        public const string Choir = "choir";
        public const string Ensemble = "ensemble";

        public static string[] All = new[]
        {
            FeaturedArtist, Featuring, RemixArtist, Remixer, VersusArtist, Orchestra, Conductor, Choir, Ensemble
        };
    }
}
