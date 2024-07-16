using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicManager.Playout.Metadata
{
    public sealed class Name
    {
        public string FullName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public NamePart[] Parts { get; set; }

        // used by JSON.NET
        public bool ShouldShouldSerializeParts()
        {
            return Parts?.Length > 0;
        }

        public Name()
        {
            FullName = String.Empty;
            Parts = null;
        }

        public Name(string fullName)
        {
            FullName = fullName;
            Parts = null;
        }

        public Name(Soundmouse.Messaging.Model.InterestedParty ip)
        {
            FullName = ip.FullName;
            Parts = ip.NameParts == null ? null : ip.NameParts.Select(p => new NamePart(p)).ToArray();
        }

        public static Name MakeWithFirstNameAndLastName(string firstName, string lastName)
        {
            return new Name()
            {
                FullName = firstName + " " + lastName,
                Parts = new[]
                {
                    new NamePart(NamePart.Types.FirstName, firstName),
                    new NamePart(NamePart.Types.LastName, lastName)
                }
            };
        }

        public Name Clone()
        {
            return new Name()
            {
                FullName = FullName,
                Parts = Parts == null ? null : Parts.Select(p => p.Clone()).ToArray()
            };
        }

        public string GetFirstName()
        {
            return Parts == null ? String.Empty : String.Join(" ",
                Parts.Where(p => p.Type == "first_name").Select(p => p.Value));
        }

        public string GetLastName()
        {
            return Parts == null ? String.Empty : String.Join(" ",
                Parts.Where(p => p.Type == "last_name").Select(p => p.Value));
        }

        public void Trim()
        {
            FullName = FullName.Trim();
            if (Parts != null)
            {
                Parts = Parts.Select(p => new NamePart(p.Type, p.Value.Trim())).ToArray();
            }
        }

        public override string ToString()
        {
            return FullName;
        }

        public static bool IsNullOrWhiteSpace(Name name)
        {
            return name == null || String.IsNullOrWhiteSpace(name.FullName);
        }

        public static implicit operator Name(string str)
        {
            return new Name(str);
        }

        public bool TryExtractNameParts(out NamePart[] parts, bool tryParseFullName = false)
        {
            var possiblePatterns = new[]
            {
                // LastName,FirstName
                new Regex(@"^(?'lastName'\p{L}+),\s*(?'firstName'\p{L}+)$", RegexOptions.Compiled),

                // FirstName LastName
                new Regex(@"^(?'firstName'\p{L}+)\s+(?'lastName'\p{L}+)$", RegexOptions.Compiled)
            };

            if (Parts != null)
            {
                parts = Parts;
                return true;
            }

            if (tryParseFullName && !String.IsNullOrWhiteSpace(FullName))
            {
                foreach (var pattern in possiblePatterns)
                {
                    var match = pattern.Match(FullName);
                    if (match.Success)
                    {
                        parts = new[]
                        {
                            new NamePart(NamePart.Types.FirstName, match.Groups["firstName"].Value),
                            new NamePart(NamePart.Types.LastName, match.Groups["lastName"].Value)
                        };

                        return true;
                    }
                }
            }

            parts = null;
            return false;
        }

        public sealed class NamePart
        {
            public string Type { get; set; }
            public string Value { get; set; }

            public NamePart()
            {
            }

            public NamePart(string type, string value)
            {
                Type = type;
                Value = value;
            }

            public NamePart(Soundmouse.Messaging.Model.Name src)
            {
                Type = GetNamePartType(src.NamePart.Value);
                Value = src.Value;
            }

            public NamePart Clone()
            {
                return new NamePart()
                {
                    Type = Type,
                    Value = Value
                };
            }

            public override string ToString()
            {
                return Type + ": " + Value;
            }

            public static string GetNamePartType(Soundmouse.Messaging.Model.NamePart namePart)
            {
                switch (namePart)
                {
                    case Soundmouse.Messaging.Model.NamePart.First:
                        return Types.FirstName;
                    case Soundmouse.Messaging.Model.NamePart.Last:
                        return Types.LastName;
                    case Soundmouse.Messaging.Model.NamePart.Middle:
                        return Types.MiddleName;
                    case Soundmouse.Messaging.Model.NamePart.Unknown:
                    default:
                        return Types.Unknown;
                }
            }

            public static class Types
            {
                public const string FirstName = "first_name";
                public const string MiddleName = "middle_name";
                public const string LastName = "last_name";
                public const string Unknown = "unknown";
            }
        }
    }
}
