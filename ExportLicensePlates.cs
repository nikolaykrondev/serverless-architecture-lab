using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace TollBooth
{
    public static class ExportLicensePlates
    {
        [FunctionName("ExportLicensePlates")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, ILogger log)
        {
            int exportedCount = 0;
            log.LogInformation("Finding license plate data to export");

            var databaseMethods = new DatabaseMethods(log);
            var licensePlates = databaseMethods.GetLicensePlatesToExport();
            if (licensePlates.Any())
            {
                log.LogInformation($"Retrieved {licensePlates.Count} license plates");
                var fileMethods = new FileMethods(log);
                var uploaded = await fileMethods.GenerateAndSaveCsv(licensePlates);
                if (uploaded)
                {
                    await databaseMethods.MarkLicensePlatesAsExported(licensePlates);
                    exportedCount = licensePlates.Count;
                    log.LogInformation("Finished updating the license plates");
                }
                else
                {
                    log.LogInformation(
                        "Export file could not be uploaded. Skipping database update that marks the documents as exported.");
                }

                log.LogInformation($"Exported {exportedCount} license plates");
            }
            else
            {
                log.LogWarning("No license plates to export");
            }

            if (exportedCount == 0)
            {
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"Exported {exportedCount} license plates", Encoding.UTF8, "text/plain"),
                };
            }
        }
    }
}
