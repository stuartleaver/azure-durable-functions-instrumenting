using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net.Http;
using Microsoft.ApplicationInsights.DataContracts;

namespace ChasseGalerie.CanoeHealthCheck
{
    public class CanoeHealthCheck
    {
        private readonly TelemetryClient telemetryClient;

        public CanoeHealthCheck(TelemetryConfiguration telemetryConfiguration)
        {
            telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        [FunctionName("CanoeHealthCheck")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "canoehealthcheck")] HttpRequestMessage req,
            [DurableClient] IDurableEntityClient client,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function to return canoe health.");

            var entityId = new EntityId(nameof(CanoeHealth), "CanoeHealth");

            await client.SignalEntityAsync(entityId, "SetStatus");

            var state = await client.ReadEntityStateAsync<CanoeHealth>(entityId);

            if (state.EntityState.Health == CanoeHealthEnum.Ok)
            {
                log.LogInformation("Canoe health is Ok.");

                return new OkObjectResult(state);
            }
            else
            {
                log.LogError("A worker has cursed!");

                // Track the Event
                var evt = new EventTelemetry("A worker has cursed!");
                telemetryClient.TrackEvent(evt);

                return new BadRequestResult();
            }
        }
    }
}
