using Elasticsearch.DataMatching;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace MusicManager.Logics.Helper
{
    public static class CommonHelper
    {
        public static long GetCurrentUtcEpochTime()
        {
           //return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
           return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }

        public static long GetCurrentUtcEpochTimeInSeconds()
        {
            //return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static long GetCurrentUtcEpochTimeMicroseconds()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds * 1000;
            //return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        }

        public static XMLMetadata ExtractXML(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return null;

            //xml = xml.Replace("&", "&amp;");
            XMLMetadata audioMeta = null;
            XmlSerializer serializer = new XmlSerializer(typeof(XMLMetadata));

            try
            {
                // convert string to stream
                byte[] byteArray = Encoding.UTF8.GetBytes(xml);
                MemoryStream stream = new MemoryStream(byteArray);
                audioMeta = (XMLMetadata)serializer.Deserialize(stream);
            }
            catch (Exception ex)
            {

            }

            return audioMeta;
        }

        public static BBCXmlMetadata ExtractBBCXML(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return null;

            BBCXmlMetadata audioMeta = null;
            XmlSerializer serializer = new XmlSerializer(typeof(BBCXmlMetadata));

            try
            {
                // convert string to stream
                byte[] byteArray = Encoding.UTF8.GetBytes(xml);
                MemoryStream stream = new MemoryStream(byteArray);
                audioMeta = (BBCXmlMetadata)serializer.Deserialize(stream);
            }
            catch (Exception)
            {

            }
            return audioMeta;
        }

        public static DHAlbum CreateAssetHubAlbum(XMLMetadata audioMeta, string sourceRef)
        {
            try
            {
                //Create Album API Call JSON
                DHAlbum maAlbum = new DHAlbum()
                {
                    name = audioMeta.ConvertedFile.IDTags.Album,
                    identifiers = new List<DHValueType>()
                };
                
                string strDisc = audioMeta.ConvertedFile?.IDTags?.Disc;

                if (!string.IsNullOrEmpty(strDisc) && strDisc.Contains("/")) {
                    strDisc = strDisc.Split('/')[1].Trim();
                }                

                if (int.TryParse(strDisc, out int disc)) {
                    maAlbum.discs = disc;
                }

                

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.AlbumArtist))
                {
                    maAlbum.artist = audioMeta.ConvertedFile.IDTags.AlbumArtist;
                }
                else
                {
                    string[] chkArtist = audioMeta.ConvertedFile.FileNamePath.Trim().TrimEnd('\\').Split(@"\");

                    //Extract the Artist
                    string artist = chkArtist[chkArtist.Length - 2];

                    if (artist.Contains("Various") || artist.Contains("Variious"))
                    {
                        maAlbum.artist = artist;
                    }
                    else
                    {
                        maAlbum.artist = artist;
                    }
                }

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.Catalog))
                    maAlbum.identifiers.Add(new DHValueType() { type = "catalogue_number", value = audioMeta.ConvertedFile.IDTags.Catalog });

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.UPC))
                    maAlbum.identifiers.Add(new DHValueType() { type = "upc", value = audioMeta.ConvertedFile.IDTags.UPC });

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.BBCBarcode)) {
                    maAlbum.descriptiveExtended = new List<DescriptiveData>();
                    maAlbum.descriptiveExtended.Add(new DescriptiveData() { 
                        DateExtracted = DateTime.Now,
                        Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                        Type = enDescriptiveExtendedType.bbc_album_id.ToString(),
                        Value = audioMeta.ConvertedFile.IDTags.BBCBarcode
                    });
                }                    

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.Year))
                {
                    string year = null;
                    string[] date = audioMeta.ConvertedFile.IDTags.Year.Split(' ');

                    switch (date.Count())
                    {
                        case 1:
                            year = date[0] + "-01-01";
                            break;
                        case 2:
                            year = date[0] + "-" + date[1] + "-01";
                            break;
                        case 3:
                            year = date[0] + "-" + date[1] + "-" + date[2];
                            break;
                    }

                    maAlbum.releaseDate = year;
                }

                if (!string.IsNullOrWhiteSpace(sourceRef))
                {
                    maAlbum.miscellaneous = new DHAMiscellaneous()
                    {
                        sourceRef = sourceRef
                    };
                }

                return maAlbum;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static DHAlbum CreateAssetHubAlbumFromBBC(BBCXmlMetadata audioMeta, string sourceRef)
        {
            try
            {
                //Create Album API Call JSON
                DHAlbum maAlbum = new DHAlbum()
                {
                    name = audioMeta.AlbumTitle,//.Contains("'") ? audioMeta.AlbumTitle.Replace("'", "") : audioMeta.AlbumTitle,
                    identifiers = new List<DHValueType>(),
                    upc = audioMeta.ProductUPC
                };

                if (!string.IsNullOrEmpty(audioMeta.ProductArtist))
                {
                    maAlbum.artist = audioMeta.ProductArtist;
                }

                if (!string.IsNullOrEmpty(audioMeta.CatalogueNo))
                    maAlbum.identifiers.Add(new DHValueType() { type = "catalogue_number", value = audioMeta.CatalogueNo });

                if (!string.IsNullOrEmpty(audioMeta.ProductUPC))
                    maAlbum.identifiers.Add(new DHValueType() { type = "upc", value = audioMeta.ProductUPC });

                if (!string.IsNullOrEmpty(audioMeta.ReleaseYear))
                {
                    string year = null;
                    year = audioMeta.ReleaseYear + "-01-01";

                    maAlbum.releaseDate = year;
                }

                if (!string.IsNullOrWhiteSpace(sourceRef))
                {
                    maAlbum.miscellaneous = new DHAMiscellaneous()
                    {
                        sourceRef = sourceRef
                    };
                }

                return maAlbum;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static DHTrack CreateAssetHubTrack(XMLMetadata audioMeta, string unique_id, string albumId, string sourceRef)
        {
            try
            {

                string[] trackFileName = Path.GetFileNameWithoutExtension(audioMeta.ConvertedFile.FileName).Split('-');

                string title = audioMeta.ConvertedFile.IDTags.Title;
                if (string.IsNullOrEmpty(title) && trackFileName.Count() > 2)
                {
                    title = trackFileName[1].Trim();
                }                

                DHTrack mATrack = new DHTrack()
                {
                    filename = audioMeta.ConvertedFile.FileName,
                    title = title,
                    musicOrigin = "commercial",
                    territories = new DHTTerritories()
                    {
                        include = new List<string>()
                    },
                    interestedParties = new List<DHTInterestedParty>()
                };

                if (!string.IsNullOrWhiteSpace(albumId))
                {
                    mATrack.albumId = new Guid(albumId);
                }

                if (!string.IsNullOrEmpty(unique_id))
                {
                    mATrack.uniqueId = unique_id;
                }

                mATrack.territories.include.Add("UK");               

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags?.Artist))
                {
                    foreach (string com in audioMeta.ConvertedFile.IDTags?.Artist.Split("\n"))
                    {
                        mATrack.interestedParties.Add(new DHTInterestedParty() { role = "performer", name = com });
                    }
                }

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags?.BBCBarcode))
                {
                   
                }


                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags?.Composer)) {                    
                    foreach (string com in audioMeta.ConvertedFile.IDTags?.Composer.Split("\n"))
                    {
                        mATrack.interestedParties.Add(new DHTInterestedParty() { role = "composer", name = com });
                    }
                }

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags?.Publisher))
                {
                    foreach (string com in audioMeta.ConvertedFile.IDTags?.Publisher.Split("\n"))
                    {
                        mATrack.interestedParties.Add(new DHTInterestedParty() { role = "publisher", name = com });
                    }
                }

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags?.Label))
                {                    
                    foreach (string label in audioMeta.ConvertedFile.IDTags?.Label.Split("\n"))
                    {
                        mATrack.interestedParties.Add(new DHTInterestedParty() { role = "record_label", name = label });
                    }
                }

                mATrack.position = audioMeta.ConvertedFile.IDTags.Track.Split('/')[0].Trim();
                mATrack.discNumber = audioMeta.ConvertedFile.IDTags.Disc.Split('/')[0].Trim();

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.Length))
                {
                    int.TryParse(audioMeta.ConvertedFile.IDTags.Length, out int duration);
                    duration = duration / 1000;
                    mATrack.duration = duration;
                }

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.Genre))
                {
                    mATrack.genres = new List<string>();

                    string[] lstGenre = audioMeta.ConvertedFile.IDTags.Genre.Split("\n");
                    foreach (string genre in lstGenre)
                    {
                        mATrack.genres.Add(genre);
                    }
                }

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.Style))
                {
                    mATrack.styles = new List<string>();

                    string[] lstStyle = audioMeta.ConvertedFile.IDTags.Style.Split("\n");
                    foreach (string style in lstStyle)
                    {
                        mATrack.styles.Add(style);
                    }
                }

                if (!string.IsNullOrWhiteSpace(sourceRef))
                {
                    mATrack.miscellaneous = new DHTMiscellaneous()
                    {
                        sourceRef = sourceRef
                    };
                }

                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile.IDTags.ISRC))
                {
                    mATrack.identifiers = new List<DHValueType>() {
                        new DHValueType(){
                            type = "isrc",
                            value = audioMeta.ConvertedFile.IDTags.ISRC
                        }
                    };
                }

                return mATrack;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string GetTrackTitle_BBCXML(BBCXmlMetadata audioMeta)
        {
            string title = null;
            if (!string.IsNullOrEmpty(audioMeta?.SongTitle))
            {
                title = audioMeta.SongTitle;//.Contains("'") ? audioMeta.SongTitle.Replace("'", "") : audioMeta.SongTitle;
            }
            return title;
        }

        public static string GetTrackTitle_XML(XMLMetadata audioMeta)
        {
            string title = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(audioMeta.ConvertedFile?.FileName)) {
                    string[] trackFileName = Path.GetFileNameWithoutExtension(audioMeta.ConvertedFile.FileName).Split('-');

                    title = audioMeta.ConvertedFile.IDTags.Title;
                    if (string.IsNullOrEmpty(title))
                    {
                        title = trackFileName[1].Trim().Contains("'") ? trackFileName[1].Trim().Replace("'", "") : trackFileName[1].Trim();
                    }
                }               
            }
            catch
            {
            }
            return title;
        }

        public static DHTrack CreateAssetHubTrackFromBBC(BBCXmlMetadata audioMeta, string unique_id, string albumId, string sourceRef)
        {
            try
            {
                string title = null;
                if (!string.IsNullOrEmpty(audioMeta.SongTitle))
                {
                    title = audioMeta?.SongTitle?.Trim();//.Contains("'") ? audioMeta.SongTitle.Replace("'", "") : audioMeta.SongTitle;
                }

                string trackArtist = audioMeta.ArtistName;

                DHTrack mATrack = new DHTrack()
                {
                    title = title,
                    musicOrigin = "commercial",
                    filename = audioMeta.FileName,
                    territories = new DHTTerritories()
                    {
                        include = new List<string>()
                    },
                    interestedParties = new List<DHTInterestedParty>(),
                    identifiers = new List<DHValueType>()
                };

                if (!string.IsNullOrWhiteSpace(albumId))
                {
                    mATrack.albumId = new Guid(albumId);
                }

                if (!string.IsNullOrEmpty(unique_id))
                {
                    mATrack.uniqueId = unique_id;
                }

                mATrack.territories.include.Add("UK");

                if (!string.IsNullOrEmpty(trackArtist))
                {
                    string[] lstArtist = trackArtist.Split(new char[] { '\n', ',' });
                    foreach (string artist in lstArtist)
                    {
                        mATrack.interestedParties.Add(new DHTInterestedParty() { role = "performer", name = artist });
                    }
                }

                if (!string.IsNullOrEmpty(audioMeta.Composer))
                {
                    string[] lstComposers = audioMeta.Composer.Split(new char[] { '\n', ',' });
                    foreach (string composer in lstComposers)
                    {
                        mATrack.interestedParties.Add(new DHTInterestedParty() { role = "composer", name = composer });
                    }
                }


                if (!string.IsNullOrEmpty(audioMeta.Publisher))
                {
                    string[] lstPublishers = audioMeta.Publisher.Split(new char[] { '\n', ',' });
                    foreach (string publisher in lstPublishers)
                    {
                        mATrack.interestedParties.Add(new DHTInterestedParty() { role = "publisher", name = publisher });
                    }
                }

                if (!string.IsNullOrEmpty(audioMeta.ProductLabel))
                {
                    if (!string.IsNullOrEmpty(audioMeta.ProductLabel))
                    {
                        string[] lstLabel = audioMeta.ProductLabel.Split(new char[] { '\n', ',' });
                        foreach (string label in lstLabel)
                        {
                            mATrack.interestedParties.Add(new DHTInterestedParty() { role = "record_label", name = label });
                        }
                    }
                }


                mATrack.position = audioMeta.TrackNo.Split('/')[0].Trim();
                mATrack.discNumber = audioMeta.Disc.ToString();

                if (!string.IsNullOrEmpty(audioMeta.ID3Genre))
                {
                    mATrack.genres = new List<string>();

                    string[] lstGenre = audioMeta.ID3Genre.Split("\n");
                    foreach (string genre in lstGenre)
                    {
                        mATrack.genres.Add(genre);
                    }
                }

                if (!string.IsNullOrEmpty(audioMeta.InstrumentOrVoice))
                {
                    mATrack.instruments = new List<string>();

                    string[] lstInstruments = audioMeta.ID3Genre.Split("\n");
                    foreach (string instrument in lstInstruments)
                    {
                        mATrack.instruments.Add(instrument);
                    }
                }


                if (!string.IsNullOrEmpty(audioMeta.Style))
                {
                    mATrack.styles = new List<string>();

                    string[] lstStyle = audioMeta.Style.Split("\n");
                    foreach (string style in lstStyle)
                    {
                        mATrack.styles.Add(style);
                    }
                }

                if (!string.IsNullOrWhiteSpace(sourceRef))
                {
                    mATrack.miscellaneous = new DHTMiscellaneous()
                    {
                        sourceRef = sourceRef
                    };
                }

                if (!string.IsNullOrEmpty(audioMeta.TrackISRC))
                {
                    mATrack.identifiers = new List<DHValueType>() {
                        new DHValueType(){
                            type = "isrc",
                            value = audioMeta.TrackISRC
                        }
                    };
                }
                mATrack.bpm = audioMeta.TempoBPM;

                return mATrack;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static DHTrack CreateEditAssetHubTrack(Track dhTrackDocument, string unique_id)
        {
            try
            {
                DHTrack mATrack = new DHTrack()
                {
                    filename = dhTrackDocument.TrackData.FileName.CheckEmptyString(),
                    title = dhTrackDocument.TrackData.Title,
                    musicOrigin = dhTrackDocument.TrackData.MusicOrigin,
                    alternativeTitle = dhTrackDocument.TrackData.AlternativeTitle.CheckEmptyString(),
                    bpm = dhTrackDocument.TrackData.Bpm.CheckEmptyString(),
                    discNumber = dhTrackDocument.TrackData.DiscNumber.CheckEmptyString(),
                    duration = dhTrackDocument.TrackData.Duration,
                    pLine = dhTrackDocument.TrackData.PLine.CheckEmptyString(),
                    notes = dhTrackDocument.TrackData.Notes.CheckEmptyString(),
                    publicDomainRecording = dhTrackDocument.TrackData.PublicDomain,
                    publicDomainWork = dhTrackDocument.TrackData.PublicDomainWork,
                    position = dhTrackDocument.TrackData.Position.CheckEmptyString(),
                    seriousMusic = dhTrackDocument.TrackData.SeriousMusic,
                    versionTitle = dhTrackDocument.TrackData.VersionTitle.CheckEmptyString(),
                    retitled = dhTrackDocument.TrackData.Retitled,
                    id = dhTrackDocument.Id,
                    subIndex = dhTrackDocument.TrackData.SubIndex.CheckEmptyString(),
                    stem = dhTrackDocument.TrackData.Stem,
                    tempo = dhTrackDocument.TrackData.Tempo.CheckEmptyString(),
                    territories = new DHTTerritories()
                    {
                        include = new List<string>()
                    },
                    moods = dhTrackDocument.TrackData.Moods != null ? dhTrackDocument.TrackData.Moods.ToList() : null,
                    genres = dhTrackDocument.TrackData.Genres != null ? dhTrackDocument.TrackData.Genres.ToList() : null,
                    instruments = dhTrackDocument.TrackData.Instrumentations != null ? dhTrackDocument.TrackData.Instrumentations.ToList() : null,
                    styles = dhTrackDocument.TrackData.Styles != null ? dhTrackDocument.TrackData.Styles.ToList() : null,
                    tags = dhTrackDocument.TrackData.Keywords != null ? dhTrackDocument.TrackData.Keywords.ToList() : null,
                };

                if (dhTrackDocument.TrackData.ContributorsExtended?.Count() > 0) {
                    mATrack.contributorsExtended = new List<Contributor>();
                    mATrack.contributorsExtended = dhTrackDocument.TrackData.ContributorsExtended?.ToList();
                }                   

                KeyValuePair<string, string> orignSubOrigin = TrackDocumentExtensions.CleanseOrigin(mATrack.musicOrigin);
                mATrack.musicOrigin = orignSubOrigin.Key;

                if (dhTrackDocument.TrackData.DescriptiveExtended?.Count() > 0)
                {
                    mATrack.descriptiveExtended = new List<Soundmouse.Messaging.Model.DescriptiveData>();
                    foreach (var item in dhTrackDocument.TrackData.DescriptiveExtended)
                    {
                        mATrack.descriptiveExtended.Add(item);
                    }
                }               

                if (dhTrackDocument.TrackData.TagsExtended?.Count() > 0)
                {
                    mATrack.tagsExtended = new List<Soundmouse.Messaging.Model.Tag>();
                    foreach (var item in dhTrackDocument.TrackData.TagsExtended)
                    {
                        mATrack.tagsExtended.Add(item);
                    }
                }              
                

                if (!string.IsNullOrEmpty(unique_id))
                    mATrack.uniqueId = unique_id;

                if (dhTrackDocument.TrackData.Product != null)
                {
                    mATrack.albumId = dhTrackDocument.TrackData.Product.Id;
                }

                if (dhTrackDocument.TrackData.LibraryId != null)
                    mATrack.libraryId = dhTrackDocument.TrackData.LibraryId;

                if (dhTrackDocument.Territories?.Count() > 0)
                {                    
                    Regex regex = new Regex(@"^[a-z]{2}$", RegexOptions.IgnoreCase);

                    foreach (var item in dhTrackDocument.Territories)
                    {
                        if (regex.IsMatch(item))
                            mATrack.territories.include.Add(item);
                    }
                }
                else
                {
                    mATrack.territories.include.Add("UK");
                }


                if (dhTrackDocument.TrackData.InterestedParties != null && dhTrackDocument.TrackData.InterestedParties.Count > 0)
                {
                    mATrack.interestedParties = new List<DHTInterestedParty>();
                    foreach (var item in dhTrackDocument.TrackData.InterestedParties)
                    {
                        mATrack.interestedParties.Add(new DHTInterestedParty()
                        {
                            name = item.FullName,
                            role = item.Role == "lyricists" ? "lyricist" : item.Role,
                        });
                    }
                }

                if (dhTrackDocument.TrackData.Identifiers != null && dhTrackDocument.TrackData.Identifiers.Count > 0)
                {
                    string[] identifiresTypes = {
                        "apra", "ascap", "bmi", "cdc", "gema", "isrc", "iswc", "jasrac", "nex_tone", "prs", "sesac", "stim"
                    };

                    mATrack.identifiers = new List<DHValueType>();
                    foreach (var item in dhTrackDocument.TrackData.Identifiers)
                    {
                        if (item.Key == "extsysref")
                        {
                            mATrack.miscellaneous = new DHTMiscellaneous()
                            {
                                sourceRef = item.Value
                            };                            
                        }
                        else if(identifiresTypes.Contains(item.Key))
                        {
                            mATrack.identifiers.Add(new DHValueType()
                            {
                                type = item.Key,
                                value = item.Value
                            });
                        }
                    }
                }

                if (dhTrackDocument.Source?.ValidFrom != null || 
                    dhTrackDocument.Source?.ValidTo != null) {

                    mATrack.validityPeriod = new DHValidityPeriod() { 
                        endDate = dhTrackDocument.Source?.ValidTo,
                        startDate = dhTrackDocument.Source?.ValidFrom
                    };
                }

                return mATrack;
            }
            catch (Exception)
            {
                return null;
            }
        }

        

        public static DHAlbum CreateDHAlbumFromProduct(Product product, string sourceRef, Guid? libraryId, Guid? uniqueId)
        {
            try
            {
                if (product == null)
                    return null;

                DHAlbum dHAlbum = new DHAlbum()
                {
                    name = product.Name,
                    artist = product.Artist.CheckEmptyString(),
                    cLine = product.CLine.CheckEmptyString(),
                    discs = product.NumberOfDiscs.CheckEmptyString() == null ? 0 : int.Parse(product.NumberOfDiscs.CheckEmptyString()),
                    id = product.Id,
                    notes = product.Notes.CheckEmptyString(),
                    subtitle = product.SubName.CheckEmptyString(),
                    miscellaneous = new DHAMiscellaneous()
                    {
                        sourceRef = sourceRef
                    },
                    tagsExtended = new List<Tag>()
                };

                if (product.DescriptiveExtended?.Count()>0)
                {
                    dHAlbum.descriptiveExtended = product.DescriptiveExtended.ToList();
                }

                if (product.TagsExtended?.Length > 0)
                {
                    foreach (var item in product.TagsExtended)
                    {
                        dHAlbum.tagsExtended.Add(new Tag() { Type = item.Type, Value = item.Value });
                    }
                }                

                if (uniqueId != null)
                    dHAlbum.uniqueId = uniqueId.ToString();

                if (dHAlbum.discs <= 0)
                    dHAlbum.discs = null;

                if (product.ReleaseDate != null)
                    dHAlbum.releaseDate = product.ReleaseDate?.ToString("yyyy-MM-dd");

                if (product.Year !=null && product.Year > 0) {
                    dHAlbum.releaseYear = product.Year.ToString();
                }

                if (libraryId != null)
                    dHAlbum.libraryId = libraryId;

                if (product.Identifiers != null && product.Identifiers.Count > 0)
                {
                    dHAlbum.identifiers = new List<DHValueType>();

                    string[] identifiresTypes = {
                        "catalogue_number", "ean", "grid", "upc"
                    };  

                    foreach (var item in product.Identifiers)
                    {
                        if (item.Key == "extsysref")
                        {
                            dHAlbum.uniqueId = item.Value;
                        }
                        else if (item.Key == "catalogue_number_2") {
                            dHAlbum.identifiers.Add(new DHValueType()
                            {
                                type = "catalogue_number",
                                value = item.Value
                            });
                        }
                        else if(identifiresTypes.Contains(item.Key))
                        {
                            dHAlbum.identifiers.Add(new DHValueType()
                            {
                                type = item.Key,
                                value = item.Value
                            });
                        }
                    }

                    if (dHAlbum.identifiers.Count() == 0)
                        dHAlbum.identifiers = null;
                }             

                return dHAlbum;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static DHAlbum UpdateDHAlbumFromEditAlbumMetadata(this DHAlbum dHAlbum, EditAlbumMetadata albumMetadata)
        {
            if (dHAlbum == null) dHAlbum = new DHAlbum();

            dHAlbum.artist = albumMetadata.album_artist.CheckEmptyString();
            dHAlbum.notes = albumMetadata.album_notes.CheckEmptyString();
            dHAlbum.name = albumMetadata.album_title;
            dHAlbum.subtitle = albumMetadata.album_subtitle.CheckEmptyString();
            dHAlbum.cLine = albumMetadata.cLine.CheckEmptyString();
            dHAlbum.libraryId = albumMetadata.library_id;
            dHAlbum.releaseDate = albumMetadata.album_release_date;
            dHAlbum.discs = albumMetadata.album_discs.CheckDiscs();
            dHAlbum.identifiers = new List<DHValueType>();
            dHAlbum.tagsExtended = new List<Tag>();
            dHAlbum.releaseYear = albumMetadata.release_year;

            if (dHAlbum.descriptiveExtended == null)
                dHAlbum.descriptiveExtended = new List<DescriptiveData>();

            if (albumMetadata.prod_year > 1000) {
                dHAlbum.year = albumMetadata.prod_year;
            }  

            if (!string.IsNullOrEmpty(albumMetadata.catalogue_number))
            {
                dHAlbum.identifiers.Add(new DHValueType() { type = "catalogue_number", value = albumMetadata.catalogue_number });
            }
            
            if (!string.IsNullOrEmpty(albumMetadata.upc))
            {
                dHAlbum.identifiers.Add(new DHValueType() { type = "upc", value = albumMetadata.upc });
            }

            //--- Clear BBC_ADMIN_NOTES and BBC_ALBUM_ID
            if (dHAlbum.descriptiveExtended?.Count() > 0)
            {
                dHAlbum.descriptiveExtended.RemoveAll(a => a.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString());
                dHAlbum.descriptiveExtended.RemoveAll(a => a.Type == enDescriptiveExtendedType.bbc_album_id.ToString());
            }

            if (!string.IsNullOrEmpty(albumMetadata.bbc_album_id)) {
                dHAlbum.descriptiveExtended.Add(new DescriptiveData()
                {
                    DateExtracted = DateTime.Now,
                    Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                    Type = enDescriptiveExtendedType.bbc_album_id.ToString(),
                    Value = albumMetadata.bbc_album_id
                });
            }

            if (!string.IsNullOrEmpty(albumMetadata.org_album_admin_notes))
            {
                dHAlbum.descriptiveExtended.Add(new DescriptiveData()
                {
                    DateExtracted = DateTime.Now,
                    Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                    Type = enDescriptiveExtendedType.bbc_admin_notes.ToString(),
                    Value = albumMetadata.org_album_admin_notes
                });
            }

            if (albumMetadata.album_orgTags != null)
            {
                foreach (var item in albumMetadata.album_orgTags)
                {  
                    if (item.Type == enAdminTypes.BBC_ADMIN_TAG.ToString())
                    {
                        dHAlbum.tagsExtended.Add(item);
                    }                                         
                }
            }

            if (dHAlbum.identifiers?.Count() == 0)
                dHAlbum.identifiers = null;

            if (dHAlbum.descriptiveExtended?.Count() == 0)
                dHAlbum.descriptiveExtended = null;

            if (dHAlbum.tagsExtended?.Count() == 0)
                dHAlbum.tagsExtended = null;

            return dHAlbum;
        }

        public static DHAlbum CreateDHAlbumFromEditAlbumMetadata(this EditAlbumMetadata mLAlbumMetadataEdit, Guid uniqueId, org_user orgUser)
        {
            DHAlbum dHAlbum = new DHAlbum()
            {
                id = mLAlbumMetadataEdit.id,
                artist = mLAlbumMetadataEdit.album_artist.CheckEmptyString(),
                name = mLAlbumMetadataEdit.album_title,
                uniqueId = uniqueId.ToString(),
                discs = mLAlbumMetadataEdit.album_discs.CheckDiscs(),
                notes = mLAlbumMetadataEdit.album_notes.CheckEmptyString(),
                libraryId = mLAlbumMetadataEdit.library_id,
                identifiers = new List<DHValueType>(),
                miscellaneous = new DHAMiscellaneous()
                {
                    sourceRef = string.IsNullOrEmpty(mLAlbumMetadataEdit.album_source_ref) ? "ML_ALBUM_ADD" : mLAlbumMetadataEdit.album_source_ref
                },
                descriptiveExtended = new List<DescriptiveData>(),
                tagsExtended = new List<Tag>()
            };

            if (mLAlbumMetadataEdit.album_orgTags != null)
            {   
                foreach (var item in mLAlbumMetadataEdit.album_orgTags)
                {
                    enDescriptiveExtendedType? _enDescriptiveExtendedType = null;

                    if (item.Type == enAdminTypes.BBC_ADMIN_TAG.ToString())
                    {
                        dHAlbum.tagsExtended.Add(item);                        
                    }
                    else if (item.Type == enAdminTypes.BBC_ADMIN_NOTES.ToString())
                    {
                        _enDescriptiveExtendedType = enDescriptiveExtendedType.bbc_admin_notes;
                    }
                    else if (item.Type == enAdminTypes.BBC_ALBUM_ID.ToString())
                    {
                        _enDescriptiveExtendedType = enDescriptiveExtendedType.bbc_album_id;
                    }

                    if (_enDescriptiveExtendedType != null)
                        dHAlbum.descriptiveExtended.Add(new DescriptiveData()
                        {
                            DateExtracted = DateTime.Now,
                            Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                            Type = _enDescriptiveExtendedType.ToString(),
                            Value = item.Value
                        });
                }                
            }

            if (!string.IsNullOrEmpty(mLAlbumMetadataEdit.album_release_date))
            {
                dHAlbum.releaseDate = mLAlbumMetadataEdit.album_release_date;
            }

            if (int.TryParse(mLAlbumMetadataEdit.release_year, out int a))
            {
                dHAlbum.releaseYear = mLAlbumMetadataEdit.release_year;
            }            

            if (!string.IsNullOrEmpty(mLAlbumMetadataEdit.cLine))
            {
                dHAlbum.cLine = mLAlbumMetadataEdit.cLine;
            }

            if (!string.IsNullOrEmpty(mLAlbumMetadataEdit.album_subtitle))
            {
                dHAlbum.subtitle = mLAlbumMetadataEdit.album_subtitle;
            }

            if (!string.IsNullOrEmpty(mLAlbumMetadataEdit.catalogue_number))
            {
                dHAlbum.identifiers.Add(new DHValueType()
                {
                    type = "catalogue_number",
                    value = mLAlbumMetadataEdit.catalogue_number
                });
            }

            if (!string.IsNullOrEmpty(mLAlbumMetadataEdit.upc))
            {
                dHAlbum.identifiers.Add(new DHValueType()
                {
                    type = "upc",
                    value = mLAlbumMetadataEdit.upc
                });
            }

            TrackChangeLog trackChangeLog = new TrackChangeLog()
            {
                Action = enAlbumChangeLogAction.UPLOAD.ToString(),
                UserId = orgUser.user_id,
                DateCreated = DateTime.Now,
                UserName = orgUser.first_name != null ? orgUser.first_name + " " + orgUser.last_name : "",
                RefId = uniqueId
            };
           
            dHAlbum.descriptiveExtended.Add(new DescriptiveData()
            {
                DateExtracted = DateTime.Now,
                Source = enDescriptiveExtendedSource.ML_UPLOAD.ToString(),
                Type = enDescriptiveExtendedType.upload_album_id.ToString(),
                Value = trackChangeLog
            });

            if (dHAlbum.descriptiveExtended?.Count() == 0)
                dHAlbum.descriptiveExtended = null;

            if (dHAlbum.tagsExtended?.Count() == 0)
                dHAlbum.tagsExtended = null;

            return dHAlbum;
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static string CheckEmptyString(this string val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return null;

            return val;
        }

        private static int? CheckDiscs(this int? val)
        {
            if(val == null || val == 0)
            {
                return null;
            }

            return val;
        }
    }
}
