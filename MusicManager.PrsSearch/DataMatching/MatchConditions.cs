using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.PrsSearch.DataMatching
{
    public class MatchConditions
    {

        public static readonly MatchConditions PRS = new MatchConditions
        {
            AttributeWeights = new Dictionary<MatchAttribute, float>
            {
                {MatchAttribute.Performer, 5},
                {MatchAttribute.Title, 5},
                {MatchAttribute.Isrc, 4}
            },
            Conditions = new[]
            {
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, .8f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Isrc, 1f}},
                new Dictionary<MatchAttribute, float>
                {
                    {MatchAttribute.Performer, .8f},
                    {MatchAttribute.Title, .7f}  // ---- Previous 8f                 
                }
            }
        };

        //public static readonly MatchConditions PRS = new MatchConditions
        //{
        //    AttributeWeights = new Dictionary<MatchAttribute, float>
        //    {
        //        {MatchAttribute.Performer, 5},
        //        {MatchAttribute.Title, 5},
        //        {MatchAttribute.Composer, 5},
        //        {MatchAttribute.Isrc, 4},
        //        {MatchAttribute.CatNo, 4}
        //    },
        //    Conditions = new[]
        //    {
        //        new Dictionary<MatchAttribute, float> {{MatchAttribute.Tunecode, 1f}}, // OR
        //        new Dictionary<MatchAttribute, float>
        //        {
        //            {MatchAttribute.Isrc, 1f},
        //            {MatchAttribute.Title, .8f},
        //            {MatchAttribute.CatNo, .1f}
        //        },
        //        new Dictionary<MatchAttribute, float>
        //        {
        //            {MatchAttribute.Isrc, 1f},
        //            {MatchAttribute.ProductTitle, 1f},
        //            {MatchAttribute.Title, .8f}
        //        },
        //        new Dictionary<MatchAttribute, float>
        //        {
        //             {MatchAttribute.CatNo, 1f},
        //            {MatchAttribute.Performer, .8f},
        //              {MatchAttribute.Title, .8f}
        //        },
        //        new Dictionary<MatchAttribute, float>
        //        {
        //            {MatchAttribute.ProductTitle, 1f},
        //            {MatchAttribute.Performer, .8f},
        //            {MatchAttribute.Title, .8f}

        //        },
        //        new Dictionary<MatchAttribute, float>
        //        {
        //             {MatchAttribute.CatNo, 1f},
        //            {MatchAttribute.Composer, .8f},
        //            {MatchAttribute.Title, .8f}
        //        }   ,
        //        new Dictionary<MatchAttribute, float>
        //        {
        //            {MatchAttribute.ProductTitle, 1f},
        //            {MatchAttribute.Composer, .8f},
        //            {MatchAttribute.Title, .8f}
        //        }
        //    }
        //};

        public static readonly MatchConditions Gema = new MatchConditions
        {
            AttributeWeights = new Dictionary<MatchAttribute, float>
            {
                {MatchAttribute.Composer, 7},
                {MatchAttribute.Isrc, 4},
                {MatchAttribute.Iswc, 4},
                {MatchAttribute.Title, 5}
            },
            Conditions = new[]
            {
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, .8f}, {MatchAttribute.Isrc, 1f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, .8f}, {MatchAttribute.Iswc, 1f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Isrc, 1f}, {MatchAttribute.Iswc, 1f}},
                new Dictionary<MatchAttribute, float>
                {
                    {MatchAttribute.Isrc, 1f},
                    {MatchAttribute.Composer, .8f},
                    {MatchAttribute.Title, .5f}
                }
            }
        };

        public static readonly MatchConditions MatchApi = new MatchConditions
        {
            AttributeWeights = new Dictionary<MatchAttribute, float>
            {
                {MatchAttribute.CatNo, 5},
                {MatchAttribute.Composer, 5},
                {MatchAttribute.Isrc, 7},
                {MatchAttribute.Iswc, 7},
                {MatchAttribute.Label, 5},
                {MatchAttribute.Performer, 5},
                {MatchAttribute.ProductTitle, 3},
                {MatchAttribute.Title, 5},
                {MatchAttribute.Tunecode, 7}
            },
            Conditions = new[]
            {
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Tunecode, 1f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Isrc, 1f}, {MatchAttribute.CatNo, 1f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Isrc, 1f}, {MatchAttribute.ProductTitle, 1f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, 1f}, {MatchAttribute.CatNo, 1f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, .8f }, {MatchAttribute.Composer, .8f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, .8f}, {MatchAttribute.Performer, .8f}}
            }
        };

        public static readonly MatchConditions Minimal = new MatchConditions
        {
            AttributeWeights = new Dictionary<MatchAttribute, float>
            {
                {MatchAttribute.Composer, 7},
                {MatchAttribute.Isrc, 4},
                {MatchAttribute.Iswc, 4},
                {MatchAttribute.Performer, 5},
                {MatchAttribute.Title, 5}
            },
            Conditions = new[]
            {
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Isrc, 1f}, {MatchAttribute.Title, .8f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Iswc, 1f}, {MatchAttribute.Title, .8f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Composer, 1f}, {MatchAttribute.Title, .8f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Performer, 1f}, {MatchAttribute.Title, .8f}}
            }
        };

        public static readonly MatchConditions Sky = new MatchConditions
        {
            AttributeWeights = new Dictionary<MatchAttribute, float>
            {
                {MatchAttribute.CatNo, 8},
                {MatchAttribute.Composer, 7},
                {MatchAttribute.Isrc, 5},
                {MatchAttribute.Iswc, 3},
                {MatchAttribute.Label, 5},
                {MatchAttribute.Performer, 5},
                {MatchAttribute.ProductTitle, 5},
                {MatchAttribute.Title, 6},
                {MatchAttribute.Tunecode, 8}
            },
            Conditions = new[]
            {
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Tunecode, 1f}}, // tunecode
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Isrc, 1f}, {MatchAttribute.CatNo, 1f}}, // match 1
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Isrc, 1f}, {MatchAttribute.ProductTitle, 1f}}, // match 1
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, 1f}, {MatchAttribute.Performer, 1f}}, // match 3
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, 1f}, {MatchAttribute.CatNo, 1f}}, // match 4
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, 1f}, {MatchAttribute.Composer, 1f}} // match 5
            },
            ProductMustMatch = true
        };

        public static readonly MatchConditions Jasrac = new MatchConditions
        {
            AttributeWeights = new Dictionary<MatchAttribute, float>(),
            Conditions = new[]
            {
                new Dictionary<MatchAttribute, float> {{MatchAttribute.FileNameExtSysRef, 1f}}
            }
        };

        public static readonly MatchConditions Samro = new MatchConditions
        {
            AttributeWeights = new Dictionary<MatchAttribute, float>(),
            Conditions = new[]
            {
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Iswc, 1f}, {MatchAttribute.Title, 0.8f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Iswc, 1f}},
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, 1f}}
            }
        };

        public static readonly MatchConditions Spotify = new MatchConditions
        {
            AttributeWeights = new Dictionary<MatchAttribute, float>(),
            Conditions = new[]
            {
                new Dictionary<MatchAttribute, float> {{MatchAttribute.Title, 0.8f}, {MatchAttribute.Performer, 0.8f}},
            }
        };


        public Dictionary<MatchAttribute, float> AttributeWeights { get; set; }

        public ICollection<string> ComposerRoles { get; set; }

        public Dictionary<MatchAttribute, float>[] Conditions { get; set; }

        /// <summary>
        /// Workspace whitelist. Matches must come from a workspace in this list. If the array is
        /// null or empty, matches can come from any workspace.
        /// </summary>
        public Guid[] IncludeWorkspaces { get; set; }

        /// <summary>
        /// True if the product must match on cat no / title, even when not part of the match condition.
        /// </summary>
        public bool ProductMustMatch { get; set; }

    }
}