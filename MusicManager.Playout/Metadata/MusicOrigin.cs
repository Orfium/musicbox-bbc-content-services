using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicManager.Playout.Metadata
{
    public sealed class MusicOrigin
    {
        public static class OriginKeys
        {
            public const string MusicOriginCommercial = "commercial";
            public const string MusicOriginCommissioned = "commissioned";
            public const string MusicOriginProductionLibrary = "library";
            public const string MusicOriginInStudioPerformance = "live";
            public const string MusicOriginProductionLibraryNonMechanical = "library_non_mechanical";
            public const string MusicOriginProductionLibraryNonAffiliated = "library_non_affiliated";
            public const string MusicOriginSoundEffect = "sound_effect";
            public const string MusicOriginVideo = "video";
            public const string MusicOriginNone = "";

            public static bool IsProductionLibrary(string origin)
            {
                switch (origin)
                {
                    case MusicOriginProductionLibrary:
                    case MusicOriginProductionLibraryNonAffiliated:
                    case MusicOriginProductionLibraryNonMechanical:
                        return true;
                    default:
                        return false;
                }
            }

            public static string[] AllKeys = new[]
            {
                MusicOriginCommercial,
                MusicOriginCommissioned,
                MusicOriginProductionLibrary,
                MusicOriginInStudioPerformance,
                MusicOriginProductionLibraryNonMechanical,
                MusicOriginProductionLibraryNonAffiliated,
                MusicOriginSoundEffect,
                MusicOriginVideo,
                MusicOriginNone
            };

        }

        private static class GlobalizationKeys
        {
            internal const string Commercial = "MusicOrigin_Commercial";
            internal const string Commissioned = "MusicOrigin_Commissioned";
            internal const string ProductionLibrary = "MusicOrigin_Library";
            internal const string InStudioPerformance = "MusicOrigin_Live";
            internal const string LibraryNonMechanical = "MusicOrigin_Library_NonMechanical";
            internal const string LibraryNonAffiliated = "MusicOrigin_Library_NonAffiliated";
            internal const string SoundEffect = "MusicOrigin_SoundEffect";
            internal const string Video = "MusicOrigin_Video";
            internal const string None = "MusicOrigin_None";
        }

        private static class AbbreviationKeys
        {
            internal const string Commercial = "MusicOrigin_Commercial_Code";
            internal const string Commissioned = "MusicOrigin_Commissioned_Code";
            internal const string ProductionLibrary = "MusicOrigin_Library_Code";
            internal const string InStudioPerformance = "MusicOrigin_Live_Code";
            internal const string LibraryNonMechanical = "MusicOrigin_Library_NonMechanical_Code";
            internal const string LibraryNonAffiliated = "MusicOrigin_Library_NonAffiliated_Code";
            internal const string SoundEffect = "MusicOrigin_SoundEffect_Code";
            internal const string Video = "MusicOrigin_Video_Code";
            internal const string None = "MusicOrigin_None_Code";
        }

        public static string ReturnGlobalizationKey(string origin)
        {
            switch (origin)
            {
                case OriginKeys.MusicOriginCommercial: return GlobalizationKeys.Commercial;
                case OriginKeys.MusicOriginCommissioned: return GlobalizationKeys.Commissioned;
                case OriginKeys.MusicOriginProductionLibrary: return GlobalizationKeys.ProductionLibrary;
                case OriginKeys.MusicOriginInStudioPerformance: return GlobalizationKeys.InStudioPerformance;
                case OriginKeys.MusicOriginProductionLibraryNonMechanical: return GlobalizationKeys.LibraryNonMechanical;
                case OriginKeys.MusicOriginProductionLibraryNonAffiliated: return GlobalizationKeys.LibraryNonAffiliated;
                case OriginKeys.MusicOriginSoundEffect: return GlobalizationKeys.SoundEffect;
                case OriginKeys.MusicOriginVideo: return GlobalizationKeys.Video;
                default: return origin;
            }
        }

        public static class SharedOrigins
        {
            internal static readonly MusicOrigin Commercial =
                new MusicOrigin(OriginKeys.MusicOriginCommercial, GlobalizationKeys.Commercial, AbbreviationKeys.Commercial);

            internal static readonly MusicOrigin Commissioned =
                new MusicOrigin(OriginKeys.MusicOriginCommissioned, GlobalizationKeys.Commissioned, AbbreviationKeys.Commissioned);

            internal static readonly MusicOrigin ProductionLibrary =
                new MusicOrigin(OriginKeys.MusicOriginProductionLibrary, GlobalizationKeys.ProductionLibrary, AbbreviationKeys.ProductionLibrary);

            internal static readonly MusicOrigin InStudioPerformance =
                new MusicOrigin(OriginKeys.MusicOriginInStudioPerformance, GlobalizationKeys.InStudioPerformance, AbbreviationKeys.InStudioPerformance);

            internal static readonly MusicOrigin LibraryNonMechanical =
                new MusicOrigin(OriginKeys.MusicOriginProductionLibraryNonMechanical, GlobalizationKeys.LibraryNonMechanical, AbbreviationKeys.LibraryNonMechanical);

            internal static readonly MusicOrigin LibraryNonAffiliated =
                new MusicOrigin(OriginKeys.MusicOriginProductionLibraryNonAffiliated, GlobalizationKeys.LibraryNonAffiliated, AbbreviationKeys.LibraryNonAffiliated);

            internal static readonly MusicOrigin SoundEffect =
                new MusicOrigin(OriginKeys.MusicOriginSoundEffect, GlobalizationKeys.SoundEffect, AbbreviationKeys.SoundEffect);

            internal static readonly MusicOrigin Video =
                new MusicOrigin(OriginKeys.MusicOriginVideo, GlobalizationKeys.Video, AbbreviationKeys.Video);

            internal static readonly MusicOrigin None =
                new MusicOrigin(OriginKeys.MusicOriginNone, GlobalizationKeys.None, AbbreviationKeys.None);

        }

        public static readonly MusicOrigin[] AllOrigins = new[]
        {
            SharedOrigins.Commercial,
            SharedOrigins.Commissioned,
            SharedOrigins.ProductionLibrary,
            SharedOrigins.InStudioPerformance,
            SharedOrigins.LibraryNonMechanical,
            SharedOrigins.LibraryNonAffiliated,
            SharedOrigins.SoundEffect,
            SharedOrigins.Video
        };

        public static readonly MusicOrigin[] AllOriginsExtended = new[]
                                                                  {

                                                                      SharedOrigins.None
                                                                  }.Concat(AllOrigins).ToArray();

        public static readonly Dictionary<string, MusicOrigin> AllOriginsCodeLookup =
            AllOrigins.ToDictionary(o => o.Code, o => o);

        public static readonly Dictionary<string, MusicOrigin> AllOriginsExtendedCodeLookup =
            AllOriginsExtended.ToDictionary(o => o.Code, o => o);

        public static readonly MusicOrigin[] Origins = new[]
        {
            SharedOrigins.Commercial,
            SharedOrigins.Commissioned,
            SharedOrigins.ProductionLibrary,
            SharedOrigins.InStudioPerformance,
            SharedOrigins.LibraryNonMechanical,
            SharedOrigins.LibraryNonAffiliated,
            SharedOrigins.SoundEffect,
            SharedOrigins.Video
        };

        public static readonly MusicOrigin[] OriginsMusicManager = new[]
        {
            SharedOrigins.Commercial,
            SharedOrigins.Commissioned,
            SharedOrigins.ProductionLibrary,
            SharedOrigins.LibraryNonMechanical,
            SharedOrigins.LibraryNonAffiliated,
            SharedOrigins.None
        };

        public string Code { get; private set; }
        public string GlobalizationKey { get; private set; }
        public string AbbreviationCode { get; private set; }

        private MusicOrigin(string code, string globalizationKey, string abbreviationCode)
        {
            Code = code;
            GlobalizationKey = globalizationKey;
            AbbreviationCode = abbreviationCode;
        }

        public static bool IsValidOriginCode(string code)
        {
            return OriginsMusicManager.Any(o => o.Code == code);
        }

        //public static string GetWorkspaceDefaultMusicOrigin(Workspace workspace, string defaultOrigin)
        //{
        //    var musicOrigin = defaultOrigin;

        //    if (workspace?.Settings.InterfaceConfiguration?.MusicOriginDefault?.DefaultMusicOrigin != null
        //        && OriginsMusicManager.Any(o => o.Code == workspace.Settings.InterfaceConfiguration.MusicOriginDefault.DefaultMusicOrigin))
        //    {
        //        musicOrigin = workspace.Settings.InterfaceConfiguration.MusicOriginDefault.DefaultMusicOrigin;
        //    }

        //    return musicOrigin;
        //}

        //public static string TryReverseMap(string smValue, Workspace workspace)
        //{
        //    static bool HasCustomMapping(string smValue, Workspace workspace)
        //    {
        //        return !(workspace?.Settings?.InterfaceConfiguration?.MusicOrigins?.ReverseMappings == null ||
        //        !workspace.Settings.InterfaceConfiguration.MusicOrigins.ReverseMappings.ContainsKey(smValue));
        //    }

        //    if (HasCustomMapping(smValue, workspace))
        //    {
        //        return workspace.Settings.InterfaceConfiguration.MusicOrigins.ReverseMappings[smValue];
        //    }

        //    // check if the selected workspace is part of a group and if yes, get that workspace
        //    IoC.Postgresql.Instance.Run(t => { workspace = WorkspaceGroup.SelectManagementWorkspace(workspace.Id, t) ?? workspace; });

        //    return HasCustomMapping(smValue, workspace)
        //               ? workspace.Settings.InterfaceConfiguration.MusicOrigins.ReverseMappings[smValue]
        //               : smValue;
        //}

        //public static string[] GetSmValuesFromReverseMapping(string[] customValues, Workspace workspace)
        //{
        //    if (customValues == null
        //        || workspace?.Settings?.InterfaceConfiguration?.MusicOrigins?.ReverseMappings == null)
        //    {
        //        return new string[0];
        //    }

        //    var result = workspace.Settings.InterfaceConfiguration.MusicOrigins.ReverseMappings
        //                          .Where(t => customValues.Contains(t.Value))
        //                          .Select(x => x.Key);

        //    return result.ToArray();
        //}

        public static string ReturnBaseCode(string origin)
        {
            return TrySplitOrigin(origin, out var baseCode, out _) ? baseCode : string.Empty;
        }

        public static bool TrySplitOrigin(string origin,
                                         out string baseCode,
                                         out Guid? variantId)
        {
            baseCode = string.Empty;
            variantId = null;

            if (string.IsNullOrWhiteSpace(origin))
            {
                return false;
            }

            try
            {
                var split = origin.Split(':');
                if (split.Length > 0)
                {
                    baseCode = split[0];
                    if (split.Length > 1)
                        variantId = Guid.Parse(split[1]);

                    if (!AllOriginsExtendedCodeLookup.ContainsKey(baseCode))
                        baseCode = OriginKeys.MusicOriginNone;
                }

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, $"Failed to split theme '{origin}'");

                return false;
            }
        }
    }
}

