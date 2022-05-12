using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TollBooth.Models;

namespace TollBooth
{
    internal class FileMethods
    {
        private readonly CloudBlobClient _blobClient;
        private readonly string _containerName = Environment.GetEnvironmentVariable("exportCsvContainerName");
        private readonly string _blobStorageConnection = Environment.GetEnvironmentVariable("dataLakeConnection");
        private readonly ILogger _log;

        public FileMethods(ILogger log)
        {
            _log = log;
            // Retrieve data lake storage account information from connection string.
            var storageAccount = CloudStorageAccount.Parse(_blobStorageConnection);

            // Create a blob client for interacting with the data lake account.
            _blobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task<bool> GenerateAndSaveCsv(IEnumerable<LicensePlateDataDocument> licensePlates)
        {
            var successful = false;

            _log.LogInformation("Generating CSV file");
            string blobName = $"{DateTime.UtcNow:s}.csv";

            using (var stream = new MemoryStream())
            {
                using (var textWriter = new StreamWriter(stream))
                using (var csv = new CsvWriter(textWriter, CultureInfo.InvariantCulture, false))
                {
                    csv.WriteRecords(licensePlates.Select(ToLicensePlateData));
                    await textWriter.FlushAsync();

                    _log.LogInformation($"Beginning file upload: {blobName}");
                    try
                    {
                        var container = _blobClient.GetContainerReference(_containerName);

                        // Retrieve reference to a blob.
                        var blob = container.GetBlockBlobReference(blobName);
                        await container.CreateIfNotExistsAsync();

                        // Upload blob.
                        stream.Position = 0;
                        // TODO 7: Asynchronously upload the blob from the memory stream.
                        // COMPLETE: await blob...;

                        successful = true;
                    }
                    catch (Exception e)
                    {
                        _log.LogCritical($"Could not upload CSV file: {e.Message}", e);
                        successful = false;
                    }
                }
            }

            return successful;
        }

        /// <summary>
        /// Used for mapping from a LicensePlateDataDocument object to a LicensePlateData object.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static LicensePlateData ToLicensePlateData(LicensePlateDataDocument source)
        {
            return new LicensePlateData
            {
                FileName = source.fileName,
                LicensePlateText = source.licensePlateText,
                TimeStamp = source.Timestamp
            };
        }
    }
}
