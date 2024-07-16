using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Playout.Database.Models;
using Soundmouse.Messaging.Model;
using System.Linq;
using static MusicManager.Playout.Metadata.InterestedParty.Roles;
using InterestedParty = MusicManager.Playout.Metadata.InterestedParty;
using static MusicManager.Playout.Metadata.MusicOrigin;

namespace MusicManager.Playout.Build.MetadataMapping
{
    public class TrackXmlMapper : ITrackXMLMapper
    {
        private readonly ILogger _logger;

        private const string IswcIdentifier = "iswc";
        private const string IsrcIdentifier = "isrc";
        private const string EanIdentifier = "ean";
        private const string CatNoIdentifier = "catalogue_number";
        private const string NAName = "N/A";

        private readonly List<PMAP_SUBFUNC> _originatorOrderedTypes = new List<PMAP_SUBFUNC>()
        {
            PMAP_SUBFUNC.PERSON_FUNCCOMPOSERComposer,
            PMAP_SUBFUNC.PERSON_FUNCLYRICISTLyricist,
            PMAP_SUBFUNC.PERSON_FUNCARRANGERArranger,
            PMAP_SUBFUNC.PERSON_FUNCTRANSLATORTranslator
        };

        private readonly List<PMAP_SUBFUNC> _performerOrderedTypes = new List<PMAP_SUBFUNC>()
        {
            PMAP_SUBFUNC.PERSON_FUNCPERFORMERMainArtist,
            PMAP_SUBFUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer,
            PMAP_SUBFUNC.PERSON_FUNCFEATURED_ARTISTFeaturedArtist,
            PMAP_SUBFUNC.PERSON_FUNCVS_ARTISTVersusArtist,
            PMAP_SUBFUNC.PERSON_FUNCREMIX_ARTISTRemixArtist,
            PMAP_SUBFUNC.PERSON_FUNCCONDUCTORConductor,
            PMAP_SUBFUNC.PERSON_FUNCORCHESTRAOrchestra,
            PMAP_SUBFUNC.PERSON_FUNCENSEMBLEEnsemble,
            PMAP_SUBFUNC.PERSON_FUNCCHOIRChoir
        };

        public TrackXmlMapper(ILogger<TrackXmlMapper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public EXPORT MapTrackToFile(Track trackToMap, string xmlType, List<string> prsPublishers)
        {
            xmlType = xmlType.ToLower();

            var export = new EXPORT { TAKE = new TAKE() };

            GENERICType generic = new GENERICType();
            generic.GENE_UDT_ID = "00000003";
            generic.UDT_NAME = "Music-DIGA";
            generic.GENE_TYPESpecified = true;
            generic.GENE_TYPE = GENERICTypeGENE_TYPE.TYPEMUSICMusic;

            if (TrackXmlType.Classical == xmlType)
            {
                generic.GENE_UDT_ID = "00000014";
                generic.UDT_NAME = "Classic-DIGA";
                generic.GENE_TYPE = GENERICTypeGENE_TYPE.TYPECLASSICClassic;
            }

            generic.GENE_MREPORT_STATUS = GENERICTypeGENE_MREPORT_STATUS.MREPORT_STATUSDETAILS_MISSINGDetailsMissing;
            generic.GENE_FUNCTION = "FUNCTION$AUDIO#Audio";
            generic.GENE_MUSIC_CODE = ToGenericTypeGeneMusicCode(ReturnBaseCode(trackToMap.TrackData.MusicOrigin));
            generic.GENE_EXT_SYS = "EXT_SYS$SNDMSE#Soundmouse Library";
            generic.GENE_TITLE = trackToMap.TrackData.Title;
            generic.GENE_TITLE_INFO = trackToMap.TrackData.Notes;
            generic.GENE_TRACK = trackToMap.TrackData.Position;
            generic.GENE_SIDE = trackToMap.TrackData.DiscNumber;
            generic.GENE_CATALOG_DB0 = trackToMap.Id.ToString();


            if (trackToMap.TrackData.Product != null)
            {
                generic.GENE_ALBUM = trackToMap.TrackData.Product.Name;
                //if (trackToMap.TrackData.Product.ReleaseDate.HasValue)
                //{
                //    generic.GENE_PUB_TIME = trackToMap.TrackData.Product.ReleaseDate.Value;
                //    generic.GENE_PUB_TIMESpecified = true;
                //}
            }

            if (trackToMap.TrackData.Identifiers != null)
            {
                if (trackToMap.TrackData.Identifiers.ContainsKey(IswcIdentifier))
                {
                    generic.GENE_WORK_ID = trackToMap.TrackData.Identifiers[IswcIdentifier]?.Replace("-", string.Empty);
                }
                else
                {
                    _logger.LogDebug("ISWC identifier not found for track {TrackId}", trackToMap.Id);
                }

                if (trackToMap.TrackData.Identifiers.ContainsKey(IsrcIdentifier))
                {
                    generic.GENE_ISRC = trackToMap.TrackData.Identifiers[IsrcIdentifier]?.Replace("-", string.Empty);
                }
                else
                {
                    _logger.LogDebug("ISRC identifier not found for track {TrackId}", trackToMap.Id);
                }
            }
            else
            {
                _logger.LogDebug("No identifiers found for track {TrackId}", trackToMap.Id);
            }

            if (trackToMap.TrackData.Product?.Identifiers != null)
            {

                if (trackToMap.TrackData.Product.Identifiers.ContainsKey(CatNoIdentifier))
                {
                    generic.GENE_ORDER_NO = trackToMap.TrackData.Product.Identifiers[CatNoIdentifier];
                }
                else
                {
                    _logger.LogDebug("Catalog No. identifier not found for {TrackId}", trackToMap.Id);
                }

                if (trackToMap.TrackData.Product.Identifiers.ContainsKey(EanIdentifier))
                {
                    generic.GENE_EAN = trackToMap.TrackData.Product.Identifiers[EanIdentifier];
                }
                else
                {
                    _logger.LogDebug("EAN identifier not found for track {TrackId}", trackToMap.Id);
                }
            }
            else
            {
                _logger.LogDebug("No identifiers found for album with track {TrackId}", trackToMap.Id);
            }

            Dictionary<PMAP_FUNC, List<PERSONType>> persons = new Dictionary<PMAP_FUNC, List<PERSONType>>();

            HashSet<string> contributorsExtendedPersonNames = new HashSet<string>();

            if (trackToMap.TrackData.ContributorsExtended != null)
            {
                MapContributors(trackToMap, xmlType, persons, contributorsExtendedPersonNames);
            }
            else
            {
                _logger.LogDebug("No Contributors found for track {TrackId}", trackToMap.Id);
            }

            if (trackToMap.TrackData.InterestedParties != null)
            {
                MapInterestedParties(trackToMap, xmlType, prsPublishers, generic, contributorsExtendedPersonNames, persons);
            }
            else
            {
                _logger.LogInformation("No Interested Parties found for track {TrackId}", trackToMap.Id);
            }

            //order persons
            if (persons.Any())
            {
                SetPersonOrderNumber(persons, PMAP_FUNC.PERSON_FUNCORIGINATOROriginator, _originatorOrderedTypes);
                SetPersonOrderNumber(persons,
                    TrackXmlType.Classical == xmlType
                        ? PMAP_FUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer
                        : PMAP_FUNC.PERSON_FUNCPERFORMERMainArtist, _performerOrderedTypes);
                export.TAKE.TAKE_PERSONS = persons.SelectMany(p => p.Value).ToArray();
            }

            export.TAKE.GENERIC = generic;

            return export;
        }        

        private string GetUnderlyingValue(string value)
        {
            char FullCodeSeparator = ':';

            if (string.IsNullOrWhiteSpace(value))
                return value;

            var separatorIndex = value.IndexOf(FullCodeSeparator);
            return separatorIndex >= 0 ? value.Substring(0, separatorIndex) : value;
        }

        private void MapInterestedParties(Track trackToMap, string xmlType, List<string> prsPublishers,
            GENERICType generic, HashSet<string> contributorsExtendedPersonNames,
            Dictionary<PMAP_FUNC, List<PERSONType>> persons)
        {
            var recordLabel =
                trackToMap.TrackData.InterestedParties.FirstOrDefault(l => l.Role == Metadata.InterestedParty.Roles.Label);
            if (recordLabel != null)
            {
                generic.GENE_LABEL = recordLabel.FullName;
                if (recordLabel.IpIdentifiers?.ContainsKey("labelCode") == true)
                    generic.GENE_LABEL_CODES = recordLabel.IpIdentifiers["labelCode"];
                else
                    _logger.LogDebug("Record label code not found for label {Name}", recordLabel.FullName);
            }

            string MapRoles(string[] roles)
            {
                var rolesFound = trackToMap.TrackData.InterestedParties.Where(ip =>
                    roles.Contains(GetUnderlyingValue(ip.Role))).ToArray();

                return rolesFound.Any() ? string.Join('/', rolesFound.Select(p => p.FullName).Distinct(StringComparer.OrdinalIgnoreCase)) : string.Empty;
            }

            generic.GENE_PUBLISHER = prsPublishers?.Any() ?? false
                ? string.Join(',', prsPublishers)
                : MapRoles(new[] { Publisher, SubPublisher, OriginalPublisher });
            generic.GENE_COMPOSERS = MapRoles(new[] { Composer, ComposerLyricist });
            generic.GENE_PERFORMERS = MapRoles(new[] { Performer });

            Soundmouse.Messaging.Model.InterestedParty[] filteredPartiesToExport = FilterInterestedParties(trackToMap.TrackData.InterestedParties.ToArray(), contributorsExtendedPersonNames);

            void GetPersonType(Soundmouse.Messaging.Model.InterestedParty interestedParty, string baseRole)
            {
                var person = new PERSONType
                {
                    PERSON_NAME = interestedParty.FullName,
                    PMAP_FUNC = IpRoleToPmapFunc(baseRole, xmlType),
                    PMAP_SUBFUNC = IpRoleToSubPmapFunc(baseRole, xmlType)
                };

                if (interestedParty.IpIdentifiers?.ContainsKey("ipi") == true)
                {
                    person.PERSON_EXT_REF00 = interestedParty.IpIdentifiers["ipi"];
                }
                else
                {
                    _logger.LogDebug("IPI not found for interested party {Name}",
                        interestedParty.FullName);
                }

                if (!persons.ContainsKey(person.PMAP_FUNC))
                {
                    persons.Add(person.PMAP_FUNC, new List<PERSONType>());
                }

                persons[person.PMAP_FUNC].Add(person);
            }

            if (!filteredPartiesToExport.Any())
            {
                return;
            }

            foreach (Soundmouse.Messaging.Model.InterestedParty interestedParty in filteredPartiesToExport)
            {
                string baseRole = GetUnderlyingValue(interestedParty.Role);

                if (baseRole == ComposerLyricist)
                {
                    GetPersonType(interestedParty, Composer);
                    GetPersonType(interestedParty, Lyricist);
                }
                else
                {
                    GetPersonType(interestedParty, baseRole);
                }
            }
        }

        private static Soundmouse.Messaging.Model.InterestedParty[] FilterInterestedParties(Soundmouse.Messaging.Model.InterestedParty[] partiesToExport, HashSet<string> contributorsExtendedPersonNames)
        {
            Soundmouse.Messaging.Model.InterestedParty[] filteredExcludedParties = FilterExcludedRoles(partiesToExport);

            Soundmouse.Messaging.Model.InterestedParty[] filteredPerformers =
                FilterPerformers(filteredExcludedParties, contributorsExtendedPersonNames);

            Soundmouse.Messaging.Model.InterestedParty[] filteredNaParties = FilterNAParties(filteredPerformers);

            Soundmouse.Messaging.Model.InterestedParty[] filteredParties = FilterDuplicateComposerLyricistParties(filteredNaParties);

            return filteredParties;
        }

        private static Soundmouse.Messaging.Model.InterestedParty[] FilterExcludedRoles(Soundmouse.Messaging.Model.InterestedParty[] partiesToExport)
        {
            string[] excludedRoles =
            {
                Publisher, SubPublisher, OriginalPublisher, InterestedParty.Roles.Label, Administrator
            };

            return partiesToExport.Where(ip => !excludedRoles.Contains(ip.Role)).ToArray();
        }

        private static Soundmouse.Messaging.Model.InterestedParty[] FilterPerformers(Soundmouse.Messaging.Model.InterestedParty[] partiesToExport, HashSet<string> contributorsExtendedPersonNames)
        {
            return partiesToExport.Where(ip => !(ip.Role == Performer && contributorsExtendedPersonNames.Contains(ip.FullName, StringComparer.OrdinalIgnoreCase))).ToArray();
        }

        private static Soundmouse.Messaging.Model.InterestedParty[] FilterNAParties(Soundmouse.Messaging.Model.InterestedParty[] partiesToExport)
        {
            return partiesToExport.Where(p =>
                !string.Equals(p.FullName, NAName, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        private static Soundmouse.Messaging.Model.InterestedParty[] FilterDuplicateComposerLyricistParties(Soundmouse.Messaging.Model.InterestedParty[] partiesToExport)
        {
            string[] composerLyricistNames = partiesToExport.Where(p => p.Role == ComposerLyricist).Select(p => p.FullName).ToArray();

            Soundmouse.Messaging.Model.InterestedParty[] composerLyricistFilteredPartiesToExport = partiesToExport.Where(p =>
                !((p.Role == Lyricist || p.Role == Composer) && composerLyricistNames.Contains(p.FullName, StringComparer.OrdinalIgnoreCase))).ToArray();

            return composerLyricistFilteredPartiesToExport;
        }

        private void MapContributors(Track trackToMap, string xmlType, Dictionary<PMAP_FUNC, List<PERSONType>> persons, HashSet<string> personNames)
        {
            var contributorsToExport = trackToMap.TrackData.ContributorsExtended.Where(ce =>
                ContributorRole.All.Contains(ce.Role) &&
                !string.Equals(ce.Name, NAName, StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var contributor in contributorsToExport)
            {
                var person = new PERSONType { PERSON_NAME = contributor.Name, PERSON_EXT_REF00 = contributor.Isni };

                if (TrackXmlType.Classical == xmlType)
                {
                    person.PMAP_FUNC = PMAP_FUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer;
                    person.PMAP_SUBFUNC = _contributorRoleToClassicSubPmapFunc[contributor.Role];
                }
                else
                {
                    person.PMAP_FUNC = PMAP_FUNC.PERSON_FUNCPERFORMERMainArtist;
                    person.PMAP_SUBFUNC = _contributorRoleToContemporarySubPmapFunc[contributor.Role];
                }

                if (!persons.ContainsKey(person.PMAP_FUNC))
                {
                    persons.Add(person.PMAP_FUNC, new List<PERSONType>());
                }

                persons[person.PMAP_FUNC].Add(person);
                personNames.Add(contributor.Name.ToLower());
            }
        }

        private void SetPersonOrderNumber(Dictionary<PMAP_FUNC, List<PERSONType>> persons, PMAP_FUNC pmapFunc, List<PMAP_SUBFUNC> orderedTypes)
        {
            if (!persons.ContainsKey(pmapFunc))
                return;

            int orderNumber = 1;
            foreach (PERSONType person in persons[pmapFunc]
                .OrderBy(e => e.PMAP_SUBFUNC, new PmapComparer(orderedTypes)))
            {
                person.PMAP_ORD_NO = orderNumber.ToString();
                orderNumber++;
            }
        }

        private GENERICTypeGENE_MUSIC_CODE ToGenericTypeGeneMusicCode(string origin) => origin switch
        {
            OriginKeys.MusicOriginCommercial => GENERICTypeGENE_MUSIC_CODE.MUSIC_CODECCCommercial,
            OriginKeys.MusicOriginProductionLibrary => GENERICTypeGENE_MUSIC_CODE.MUSIC_CODEMMLibraryMusic,
            OriginKeys.MusicOriginProductionLibraryNonAffiliated => GENERICTypeGENE_MUSIC_CODE.MUSIC_CODEMMLibraryMusic,
            OriginKeys.MusicOriginProductionLibraryNonMechanical => GENERICTypeGENE_MUSIC_CODE.MUSIC_CODEMMLibraryMusic,
            OriginKeys.MusicOriginCommissioned => GENERICTypeGENE_MUSIC_CODE.MUSIC_CODEXXSpecComposed,
            OriginKeys.MusicOriginInStudioPerformance => GENERICTypeGENE_MUSIC_CODE.MUSIC_CODERRLiveRecSession,
            OriginKeys.MusicOriginVideo => GENERICTypeGENE_MUSIC_CODE.MUSIC_CODECCCommercial,
            OriginKeys.MusicOriginSoundEffect => GENERICTypeGENE_MUSIC_CODE.MUSIC_CODEZZNottobereported,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), $"Origin {origin} could not be mapped")
        };

        private PMAP_SUBFUNC IpRoleToSubPmapFunc(string role, string xmlType) => role switch
        {
            Lyricist => PMAP_SUBFUNC.PERSON_FUNCLYRICISTLyricist,
            SubLyricist => PMAP_SUBFUNC.PERSON_FUNCLYRICISTLyricist,
            Adaptor => PMAP_SUBFUNC.PERSON_FUNCLYRICISTLyricist,
            SubAdaptor => PMAP_SUBFUNC.PERSON_FUNCLYRICISTLyricist,
            Arranger => PMAP_SUBFUNC.PERSON_FUNCARRANGERArranger,
            SubArranger => PMAP_SUBFUNC.PERSON_FUNCARRANGERArranger,
            Composer => PMAP_SUBFUNC.PERSON_FUNCCOMPOSERComposer,
            Translator => PMAP_SUBFUNC.PERSON_FUNCTRANSLATORTranslator,
            Performer => xmlType == TrackXmlType.Classical
                ? PMAP_SUBFUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer
                : PMAP_SUBFUNC.PERSON_FUNCPERFORMERMainArtist,
            _ => xmlType == TrackXmlType.Classical
                ? PMAP_SUBFUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer
                : PMAP_SUBFUNC.PERSON_FUNCPERFORMERMainArtist
        };

        private PMAP_FUNC IpRoleToPmapFunc(string role, string xmlType) => role switch
        {
            Lyricist => PMAP_FUNC.PERSON_FUNCORIGINATOROriginator,
            SubLyricist => PMAP_FUNC.PERSON_FUNCORIGINATOROriginator,
            Adaptor => PMAP_FUNC.PERSON_FUNCORIGINATOROriginator,
            SubAdaptor => PMAP_FUNC.PERSON_FUNCORIGINATOROriginator,
            Arranger => PMAP_FUNC.PERSON_FUNCORIGINATOROriginator,
            SubArranger => PMAP_FUNC.PERSON_FUNCORIGINATOROriginator,
            Composer => PMAP_FUNC.PERSON_FUNCORIGINATOROriginator,
            Translator => PMAP_FUNC.PERSON_FUNCORIGINATOROriginator,
            Performer => xmlType == TrackXmlType.Classical
                ? PMAP_FUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer
                : PMAP_FUNC.PERSON_FUNCPERFORMERMainArtist,
            _ => xmlType == TrackXmlType.Classical
                ? PMAP_FUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer
                : PMAP_FUNC.PERSON_FUNCPERFORMERMainArtist
        };


        private readonly Dictionary<string, PMAP_SUBFUNC> _contributorRoleToContemporarySubPmapFunc =
            new Dictionary<string, PMAP_SUBFUNC>
            {
                {ContributorRole.FeaturedArtist, PMAP_SUBFUNC.PERSON_FUNCFEATURED_ARTISTFeaturedArtist},
                {ContributorRole.Featuring, PMAP_SUBFUNC.PERSON_FUNCFEATURED_ARTISTFeaturedArtist},
                {ContributorRole.RemixArtist, PMAP_SUBFUNC.PERSON_FUNCREMIX_ARTISTRemixArtist},
                {ContributorRole.Remixer, PMAP_SUBFUNC.PERSON_FUNCREMIX_ARTISTRemixArtist},
                {ContributorRole.VersusArtist, PMAP_SUBFUNC.PERSON_FUNCVS_ARTISTVersusArtist},
                {ContributorRole.Orchestra, PMAP_SUBFUNC.PERSON_FUNCPERFORMERMainArtist},
                {ContributorRole.Conductor, PMAP_SUBFUNC.PERSON_FUNCPERFORMERMainArtist},
                {ContributorRole.Choir, PMAP_SUBFUNC.PERSON_FUNCPERFORMERMainArtist},
                {ContributorRole.Ensemble, PMAP_SUBFUNC.PERSON_FUNCPERFORMERMainArtist},
            };

        private readonly Dictionary<string, PMAP_SUBFUNC> _contributorRoleToClassicSubPmapFunc =
            new Dictionary<string, PMAP_SUBFUNC>
            {
                {ContributorRole.FeaturedArtist, PMAP_SUBFUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer},
                {ContributorRole.Featuring, PMAP_SUBFUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer},
                {ContributorRole.RemixArtist, PMAP_SUBFUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer},
                {ContributorRole.Remixer, PMAP_SUBFUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer},
                {ContributorRole.VersusArtist, PMAP_SUBFUNC.PERSON_FUNCCLASSIC_PERFORMERClassicPerformer},
                {ContributorRole.Orchestra, PMAP_SUBFUNC.PERSON_FUNCORCHESTRAOrchestra},
                {ContributorRole.Conductor, PMAP_SUBFUNC.PERSON_FUNCCONDUCTORConductor},
                {ContributorRole.Choir, PMAP_SUBFUNC.PERSON_FUNCCHOIRChoir},
                {ContributorRole.Ensemble, PMAP_SUBFUNC.PERSON_FUNCENSEMBLEEnsemble},
            };


    }
}
