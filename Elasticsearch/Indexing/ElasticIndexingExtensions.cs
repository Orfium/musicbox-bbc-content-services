using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elasticsearch.Indexing
{
    public static class ElasticIndexingExtensions
    {
        /// <summary>
        /// Bulk index/delete track documents.
        /// </summary>
        /// <returns>IDs of the tracks which failed to index.</returns>
        public static (string error, Guid id, string reason)[] BulkIndexTracks(this ElasticClient client,
            List<MLTrackDocument> tracks,string index)
        {

            IEnumerable<BulkResponseItemBase> errors = null;

            var response = client.Bulk(b => b
                    .Index(index) //track-ml-test
                    .IndexMany(tracks, (d, t) => d.Id(t.id))
                );

            errors = response.ItemsWithErrors;

            return errors
                .Select(i => (i.Error.Type, new Guid(i.Id), i.Error.Reason))
                .ToArray();
        }

        public static (string error, Guid id, string reason)[] BulkDeleteTracks(this ElasticClient client,
            List<MLTrackDocument> tracks, string index)
        {
            IEnumerable<BulkResponseItemBase> errors = null;

            var response = client.Bulk(b => b
                    .Index(index) //track-ml-test
                    .DeleteMany(tracks, (d, t) => d.Id(t.id))
                );

            //var response = client.DeleteMany(tracks);

            errors = response.ItemsWithErrors;

            return errors
                .Select(i => (i.Error.Type, new Guid(i.Id), i.Error.Reason))
                .ToArray();
        }

        public static (string error, Guid id, string reason)[] BulkIndexAlbums(this ElasticClient client,
            List<MLAlbumDocument> albums, string index)
        {
            IEnumerable<BulkResponseItemBase> errors = null;

            var response = client.Bulk(b => b
                    .Index(index) //track-ml-test
                    .IndexMany(albums, (d, t) => d.Id(t.id))
                );

            errors = response.ItemsWithErrors;

            return errors
                .Select(i => (i.Error.Type, new Guid(i.Id), i.Error.Reason))
                .ToArray();
        }

        //public static (string error, Guid id, string reason)[] BulkIndexLibraries(this ElasticClient client,
        //    List<MLLibraryDocument> libraries, string index)
        //{
        //    IEnumerable<BulkResponseItemBase> errors = null;

        //    var response = client.Bulk(b => b
        //            .Index(index) //track-ml-test
        //            .IndexMany(libraries, (d, t) => d.Id(t.id))
        //        );

        //    errors = response.ItemsWithErrors;

        //    return errors
        //        .Select(i => (i.Error.Type, new Guid(i.Id), i.Error.Reason))
        //        .ToArray();
        //}
    }
}
