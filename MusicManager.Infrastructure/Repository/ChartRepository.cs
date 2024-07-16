using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class ChartRepository : GenericRepository<chart_master_tracks>, IChartRepository
    {
        public IOptions<AppSettings> _appSettings { get; }
        public ILogger<ChartRepository> _logger { get; }

        public ChartRepository(MLContext context, IOptions<AppSettings> appSettings, 
            ILogger<ChartRepository> logger) : base(context)
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<int> InsertUpdateMasterTracksBulk(List<chart_master_tracks> chartMasterTracks)
        {
            int successCount = 0;

            string sql = @"INSERT INTO charts.chart_master_tracks(
master_id, title, artist, external_id, first_date_released, highest_date_released, first_pos, highest_pos, chart_type_id, chart_type_name, date_created, date_last_edited)
VALUES (@master_id, @title, @artist, @external_id, @first_date_released, @highest_date_released, @first_pos, @highest_pos, @chart_type_id, @chart_type_name, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
on conflict (master_id) do update 
set title=@title,artist=@artist, external_id=@external_id,
first_date_released=@first_date_released,
highest_date_released=@highest_date_released,
first_pos=@first_pos,
highest_pos=@highest_pos,
chart_type_id=@chart_type_id,
chart_type_name=@chart_type_name,
date_last_edited=CURRENT_TIMESTAMP;";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                foreach (var item in chartMasterTracks)
                {
                    successCount += await c.ExecuteAsync(sql, item);
                }
            }
            return successCount;
        }

        public async Task<int> InsertUpdateMasterAlbumBulk(List<chart_master_albums> chartMasterAlbums)
        {
            int successCount = 0;

            string sql = @"INSERT INTO charts.chart_master_albums(
master_id, title, artist, external_id, first_date_released, highest_date_released, first_pos, highest_pos, chart_type_id, chart_type_name, date_created, date_last_edited)
VALUES (@master_id, @title, @artist, @external_id, @first_date_released, @highest_date_released, @first_pos, @highest_pos, @chart_type_id, @chart_type_name, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
on conflict (master_id) do update 
set title=@title,artist=@artist, external_id=@external_id,
first_date_released=@first_date_released,
highest_date_released=@highest_date_released,
first_pos=@first_pos,
highest_pos=@highest_pos,
chart_type_id=@chart_type_id,
chart_type_name=@chart_type_name,
date_last_edited=CURRENT_TIMESTAMP;";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                foreach (var item in chartMasterAlbums)
                {
                    successCount += await c.ExecuteAsync(sql, item);
                }
            }
            return successCount;
        }

        public async Task<MasterTrackChartResponse> GetAllTrackMasterTracks(chart_sync_summary trackChartSyncSummary, string chartTypeId)
        {
            try
            {
                string _url = _appSettings.Value.ChartApiSettings.MasterTracks + "?chartId=" + chartTypeId;

                if (trackChartSyncSummary == null)
                {
                    _url += "&chartDate=&pageNo=&pageSize=";
                }
                else
                {
                    _url += $"&chartDate={trackChartSyncSummary?.check_date.ToString("yyyy-MM-dd")}&pageNo=&pageSize=";
                }

                HttpWebResponse httpResponse = await httpGetRequest(_url, null);

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(httpResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        string output = reader.ReadToEnd();
                        return JsonConvert.DeserializeObject<MasterTrackChartResponse>(output);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync Master Tracks from ChartDB | Module : {Module}","ChartDB Sync");
                return null;
            }
        }

        public async Task<MasterAlbumChartResponse> GetAllMasterAlbums(chart_sync_summary trackChartSyncSummary, string chartTypeId)
        {
            try
            {
                string _url = _appSettings.Value.ChartApiSettings.MasterAlbums + "?chartId=" + chartTypeId;

                if (trackChartSyncSummary == null)
                {
                    _url += "&chartDate=&pageNo=&pageSize=";
                }
                else
                {
                    _url += $"&chartDate={trackChartSyncSummary?.check_date.ToString("yyyy-MM-dd")}&pageNo=&pageSize=";
                }

                HttpWebResponse httpResponse = await httpGetRequest(_url, null);

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(httpResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        string output = reader.ReadToEnd();
                        return JsonConvert.DeserializeObject<MasterAlbumChartResponse>(output);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync Master Albums from ChartDB | Module : {Module}", "ChartDB Sync");
                return null;
            }
        }

        private async Task<HttpWebResponse> httpGetRequest(string url, byte[] data)
        {
            HttpWebRequest httpWebRequest;
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();

            try
            {
                httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Headers.Add(webHeaderCollection);
                httpWebRequest.Method = "GET";
                httpWebRequest.ContentType = "application/json";

                return await httpWebRequest.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException)
            {
                return null;
            }
        }

        public async Task<chart_sync_summary> GetLastChartSync(Guid chartTypeId, string type)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<chart_sync_summary>(@"SELECT cs.* FROM charts.chart_sync_summary cs WHERE cs.chart_type_id=@chart_type_id 
                                                                              AND cs.type=@type;",
                                                                            new chart_sync_summary()
                                                                            {
                                                                                chart_type_id = chartTypeId,
                                                                                type = type
                                                                            });

            }
        }

        public async Task<chart_sync_summary> InsertChartSyncSummary(chart_sync_summary chartSyncSummary)
        {
            string sql = @"INSERT INTO charts.chart_sync_summary(
	chart_type_id, type, check_date, count, date_last_edited)
	VALUES (@chart_type_id, @type, CURRENT_TIMESTAMP, @count, CURRENT_TIMESTAMP) returning *;";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleAsync<chart_sync_summary>(sql, chartSyncSummary);
            }
        }

        public async Task<IEnumerable<string>> GetDistinctTrackArtists()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<string>(@"select distinct(lower(artist)) from charts.chart_master_tracks;");
            }
        }

        public async Task<IEnumerable<string>> GetDistinctAlbumArtists()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<string>(@"select distinct(lower(artist)) from charts.chart_master_albums;");
            }
        }
    }
}
