using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Serilog;

namespace MYOB.AccountRight.SDK.Communication
{
    public class CustomLogging
    {
        private readonly string _storageConnectionString;

        public CustomLogging(string storageConnectionString)
        {
            _storageConnectionString = storageConnectionString;
        }

        public async Task LogHttp(string requestMethod, string requestUri, string requestBody, int responseCode, string responseBody, long start, long finish)
        {
            var context = Log.Logger;

            if (!string.IsNullOrEmpty(requestBody))
            {
                context = context.ForContext("RequestBody", requestBody);
            }

            if (responseBody.Length > 2000)
            {
                CloudBlobContainer containerReference = CloudStorageAccount.Parse(_storageConnectionString).CreateCloudBlobClient().GetContainerReference("myob-logs");
                CloudBlockBlob blockBlobReference = containerReference.GetBlockBlobReference(Guid.NewGuid().ToString() + ".txt");
                await blockBlobReference.UploadTextAsync(responseBody);
                context = context.ForContext("ResponseBody", blockBlobReference.Uri);
            }
            else
            {
                context = context.ForContext("ResponseBody", responseBody);
            }

            var elapsed = (finish - start) * 1000 / (double)Stopwatch.Frequency;

            context.Debug(MessageTemplate, "Myob", requestMethod, requestUri, responseCode, elapsed);
        }

        public void LogHttpFailure(string requestMethod, string requestUri, string requestBody, Exception ex, long start)
        {
            var context = Log.Logger;

            if (!string.IsNullOrEmpty(requestBody))
            {
                context = context.ForContext("RequestBody", requestBody);
            }

            var elapsed = (Stopwatch.GetTimestamp() - start) * 1000 / (double)Stopwatch.Frequency;

            context.Error(ex, MessageTemplate, "Myob", requestMethod, requestUri, 0, elapsed);
        }

        const string MessageTemplate = "Dependency {Dependency} HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    }
}
