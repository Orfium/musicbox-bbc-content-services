using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MusicManager.SupportApp
{
    public class Service
    {
        private static IOptions<AppSettings> _appSettings;
        private static ICTagsRepository _cTagsRepository;
        public Service(ICTagsRepository cTagsRepository, IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
            _cTagsRepository = cTagsRepository;
        }
        public static void AddMemberLabels()
        {
            string ApplicationName = "AddMemberLabels";

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleAuthenticate(),
                ApplicationName = ApplicationName,
            });
            String sheetId = "1EVU56rVIBQzj1DCGccLv_Chc6LC1xPIAiDtTFvIC2wc";
            var ssRequest = service.Spreadsheets.Get(sheetId);
            Spreadsheet ss = ssRequest.Execute();
            List<string> sheetList = new List<string>();
            int count = 0;

            foreach (Sheet sheet in ss.Sheets)
            {
                var tabName = sheet.Properties.Title;
                List<string> ranges = new List<string>();
                ranges.Add(string.Format("{0}!A2:C", sheet.Properties.Title));
                SpreadsheetsResource.ValuesResource.BatchGetRequest request = service.Spreadsheets.Values.BatchGet(sheetId);
                request.Ranges = ranges;

                BatchGetValuesResponse response = request.Execute();

                if (response.ValueRanges != null && response.ValueRanges.Count > 0)
                {
                    foreach (var values in response.ValueRanges)
                    {
                        if (values.Values != null)
                        {
                            foreach (var row in values.Values)
                            {
                                member_label ml = new member_label();
                                if (row.Count == 3)
                                {
                                    ml.label = row[1].ToString();
                                    ml.member = row[0].ToString();
                                    ml.date_created = DateTime.Now;
                                    ml.source = tabName;
                                    ml.mlc = row[2].ToString();
                                }
                                else if (row.Count == 2)
                                {
                                    ml.label = row[1].ToString();
                                    ml.member = row[0].ToString();
                                    ml.date_created = DateTime.Now;
                                    ml.source = tabName;
                                }
                                else
                                {
                                    ml.member = row[0].ToString();
                                    ml.date_created = DateTime.Now;
                                    ml.source = tabName;
                                }


                                _cTagsRepository.AddMemberLabelData(ml);
                                Console.WriteLine("Processing: " + count);
                                count++;
                            }
                        }
                    }
                }
            }

        }
        private static UserCredential GoogleAuthenticate()
        {
            string[] Scopes = { SheetsService.Scope.Spreadsheets };

            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);


                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            return credential;
        }
        public static void AddPriorApprovals()
        {
            string ApplicationName = "AddPriorApprovals";

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleAuthenticate(),
                ApplicationName = ApplicationName,
            });
            String sheetId = "1pwb8Ou1noqaOD7D4NAL-JTZG-iB7nu3jrR5Zi6XnNEk";
            var ssRequest = service.Spreadsheets.Get(sheetId);
            Spreadsheet ss = ssRequest.Execute();
            List<string> sheetList = new List<string>();
            int count = 0;

            foreach (Sheet sheet in ss.Sheets)
            {

                var tabName = sheet.Properties.Title;
                List<string> ranges = new List<string>();
                ranges.Add(string.Format("{0}!A2:J", sheet.Properties.Title));
                SpreadsheetsResource.ValuesResource.BatchGetRequest request = service.Spreadsheets.Values.BatchGet(sheetId);
                request.Ranges = ranges;

                BatchGetValuesResponse response = request.Execute();

                if (response.ValueRanges != null && response.ValueRanges.Count > 0)
                {
                    foreach (var values in response.ValueRanges)
                    {
                        if (values.Values != null)
                        {
                            foreach (var row in values.Values)
                            {
                                prior_approval_work paw = new prior_approval_work
                                {
                                    date_created = DateTime.Now,
                                    created_by = 89,
                                    ice_mapping_code = row[0].ToString(),
                                    local_work_id = row[1].ToString(),
                                    tunecode = row[2].ToString(),
                                    iswc = row[3].ToString(),
                                    work_title = row[4].ToString(),
                                    composers = row[5].ToString(),
                                    publisher = row[6].ToString(),
                                    date_last_edited = DateTime.Now,
                                    last_edited_by = 89,
                                    matched_dh_ids = null,
                                    matched_isrc = null,
                                    artist = row[7].ToString(),
                                    broadcaster = sheet.Properties.Title,
                                    writers = row[8].ToString(),
                                };

                                _cTagsRepository.AddPriorApprovalWork(paw);
                                Console.WriteLine("Processing: " + count);
                                count++;
                            }
                        }
                    }
                }
            }


        }


    }
}
