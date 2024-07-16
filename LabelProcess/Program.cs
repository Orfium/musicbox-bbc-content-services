using Elasticsearch;
using LabelProcess.Logics;
using LabelProcess.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using MusicManager.Infrastructure;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LabelProcess
{
    class Program
    {
        public static string folderPath = "";       

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            BuildConfig(builder);
            var configuration = builder.Build();

            IHost host = ConfigureService(configuration);

            Service svc = ActivatorUtilities.CreateInstance<Service>(host.Services);

            //string buildId = Console.ReadLine();

            //await svc.PublishPlayout(new Guid(buildId));


            folderPath = Console.ReadLine();
            //List<member_label> list = await ReadPPLCSV(folderPath);
            //await svc.ImportPPLList(list);  

            await ReadProblematicISRCsCSV(folderPath, svc);
            //List<string> incorrectList = svc.CheckIncorrectTunecode(list);
            

            Console.WriteLine("Done");

            Console.ReadLine();
        }

        static async Task<List<string>> ReadCSV(string path)
        {
            List<string> list = new List<string>();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (line.Contains("|~|"))
                    {
                        string[] multipleLable = line.Split("|~|");
                        foreach (var item in multipleLable)
                        {
                            list.Add(item.TrimStart('"').TrimEnd('"'));
                        }
                    }
                    else if(line.Length > 1) {
                        list.Add(line.Trim().TrimStart('"').TrimEnd('"').Replace("\t", "").Trim());
                    }                    
                }
            }
            return list.Distinct().ToList();
        }

        static async Task ReadProblematicISRCsCSV(string path, Service svc)
        {
            List<ProblematicISRCsMatchedClearanceTracks> matchResults = new List<ProblematicISRCsMatchedClearanceTracks>();
            var ctags = await svc.GetAllActiveCtags();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    string[] lineItems = line.Split(',');

                    MLTrackDocument mLTrackDocument = await svc.GetElasticTrackById(lineItems[11]);

                    ProblematicISRCsMatchedClearanceTracks problematicISRCsMatchedClearanceTracks = new ProblematicISRCsMatchedClearanceTracks()
                    {
                        ClearancFormTrackID = lineItems[0],
                        ClearancFormID = lineItems[1],
                        ClearanceFormRefNo = lineItems[3],
                        ClearanceFormDeadline = lineItems[4],
                        ClearanceStatus = int.Parse(lineItems[5]),
                        ISRC = lineItems[6],
                        DHTrackID = lineItems[7],
                        PRSWorkTunecode = lineItems[8],
                        PRSSearchDatetime = lineItems[9],
                        DHTuneCode = lineItems[10],
                        MLID = lineItems[11],
                        WSID = lineItems[12],
                        MLComposer = mLTrackDocument.composer!=null ? string.Join(", ", mLTrackDocument.composer) : "",
                        MLPublisher = mLTrackDocument.publisher != null ? string.Join(", ", mLTrackDocument.publisher) : "",
                        MLPerformer = mLTrackDocument.performer != null ? string.Join(", ", mLTrackDocument.performer) : "",
                        MLTrackTitle = mLTrackDocument.trackTitle,
                        PRSWorkTitle = mLTrackDocument.prsWorkTitle,
                        PRSWorkPublishers = mLTrackDocument.prsWorkPublishers != null ? string.Join(", ", mLTrackDocument.prsWorkPublishers) : "",
                        PRSWorkWritors = mLTrackDocument.prsWorkWriters != null ? string.Join(", ", mLTrackDocument.prsWorkWriters) : "",
                    };

                    //if (!string.IsNullOrEmpty(problematicISRCsMatchedClearanceTracks.PRSWorkTunecode)) {
                    //    ClearanceCTags clearanceCTags = svc.PRSSearchByTrackId(problematicISRCsMatchedClearanceTracks.PRSWorkTunecode);
                    //    if (clearanceCTags != null) {
                    //        problematicISRCsMatchedClearanceTracks.OldPRSWorkTitle = clearanceCTags.workTitle;
                    //        problematicISRCsMatchedClearanceTracks.OldPRSWorkWritors = clearanceCTags.workWriters;
                    //        problematicISRCsMatchedClearanceTracks.OldPRSWorkPublishers = clearanceCTags.workPublishers;
                    //    }
                    //}

                    if (!string.IsNullOrEmpty(lineItems[2]))
                        problematicISRCsMatchedClearanceTracks.ClearanceFormAllocatedUser = int.Parse(lineItems[2]);

                    if (!string.IsNullOrEmpty(problematicISRCsMatchedClearanceTracks.PRSWorkTunecode) &&
                        !string.IsNullOrEmpty(problematicISRCsMatchedClearanceTracks.DHTuneCode) &&
                        problematicISRCsMatchedClearanceTracks.PRSWorkTunecode == problematicISRCsMatchedClearanceTracks.DHTuneCode)
                    {
                        problematicISRCsMatchedClearanceTracks.MatchingType = "Correct match - Tunecode exsist";
                        problematicISRCsMatchedClearanceTracks.NewPRSWorkTunecode = problematicISRCsMatchedClearanceTracks.PRSWorkTunecode;
                    }
                    else {
                        ProblematicISRCsMatchedClearanceTracks prevMatch = matchResults.FirstOrDefault(a => a.ISRC == problematicISRCsMatchedClearanceTracks.ISRC);

                        if (prevMatch != null)
                        {
                            problematicISRCsMatchedClearanceTracks.MatchingType = prevMatch.MatchingType;
                            problematicISRCsMatchedClearanceTracks.NewPRSWorkTunecode = prevMatch.NewPRSWorkTunecode;
                            problematicISRCsMatchedClearanceTracks.NewPRSWorkTitle = prevMatch.NewPRSWorkTitle;
                            problematicISRCsMatchedClearanceTracks.NewPRSWorkPublishers = prevMatch.NewPRSWorkPublishers;
                            problematicISRCsMatchedClearanceTracks.NewPRSWorkWritors = prevMatch.NewPRSWorkWritors;
                        }
                        else {
                            ClearanceCTags clearanceCTags = await svc.PRSSearchByTrackId(ctags, problematicISRCsMatchedClearanceTracks.MLID);
                            if (!string.IsNullOrEmpty(clearanceCTags.workTunecode))
                            {
                                problematicISRCsMatchedClearanceTracks.NewPRSWorkTitle = clearanceCTags.workTitle;
                                problematicISRCsMatchedClearanceTracks.NewPRSWorkPublishers = clearanceCTags.workPublishers;
                                problematicISRCsMatchedClearanceTracks.NewPRSWorkWritors = clearanceCTags.workWriters;
                                problematicISRCsMatchedClearanceTracks.MatchingType = "PRS found";
                                problematicISRCsMatchedClearanceTracks.NewPRSWorkTunecode = clearanceCTags.workTunecode;
                            }
                            else {
                                problematicISRCsMatchedClearanceTracks.MatchingType = "PRS not found";                               
                            }
                        }
                    }

                    Console.WriteLine(problematicISRCsMatchedClearanceTracks.MatchingType);

                    matchResults.Add(problematicISRCsMatchedClearanceTracks);
                }
            }
            await PrepaireProblematicISRCsMatchedClearanceTracksCSV(matchResults);
        }

        static async Task<List<member_label>> ReadPPLCSV(string path)
        {
            List<member_label> list = new List<member_label>();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    string[] multipleLable = line.Split("\t");
                    list.Add(new member_label() { 
                        member = multipleLable[0].Trim(),
                        label = multipleLable[1].Trim(),
                        source = "Manual"
                    });
                }
            }
            return list;
        }

        static async Task PrepaireProblematicISRCsMatchedClearanceTracksCSV(List<ProblematicISRCsMatchedClearanceTracks> matchResults)
        {
            string fileName = "Problematic_ISRCs_Matched_Clearance_Tracks_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".csv";

            //Create the Header of the CSV File
            StringBuilder sHeader = new StringBuilder().AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}\t{20}\t{21}\t{22}\t{23}\t{24}",
                        "Clearanc Form Track ID", "Clearance Form ID", "Clearance Form Allocated User", "Clearance Form Ref No",
                        "Clearance Form Deadline", "Clearance Status", "ISRC", "DH Track ID","DH Title",
                        "DH Composer", "DH Publisher", "DH Performer", "Current PRS Work Tunecode", "Current PRS Work Title",
                        "Current PRS Work Writers", "Current PRS Work Publishers","Current PRS Search Datetime",
                        "DH Tunecode", "ML ID", "WS ID", "New Matching TuneCode","New PRS Title", "New PRS Publishers", "New PRS Writers", "Matching Type"));
            await GenerateCSVLine(sHeader, Path.GetDirectoryName(folderPath), fileName);

            foreach (var match in matchResults)
            {
                //Create the Body of the CSV File
                StringBuilder sBody = new StringBuilder().AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}\t{20}\t{21}\t{22}\t{23}\t{24}",                
                            match.ClearancFormTrackID, match.ClearancFormID, match.ClearanceFormAllocatedUser, match.ClearanceFormRefNo, match.ClearanceFormDeadline,
                            match.ClearanceStatus, match.ISRC, match.DHTrackID, match.MLTrackTitle, match.MLComposer, match.MLPublisher, match.MLPerformer,
                            match.PRSWorkTunecode, match.PRSWorkTitle, match.PRSWorkWritors, match.PRSWorkPublishers, match.PRSSearchDatetime,
                            match.DHTuneCode, match.MLID, match.WSID, match.NewPRSWorkTunecode, match.NewPRSWorkTitle, match.NewPRSWorkPublishers, 
                            match.NewPRSWorkWritors, match.MatchingType));
                await GenerateCSVLine(sBody, Path.GetDirectoryName(folderPath), fileName);
            }
        }

        static async Task PrepaireCSV(List<MatchResult> matchResults) {
            string fileName = "Report_output_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".csv";

            //Create the Header of the CSV File
            StringBuilder sHeader = new StringBuilder().AppendLine(string.Format("\"{0}\"\t{1}\t{2}\t{3}\t{4}",
                        "Label", "Exact Match", "Contains Match", "Chart Track Count",
                        "Total Track Count"));
            await GenerateCSVLine(sHeader, Path.GetDirectoryName(folderPath), fileName);

            foreach (var match in matchResults)
            {
                //Create the Body of the CSV File
                StringBuilder sBody = new StringBuilder().AppendLine(string.Format("\"{0}\"\t{1}\t{2}\t{3}\t{4}",
                            match.label,match.isExactMatch,match.isContainsMatch,match.chartTrackCount,match.totalTrackCount));
                await GenerateCSVLine(sBody, Path.GetDirectoryName(folderPath), fileName);
            }
        }

        static async Task GenerateCSVLine(StringBuilder sbuilder, string filePath, string fileName)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    filePath = filePath.Replace('\\', '/');
                }

                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (StreamWriter writer = new StreamWriter(Path.Combine(filePath ,fileName), true))
                {
                   await  writer.WriteAsync(sbuilder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static IHost ConfigureService(IConfigurationRoot configuration)
        {
            var logsEnvironment = configuration.GetSection("AppSettings:AppEnvironment").Value ?? "dev";         

            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddMemoryCache();

                    services.AddDbContextPool<MLContext>(_builder =>
                    _builder.UseNpgsql(configuration.GetSection("AppSettings:NpgConnection").Value));

                    services.AddElasticsearch(configuration);

                    services.AddAwsServices();                  

                    services.Configure<AppSettings>(x => configuration.GetSection("AppSettings").Bind(x));

                    services.AddInfrastructure();

                })                
                .Build();
        }

        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();
        }
    }
}
