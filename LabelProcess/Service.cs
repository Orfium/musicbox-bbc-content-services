using Microsoft.EntityFrameworkCore.Metadata;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Logics.ServiceLogics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MusicManager.Logics.Helper;
using Microsoft.Extensions.Options;
using MusicManager.Core.ViewModules;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using MusicManager.Logics.Logics;

namespace LabelProcess
{
    public class Service
    {
        private readonly IElasticLogic _elasticLogic;
        private readonly IUnitOfWork _unitOfWork;       
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<Service> _logger;
        private readonly ICtagLogic _ctagLogic;

        public Service(IElasticLogic elasticLogic,
            IUnitOfWork unitOfWork, 
            IOptions<AppSettings> appSettings,
            ILogger<Service> logger,
            ICtagLogic ctagLogic)
        {
            _elasticLogic = elasticLogic;
            _unitOfWork = unitOfWork;           
            _appSettings = appSettings;
            _logger = logger;
            _ctagLogic = ctagLogic;
        }

        //public async Task PublishPlayout(Guid buildId)
        //{
        //   // playout_session playout_Session = await _unitOfWork.PlayOut.GetPlayoutSessionByBuildId(buildId);

        //    IEnumerable<playout_session> sessions = await _unitOfWork.PlayOut.GetPendingPlayoutsessions();

        //    foreach (playout_session item in sessions)
        //    {
        //        var jsonString = item.request_json;
        //        var body = Encoding.UTF8.GetBytes(jsonString);

        //        var props = _model.CreateBasicProperties();
        //        props.Headers = new Dictionary<string, object>();
        //        props.Headers.Add("sent", CommonHelper.GetCurrentUtcEpochTime());

        //        _model.BasicPublish("", _appSettings.Value.RabbitMQConfiguration.RequestQueue, props, body);

        //        _logger.LogInformation("Playout Publish ({@buildId}) >  {@request}", buildId, jsonString);

        //        await Task.Delay(TimeSpan.FromSeconds(1));
        //    }
        //}

        public List<string> CheckIncorrectTunecode(List<string> list)
        {
            List<string> incorrectList = new List<string>();

            Regex regex = new Regex(@"^[0-9]+[a-z]{1,2}$", RegexOptions.IgnoreCase);

            foreach (var item in list)
            {
                if (item.Length > 8 || !regex.IsMatch(item.Trim()))
                {
                    //if (!item.Contains(','))
                        Console.WriteLine(item);

                    incorrectList.Add(item);
                }
            }
            return incorrectList;
        }
        public async Task<List<MatchResult>> Process(List<string> list)
        {          

            List<MatchResult> MatchResultList = new List<MatchResult>();
            int i = 0;

            foreach (var item in list)
            {
                i++;
                MatchResult matchResult = new MatchResult() { 
                    label = item
                };

                matchResult.isContainsMatch = await _unitOfWork.CTags.CheckPPLLabelContains(item);
                //if (matchResult.isContainsMatch) {
                //    matchResult.isExactMatch = await _unitOfWork.CTags.CheckPPLLabelExact(item);
                //}

                matchResult.totalTrackCount = await _elasticLogic.GetTrackCountByQuery($"musicOrigin:commercial AND recordLabel:\"{item.Replace("\"", "\"")}\"");
                //matchResult.chartTrackCount = await _elasticLogic.GetTrackCountByQuery($"charted:true AND musicOrigin:commercial AND recordLabel:\"{item.Replace("\"", "\"")}\"");

                MatchResultList.Add(matchResult);

                Console.WriteLine($"{list.Count} / {i}");
            }

            return MatchResultList;
        }

        public async Task ImportPPLList(List<member_label> list)
        {            
            int i = 0;
            foreach (var item in list)
            {
                i++;
                await _unitOfWork.MemberLabel.InsertManualLabel(item);   
                Console.WriteLine($"{list.Count} / {i}");
            }           
        }

        public async Task<List<c_tag>> GetAllActiveCtags()
        { 
            return await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;
        }

        public async Task<MLTrackDocument> GetElasticTrackById(string trackId)
        {
           return await _elasticLogic.GetElasticTrackDocById(Guid.Parse(trackId));          
        }

        public async Task<ClearanceCTags> PRSSearchByTrackId(List<c_tag> c_Tags, string trackId)
        {
            var mLTrackDocument = await _elasticLogic.GetElasticTrackDocById(Guid.Parse(trackId));
            return null;
        }
        public ClearanceCTags PRSSearchByTrackId(string tunecode)
        {
            return _ctagLogic.GetPRSWorkDetailsByTunecode(tunecode);           
        }
    }

    public class MatchResult
    {
        public string label { get; set; }
        public bool isExactMatch { get; set; }
        public bool isContainsMatch { get; set; }
        public double chartTrackCount { get; set; }
        public double totalTrackCount { get; set; }
    }
}
