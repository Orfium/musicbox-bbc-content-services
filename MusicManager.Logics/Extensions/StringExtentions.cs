using Elasticsearch.Util;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicManager.Logics.Extensions
{
    public static class StringExtensions
    {
        public static string PPLLabelSearchReplace(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str.Trim().TrimEnd('.').Replace("'", "''").Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)")
                .Replace("[", @"\[").Replace("]", @"\]").Replace("\t","").Trim();
        }

        public static byte[] GetBytes(this Stream stream)
        {
            var bytes = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.ReadAsync(bytes, 0, bytes.Length);
            stream.Dispose();
            return bytes;
        }

        public static char[] GetInvalidFileNameChars() => new char[]
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, ':', '*', '?', '\\', '/'
        };

        public static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(GetInvalidFileNameChars())).Replace(" ", "_");
        }

        private static string FilenameCleanup(string name)
        {
            string regex = $"[ /\\?%*:|\"<>]/g";
            return Regex.Replace(name, regex, "_").Replace(" ", "_");
        }

        public static string CreateFileNameByMLMasterTrack(this MLTrackDocument mLTrackDocument)
        {
            if (mLTrackDocument == null)
                return null;

            string filename = string.Empty;
            if (mLTrackDocument.musicOrigin?.StartsWith("library") == true)
            {
                string library = !string.IsNullOrEmpty(mLTrackDocument.libName) ? mLTrackDocument.libName : "[Untitled Library]";
                filename =
                  library +
                  (!string.IsNullOrEmpty(mLTrackDocument.catNo) ? " - " + mLTrackDocument.catNo : "") +
                  (!string.IsNullOrEmpty(mLTrackDocument.position) ? " - " + mLTrackDocument.position : "") +
                  (!string.IsNullOrEmpty(mLTrackDocument.trackTitle) ? " - " + mLTrackDocument.trackTitle : "[Untitled Track]") +
                  (!string.IsNullOrEmpty(mLTrackDocument.trackVersionTitle) ? $"_({mLTrackDocument.trackVersionTitle})" : "");
            }
            else if (mLTrackDocument.musicOrigin?.StartsWith("commercial") == true)
            {
                string performer = mLTrackDocument.performer?.Count > 0
                    ? mLTrackDocument.performer[0] + (mLTrackDocument.performer.Count > 1 ? "_etal" : "")
                    : !string.IsNullOrEmpty(mLTrackDocument.prodArtist)
                    ? mLTrackDocument.prodArtist + "_a_"
                    : "[Unknown Artist]";

                string position =
                  (!string.IsNullOrEmpty(mLTrackDocument.position)
                    ? mLTrackDocument.position
                    : "[na]") +
                  "_" +
                  (!string.IsNullOrEmpty(mLTrackDocument.prodDiscNr) ? mLTrackDocument.prodDiscNr
                    : "[na]");
                filename =
                  performer +
                  " - " +
                  (!string.IsNullOrEmpty(mLTrackDocument.trackTitle) ? mLTrackDocument.trackTitle : "[Untitled Track]") +
                  (!string.IsNullOrEmpty(mLTrackDocument.trackVersionTitle) ? $"_({mLTrackDocument.trackVersionTitle})" : "") +
                    (!string.IsNullOrEmpty(position) ? $"_{position}" : "");
            }
            else
            {
                string performer = mLTrackDocument.performer?.Count > 0
                    ? mLTrackDocument.performer[0] +
                      (mLTrackDocument.performer.Count > 1 ? "_etal" : "")
                    : !string.IsNullOrEmpty(mLTrackDocument.prodArtist)
                    ? mLTrackDocument.prodArtist + "_a_"
                    : "[Unknown Artist]";

                filename =
                  performer +
                  " - " +
                  (!string.IsNullOrEmpty(mLTrackDocument.trackTitle) ? mLTrackDocument.trackTitle : "[Untitled Track]") +
                   (!string.IsNullOrEmpty(mLTrackDocument.trackVersionTitle) ? $"_({mLTrackDocument.trackVersionTitle})" : "");

            }
            return ReplaceInvalidChars(filename.Substring(0, Math.Min(filename.Length, 250)));          
        }

        

    }
}
