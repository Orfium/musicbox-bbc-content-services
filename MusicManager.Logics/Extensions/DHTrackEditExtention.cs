using Elasticsearch.Util;
using MusicManager.Core.ViewModules;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicManager.Logics.Extensions
{
    public static class DHTrackEditExtention
    {
        public static EditTrackMetadata CreateEditTrack(this DHTrack dHTrack, string trackTitle)
        {
            EditTrackMetadata editTrackMetadata = new EditTrackMetadata();

            if (dHTrack == null)
            {
                editTrackMetadata.track_title = trackTitle;
            }
            else
            {
                if (dHTrack.interestedParties == null)
                    dHTrack.interestedParties = new List<DHTInterestedParty>();

                //List<string> composerlyricist = GetNameListByRole("composer_lyricist", dHTrack.interestedParties);

                editTrackMetadata.id = dHTrack.uniqueId;
                editTrackMetadata.albumId = dHTrack.albumId;
                editTrackMetadata.file_name = dHTrack.filename;
                editTrackMetadata.track_title = dHTrack.title.ReplaceSpecialCodes();
                editTrackMetadata.isrc = dHTrack.identifiers.CheckByValueType("isrc");
                editTrackMetadata.iswc = dHTrack.identifiers.CheckByValueType("iswc");
                editTrackMetadata.prs = dHTrack.identifiers.CheckByValueType("prs");
                editTrackMetadata.duration = dHTrack.duration;
                editTrackMetadata.performers = GetNameListByRole("performer", dHTrack.interestedParties);
                editTrackMetadata.publishers = GetNameListByRole("publisher", dHTrack.interestedParties);
                editTrackMetadata.translators = GetNameListByRole("translator", dHTrack.interestedParties);
                editTrackMetadata.arrangers = GetNameListByRole("arranger", dHTrack.interestedParties);
                editTrackMetadata.rec_label = GetNameListByRole("record_label", dHTrack.interestedParties)?.Count > 0 ? GetNameListByRole("record_label", dHTrack.interestedParties)[0] : "";
                editTrackMetadata.lyricist = GetNameListByRole("lyricist", dHTrack.interestedParties);
                editTrackMetadata.composers = GetNameListByRole("composer", dHTrack.interestedParties);
                editTrackMetadata.composer_lyricists = GetNameListByRole("composer_lyricist", dHTrack.interestedParties);
                editTrackMetadata.sub_lyricist = GetNameListByRole("sub_lyricist", dHTrack.interestedParties);
                editTrackMetadata.sub_adaptor = GetNameListByRole("sub_adaptor", dHTrack.interestedParties);
                editTrackMetadata.sub_arranger = GetNameListByRole("sub_arranger", dHTrack.interestedParties);
                editTrackMetadata.adaptor = GetNameListByRole("adaptor", dHTrack.interestedParties);
                editTrackMetadata.original_publisher = GetNameListByRole("original_publisher", dHTrack.interestedParties);
                editTrackMetadata.sub_publisher = GetNameListByRole("sub_publisher", dHTrack.interestedParties);

                

                //if (composerlyricist?.Count() > 0)
                //{
                //    if (editTrackMetadata.composers == null) editTrackMetadata.composers = new List<string>();
                //    if (editTrackMetadata.lyricist == null) editTrackMetadata.lyricist = new List<string>();

                //    foreach (var item in composerlyricist)
                //    {
                //        if (editTrackMetadata.composers?.Count(a => a == item) == 0)
                //            editTrackMetadata.composers.Add(item);

                //        if (editTrackMetadata.lyricist?.Count(a => a == item) == 0)
                //            editTrackMetadata.lyricist.Add(item);
                //    }
                //}

                editTrackMetadata.genres = dHTrack.genres;
                editTrackMetadata.moods = dHTrack.moods;
                editTrackMetadata.keywords = dHTrack.tags;
                editTrackMetadata.instruments = dHTrack.instruments;
                editTrackMetadata.styles = dHTrack.styles;
                editTrackMetadata.musicorigin = dHTrack.musicOrigin;
                editTrackMetadata.position = dHTrack.position;
                editTrackMetadata.numPosition = dHTrack.position.StringToDouble();
                editTrackMetadata.disc_number = dHTrack.discNumber;
                editTrackMetadata.source_ref = dHTrack.miscellaneous?.sourceRef;
                editTrackMetadata.bpm = dHTrack.bpm.ReplaceSpecialCodes();
                editTrackMetadata.tempo = dHTrack.tempo.ReplaceSpecialCodes();
                editTrackMetadata.pLine = dHTrack.pLine.ReplaceSpecialCodes();
                editTrackMetadata.track_notes = dHTrack.notes.ReplaceSpecialCodes();
                editTrackMetadata.pre_release = dHTrack.pre_release;
                editTrackMetadata.alternate_title = dHTrack.alternativeTitle.ReplaceSpecialCodes();
                editTrackMetadata.version_title = dHTrack.versionTitle.ReplaceSpecialCodes();

                editTrackMetadata.contributor = new List<Contributor>();

                if (dHTrack.contributorsExtended?.Count() > 0)
                {
                    editTrackMetadata.contributor = dHTrack.contributorsExtended;
                }

                //editTrackMetadata.contributor = BindIpToContributor(enIPRole.adaptor.ToString(), dHTrack.interestedParties, editTrackMetadata.contributor);
                //editTrackMetadata.contributor = BindIpToContributor(enIPRole.administrator.ToString(), dHTrack.interestedParties, editTrackMetadata.contributor);
                //editTrackMetadata.contributor = BindIpToContributor(enIPRole.original_publisher.ToString(), dHTrack.interestedParties, editTrackMetadata.contributor);
                //editTrackMetadata.contributor = BindIpToContributor(enIPRole.sub_lyricist.ToString(), dHTrack.interestedParties, editTrackMetadata.contributor);
                //editTrackMetadata.contributor = BindIpToContributor(enIPRole.sub_adaptor.ToString(), dHTrack.interestedParties, editTrackMetadata.contributor);
                //editTrackMetadata.contributor = BindIpToContributor(enIPRole.sub_arranger.ToString(), dHTrack.interestedParties, editTrackMetadata.contributor);
                //editTrackMetadata.contributor = BindIpToContributor(enIPRole.sub_publisher.ToString(), dHTrack.interestedParties, editTrackMetadata.contributor);


                if (dHTrack.descriptiveExtended?.Count() > 0) {
                    foreach (var item in dHTrack.descriptiveExtended)
                    {
                        if (item.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString())
                        {
                            editTrackMetadata.org_admin_notes = item.Value.ToString().ReplaceSpecialCodes();
                        }
                        else if (item.Type == enDescriptiveExtendedType.bbc_track_id.ToString())
                        {
                            editTrackMetadata.bbc_track_id = item.Value.ToString().ReplaceSpecialCodes();
                        }
                    }
                }

                if (dHTrack.tagsExtended?.Count() > 0)
                {
                    editTrackMetadata.orgTags = dHTrack.tagsExtended;
                    foreach (var item in dHTrack.tagsExtended)
                    {
                        if (item.Type == enAdminTypes.BBC_ADMIN_TAG.ToString())
                        {
                            if (editTrackMetadata.org_adminTags == null)
                                editTrackMetadata.org_adminTags = new List<string>();

                            editTrackMetadata.org_adminTags.Add(item.Value);
                        }                        
                    }
                }

                if (dHTrack.validityPeriod != null) {
                    editTrackMetadata.valid_to_date = dHTrack.validityPeriod.endDate != null ? dHTrack.validityPeriod.endDate?.ToString("yyyy-MM-dd") : "";
                    editTrackMetadata.valid_from_date = dHTrack.validityPeriod.startDate != null ? dHTrack.validityPeriod.startDate?.ToString("yyyy-MM-dd") : "";
                }
            }
            return editTrackMetadata;
        }

        public static double? StringToDouble(this string val)
        {
            double douVal = 0;
            if (double.TryParse(val, out douVal))
                return douVal;

            return null;
        }

        public static DHTrack CreateDHTrackFromEditTrackMetadata(DHTrack dHTrack, EditTrackMetadata editTrackMetadata, string uniqueId)
        {

            if (dHTrack == null)
            {
                dHTrack = new DHTrack()
                {
                    identifiers = new List<DHValueType>(),
                    genres = new List<string>(),
                    instruments = new List<string>(),
                    styles = new List<string>(),
                    tags = new List<string>(),
                    miscellaneous = new DHTMiscellaneous()
                    {
                        sourceRef = "ML_TRACK_ADD"
                    },
                    interestedParties = new List<DHTInterestedParty>(),
                    territories = new DHTTerritories()
                    {
                        include = new List<string>()
                    },
                    uniqueId = uniqueId ?? Guid.NewGuid().ToString(),
                    duration = editTrackMetadata.duration,
                    contributorsExtended = new List<Contributor>()
                };
                dHTrack.territories.include.Add("UK");
            }
            else
            {
                dHTrack.contributorsExtended = new List<Contributor>();
            }

            dHTrack.validityPeriod = new DHValidityPeriod();
            dHTrack.tagsExtended = new List<Tag>();          

            if (editTrackMetadata.version_id != null)
            {
                if (dHTrack.miscellaneous == null)
                    dHTrack.miscellaneous = new DHTMiscellaneous();
                dHTrack.miscellaneous.sourceVersionId = editTrackMetadata.version_id.ToString();
            }



            if (uniqueId != null)
                dHTrack.uniqueId = uniqueId;

            if (dHTrack.interestedParties == null)
                dHTrack.interestedParties = new List<DHTInterestedParty>();

            if (!string.IsNullOrWhiteSpace(editTrackMetadata.version_title))
                dHTrack.versionTitle = editTrackMetadata.version_title.Trim().ReplaceSpecialCodes();

            dHTrack.albumId = editTrackMetadata.albumId == null ? dHTrack.albumId : editTrackMetadata.albumId;
            dHTrack.title = editTrackMetadata.track_title.ReplaceSpecialCodes();

            if (!string.IsNullOrEmpty(editTrackMetadata.isrc))
            {
                if (dHTrack.identifiers == null) dHTrack.identifiers = new List<DHValueType>();

                int _index = dHTrack.identifiers.FindIndex(a => a.type == "isrc");
                if (_index > -1)
                {
                    dHTrack.identifiers[_index].value = editTrackMetadata.isrc.ReplaceSpecialCodes();
                }
                else
                {
                    dHTrack.identifiers.Add(new DHValueType() { type = "isrc", value = editTrackMetadata.isrc.ReplaceSpecialCodes() });
                }
            }

            if (!string.IsNullOrEmpty(editTrackMetadata.iswc))
            {
                if (dHTrack.identifiers == null) dHTrack.identifiers = new List<DHValueType>();

                int _index = dHTrack.identifiers.FindIndex(a => a.type == "iswc");
                if (_index > -1)
                {
                    dHTrack.identifiers[_index].value = editTrackMetadata.iswc.ReplaceSpecialCodes();
                }
                else
                {
                    dHTrack.identifiers.Add(new DHValueType() { type = "iswc", value = editTrackMetadata.iswc.ReplaceSpecialCodes() });
                }
            }

            if (!string.IsNullOrEmpty(editTrackMetadata.prs))
            {
                if (dHTrack.identifiers == null) dHTrack.identifiers = new List<DHValueType>();

                int _index = dHTrack.identifiers.FindIndex(a => a.type == "prs");
                if (_index > -1)
                {
                    dHTrack.identifiers[_index].value = editTrackMetadata.prs.ReplaceSpecialCodes();
                }
                else
                {
                    dHTrack.identifiers.Add(new DHValueType() { type = "prs", value = editTrackMetadata.prs.ReplaceSpecialCodes() });
                }
            }

            if (dHTrack.identifiers != null && dHTrack.identifiers.Count > 0 && dHTrack.identifiers.FindIndex(a => a.type == "extsysref") > -1)
            {
                dHTrack.identifiers.RemoveAt(dHTrack.identifiers.FindIndex(a => a.type == "extsysref"));
            }
            dHTrack.pre_release = editTrackMetadata.pre_release;

            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.performer.ToString(), editTrackMetadata.performers);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.composer.ToString(), editTrackMetadata.composers);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.publisher.ToString(), editTrackMetadata.publishers);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.translator.ToString(), editTrackMetadata.translators);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.arranger.ToString(), editTrackMetadata.arrangers);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.lyricist.ToString(), editTrackMetadata.lyricist);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.composer_lyricist.ToString(), editTrackMetadata.composer_lyricists);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.sub_arranger.ToString(), editTrackMetadata.sub_arranger);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.sub_adaptor.ToString(), editTrackMetadata.sub_adaptor);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.sub_lyricist.ToString(), editTrackMetadata.sub_lyricist);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.adaptor.ToString(), editTrackMetadata.adaptor);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.original_publisher.ToString(), editTrackMetadata.original_publisher);
            dHTrack.interestedParties.MapIpsFromEditTrack(enIPRole.sub_publisher.ToString(), editTrackMetadata.sub_publisher);
            dHTrack.genres = editTrackMetadata.genres;
            dHTrack.instruments = editTrackMetadata.instruments;
            dHTrack.styles = editTrackMetadata.styles;
            dHTrack.tags = editTrackMetadata.keywords;
            dHTrack.moods = editTrackMetadata.moods;
            dHTrack.musicOrigin = editTrackMetadata.musicorigin;
            dHTrack.position = string.IsNullOrWhiteSpace(editTrackMetadata.position) ? null : editTrackMetadata.position.ReplaceSpecialCodes();
            dHTrack.discNumber = string.IsNullOrWhiteSpace(editTrackMetadata.disc_number) ? null : editTrackMetadata.disc_number.ReplaceSpecialCodes();
            dHTrack.bpm = string.IsNullOrWhiteSpace(editTrackMetadata.bpm) ? null : editTrackMetadata.bpm.ReplaceSpecialCodes();
            dHTrack.tempo = string.IsNullOrWhiteSpace(editTrackMetadata.tempo) ? null : editTrackMetadata.tempo.ReplaceSpecialCodes();
            dHTrack.pLine = string.IsNullOrWhiteSpace(editTrackMetadata.pLine) ? null : editTrackMetadata.pLine.ReplaceSpecialCodes();
            dHTrack.notes = string.IsNullOrWhiteSpace(editTrackMetadata.track_notes) ? null : editTrackMetadata.track_notes.ReplaceSpecialCodes();
            dHTrack.alternativeTitle = editTrackMetadata.alternate_title.ReplaceSpecialCodes();

            if (!string.IsNullOrEmpty(editTrackMetadata.rec_label))
            {
                if (dHTrack.interestedParties?.Count() > 0)
                    dHTrack.interestedParties.RemoveAll(a => a.role == enIPRole.record_label.ToString());

                dHTrack.interestedParties.Add(new DHTInterestedParty()
                {
                    name = editTrackMetadata.rec_label.ReplaceSpecialCodes(),
                    role = enIPRole.record_label.ToString()
                });
            }

            if (dHTrack.interestedParties?.Count() == 0)
                dHTrack.interestedParties = null;

            if (dHTrack.identifiers?.Count() == 0)
                dHTrack.identifiers = null;

            //--- Clear BBC_ADMIN_NOTES and BBC_TRACK_ID
            if (dHTrack.descriptiveExtended?.Count() > 0) {
                dHTrack.descriptiveExtended.RemoveAll(a => a.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString());
                dHTrack.descriptiveExtended.RemoveAll(a => a.Type == enDescriptiveExtendedType.bbc_track_id.ToString());
            }

            if (!string.IsNullOrEmpty(editTrackMetadata.bbc_track_id)) {
                dHTrack.descriptiveExtended.Add(new DescriptiveData()
                {
                    DateExtracted = DateTime.Now,
                    Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                    Type = enDescriptiveExtendedType.bbc_track_id.ToString(),
                    Value = editTrackMetadata.bbc_track_id
                });
            }

            if (!string.IsNullOrEmpty(editTrackMetadata.org_admin_notes))
            {
                dHTrack.descriptiveExtended.Add(new DescriptiveData()
                {
                    DateExtracted = DateTime.Now,
                    Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                    Type = enDescriptiveExtendedType.bbc_admin_notes.ToString(),
                    Value = editTrackMetadata.org_admin_notes
                });
            }

            if (editTrackMetadata.orgTags != null)
            {
                foreach (var item in editTrackMetadata.orgTags)
                { 
                    if (item.Type == enAdminTypes.BBC_ADMIN_TAG.ToString())
                    {
                        dHTrack.tagsExtended.Add(item);
                    }                    
                }               
            }

            if (dHTrack.tagsExtended?.Count() == 0)
                dHTrack.tagsExtended = null;

            if (editTrackMetadata.contributor?.Count() > 0)
                dHTrack.contributorsExtended = editTrackMetadata.contributor;

            if (!string.IsNullOrEmpty(editTrackMetadata.valid_from_date) 
                || !string.IsNullOrEmpty(editTrackMetadata.valid_to_date)) {               

                DateTime dateTimeFrom;
                if (DateTime.TryParse(editTrackMetadata.valid_from_date,out dateTimeFrom)) {
                    dHTrack.validityPeriod.startDate = dateTimeFrom;
                }

                DateTime dateTimeTo;
                if (DateTime.TryParse(editTrackMetadata.valid_to_date, out dateTimeTo))
                {
                    dHTrack.validityPeriod.endDate = dateTimeTo;
                }
            }

            return dHTrack;
        }

        public static EditAlbumMetadata CreateEditAlbum(this DHAlbum dHAlbum)
        {
            EditAlbumMetadata editAlbumMetadata = new EditAlbumMetadata()
            {
                album_artist = dHAlbum.artist,
                album_notes = dHAlbum.notes,
                album_title = dHAlbum.name,
                album_subtitle = dHAlbum.subtitle,
                catalogue_number = dHAlbum.identifiers.CheckByValueType("catalogue_number"),
                upc = dHAlbum.identifiers.CheckByValueType("upc"),
                cLine = dHAlbum.cLine,
                library_id = dHAlbum.libraryId,
                album_release_date = dHAlbum.releaseDate,
                album_discs = dHAlbum.discs,
                album_orgTags = new List<Tag>(),
                org_album_adminTags = new List<string>(),
                release_year = dHAlbum.releaseYear
            };

            if (dHAlbum.descriptiveExtended?.Count() > 0)
            {
                foreach (var item in dHAlbum.descriptiveExtended)
                {
                    if (item.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString())
                    {
                        editAlbumMetadata.org_album_admin_notes = item.Value.ToString();
                        editAlbumMetadata.album_orgTags.Add(new Tag() { 
                            Type = enAdminTypes.BBC_ADMIN_NOTES.ToString(),
                            Value = item.Value.ToString()
                        }); 
                    }
                    if (item.Type == enDescriptiveExtendedType.bbc_album_id.ToString())
                    {
                        editAlbumMetadata.bbc_album_id = item.Value.ToString();
                        editAlbumMetadata.album_orgTags.Add(new Tag()
                        {
                            Type = enAdminTypes.BBC_ALBUM_ID.ToString(),
                            Value = item.Value.ToString()
                        });
                    }
                }
            }

            if (dHAlbum.tagsExtended?.Count() > 0)
            {               
                foreach (var item in dHAlbum.tagsExtended)
                {
                    if (item.Type == enAdminTypes.BBC_ADMIN_TAG.ToString())
                    {
                        editAlbumMetadata.album_orgTags.Add(new Tag() { 
                            Type = enAdminTypes.BBC_ADMIN_TAG.ToString(),
                            Value = item.Value
                        });
                        editAlbumMetadata.org_album_adminTags.Add(item.Value);
                    }
                }
            }

            return editAlbumMetadata;
        }

        private static string CheckByValueType(this List<DHValueType> dHValueTypes, string type)
        {
            if (dHValueTypes != null && dHValueTypes.Count > 0)
            {
                foreach (var item in dHValueTypes)
                {
                    if (type == item.type)
                    {
                        return item.value.ReplaceSpecialCodes();
                    }
                }
            }
            return "";
        }

        private static string GetIPName(this List<DHTInterestedParty> dHTInterestedParties, string role)
        {
            if (dHTInterestedParties != null && dHTInterestedParties.Count > 0)
            {
                foreach (var item in dHTInterestedParties)
                {
                    if (role == item.role)
                    {
                        return item.name;
                    }
                }
            }
            return "";
        }

        public static List<string> GetIPNameListByRole(this ICollection<DHTInterestedParty> interestedParties, string role)
        {
            if (interestedParties.Count > 0)
            {
                List<string> mLInterestedParties = interestedParties.Where(a => a.role == role).Select(a => a.name.ReplaceSpecialCodes()).ToList();

                if (mLInterestedParties?.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }

        public static string GetRecordLabel(this ICollection<DHTInterestedParty> interestedParties)
        {
            List<string> mLInterestedParties = GetIPNameListByRole(interestedParties, enIPRole.record_label.ToString());
            if (mLInterestedParties?.Count() > 0)
                return mLInterestedParties[0];

            return string.Empty;
        }

        private static List<string> GetNameListByRole(string role, ICollection<DHTInterestedParty> interestedParties)
        {
            if (interestedParties.Count > 0)
            {
                List<string> mLInterestedParties = interestedParties.Where(a => a.role == role).Select(a => a.name.ReplaceSpecialCodes()).ToList();

                if (mLInterestedParties?.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }

        private static List<Contributor> BindIpToContributor(string role, ICollection<DHTInterestedParty> interestedParties, List<Contributor> contributors)
        {
            if (interestedParties.Count > 0)
            {
                List<string> mLInterestedParties = interestedParties.Where(a => a.role == role).Select(a => a.name).ToList();

                if (mLInterestedParties.Count > 0)
                {
                    foreach (var item in mLInterestedParties)
                    {
                        contributors.Add(new Contributor()
                        {
                            Name = item,
                            Role = role
                        });
                    }
                }
            }
            return contributors;
        }

        private static List<DHTInterestedParty> MapIpsFromEditTrack(this List<DHTInterestedParty> dHTInterestedParties, string role, List<string> editList)
        {
            if (editList == null)
                return dHTInterestedParties;


            List<DHTInterestedParty> _ipList = dHTInterestedParties;

            if (_ipList?.Count() > 0)
                _ipList.RemoveAll(a => a.role == role);

            List<DHTInterestedParty> _dHTInterestedParties = editList.Select(a => new DHTInterestedParty()
            {
                role = role,
                name = a.ReplaceSpecialCodes()
            }).ToList();

            if (_dHTInterestedParties.Count() > 0)
                _ipList.AddRange(_dHTInterestedParties);

            return _ipList;
        }

        public static ICollection<InterestedParty> GetIpsValueToCollection(this List<MLInterestedParty> mLInterestedParties)
        {
            List<InterestedParty> partyCollection = new List<InterestedParty>();

            if (mLInterestedParties != null)
            {
                foreach (var party in mLInterestedParties)
                {
                    var ip = new Soundmouse.Messaging.Model.InterestedParty();
                    ip.FullName = party.fullName;
                    ip.Role = party.role;
                    partyCollection.Add(ip);
                }
            }

            return partyCollection;
        }

        public static ICollection<InterestedParty> AddInterestedParty(this ICollection<InterestedParty> interestedParties, List<string> ips, enIPRole enIPRole)
        {
            if (ips?.Count > 0)
                foreach (var item in ips)
                {
                    interestedParties.Add(new InterestedParty()
                    {
                        FullName = item,
                        Role = enIPRole.ToString()
                    });
                }
            return interestedParties;
        }

        public static ICollection<InterestedParty> RemoveInterestedParty(this ICollection<InterestedParty> interestedParties, enIPRole enIPRole)
        {
            interestedParties = interestedParties.Where(a => a.Role != enIPRole.ToString()).ToList();
            return interestedParties;
        }

        public static IDictionary<string, string> UpdateDictionary(this IDictionary<string, string> list, string key, string value)
        {
            if (list == null)
            {
                list = new Dictionary<string, string> { { key, value } };
                return list;
            }

            if (!string.IsNullOrEmpty(value))
            {
                list[key] = value.Trim();
            }
            else
            {
                list.Remove(key);
            }
            return list;
        }

    }
}
