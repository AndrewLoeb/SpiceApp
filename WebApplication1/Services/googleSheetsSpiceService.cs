using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// Added for this app from https://dev.to/zoltanhalasz/google-sheets-with-net-core-razor-pages-crud-tutorial-38ee
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
// Added for me
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class googleSheetsSpiceService
    {

        private readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private readonly string ApplicationName = "Spices and Herbs";
        private readonly string sheet = "Sheet2";
        private readonly string SpreadsheetId = "1D4BDJRtUyU1rvgenlYp3gpxgrRS3wclt15CSAbHy7Eo";
        private SheetsService service;
        public googleSheetsSpiceService()
        {
            Init();
        }

        private void Init()
        {
            GoogleCredential credential;
            //Reading Credentials File...
            using (var stream = new FileStream("app_client_secret.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(Scopes);
            }
            // Creating Google Sheets API service...
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public List<SpiceModel> Spices_GetList()
        {
            var myInvList = new List<SpiceModel>();
            var range = $"{sheet}!A:D";
            int j = 0;
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(SpreadsheetId, range);
            // Ecexuting Read Operation...
            var response = request.Execute();
            // Getting all records from Column A to E...
            IList<IList<object>> values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    j++;
                    if (j > 0)
                    {
                        var myInv = new SpiceModel()
                        {
                            Id = Int32.Parse(row[0].ToString()),
                            Name = row[1].ToString(),
                            Category1 = row[2].ToString(),
                            Category2 = row[3].ToString(),
                        };

                        myInvList.Add(myInv);
                    }

                }
            }
            else
            {
                Console.WriteLine("Error reading data.");
            }

            return myInvList;
        }


        public void AddInvoice(SpiceModel Spice)
        {
            // Specifying Column Range for reading...
            var range = $"{sheet}!A:D";
            var valueRange = new ValueRange();
            int mynewID = 1;
            var myInvList = Spices_GetList();
            if (myInvList.Count > 0) mynewID = myInvList.Max(x => x.Id) + 1;
            var oblist = new List<object>() { mynewID, Spice.Name, Spice.Category1, Spice.Category2};
            valueRange.Values = new List<IList<object>> { oblist };
            // Append the above record...
            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var appendReponse = appendRequest.Execute();
        }

        public void UpdateInvoice(SpiceModel Spice)
        {


            int rowID = 0;
            var range = $"{sheet}!A:D";
            int j = 0;
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = request.Execute();
            IList<IList<object>> values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var invoicerow in values)
                {
                    j++;
                    if (j > 1)
                    {
                        var Id = Int32.Parse(invoicerow[0].ToString());
                        if (Id == Spice.Id) rowID = j;
                    }

                }
            }

            var range2 = $"{sheet}!A{rowID}:D{rowID}";
            var valueRange = new ValueRange();
            var oblist = new List<object>() { Spice.Id, Spice.Name, Spice.Category1, Spice.Category2 };
            valueRange.Values = new List<IList<object>> { oblist };
            // Performing Update Operation...
            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range2);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var appendReponse = updateRequest.Execute();
        }

        public void ClearInvoiceID(int Id)
        {
            int rowID = Id + 1;
            var range = $"{sheet}!A{rowID}:D{rowID}";
            var requestBody = new ClearValuesRequest();
            var deleteRequest = service.Spreadsheets.Values.Clear(requestBody, SpreadsheetId, range);
            var deleteReponse = deleteRequest.Execute();
        }


        public void DeleteInvoice(SpiceModel inv)
        {

            int row = 0;
            var range = $"{sheet}!A:D";
            int j = 0;
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = request.Execute();
            IList<IList<object>> values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var invoicerow in values)
                {
                    j++;
                    if (j > 1)
                    {
                        var Id = Int32.Parse(invoicerow[0].ToString());
                        if (Id == inv.Id) row = j - 1;
                    }

                }
            }


            Request RequestBody = new Request()
            {
                DeleteDimension = new DeleteDimensionRequest()
                {
                    Range = new DimensionRange()
                    {
                        SheetId = 461012880,
                        Dimension = "ROWS",
                        StartIndex = row,
                        EndIndex = row + 1
                    }
                }
            };

            List<Request> RequestContainer = new List<Request>();
            RequestContainer.Add(RequestBody);

            BatchUpdateSpreadsheetRequest DeleteRequest = new BatchUpdateSpreadsheetRequest();
            DeleteRequest.Requests = RequestContainer;

            SpreadsheetsResource.BatchUpdateRequest Deletion = new SpreadsheetsResource.BatchUpdateRequest(service, DeleteRequest, SpreadsheetId);
            Deletion.Execute();

        }

    }
}