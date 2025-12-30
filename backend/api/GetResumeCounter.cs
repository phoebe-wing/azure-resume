using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics.Metrics;
using System;
using System.Threading.Tasks;

namespace Resume.Function;

public class GetResumeCounter
{
    private readonly ILogger<GetResumeCounter> _logger;
    private readonly Container _container;

    public GetResumeCounter(ILogger<GetResumeCounter> logger)
    {
        _logger = logger;
        var connectionString = Environment.GetEnvironmentVariable("AzureResumeConnectionString");
        var cosmosClient = new CosmosClient(connectionString);
        _container = cosmosClient.GetContainer("Resume_Counter", "counter");
    }

    [Function("GetResumeCounter")]
    // [CosmosDBOutput(databaseName: "Resume_Counter", containerName: "counter", Connection="AzureResumeConnectionString", PartitionKey="1")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route=null)] HttpRequest req,
        [CosmosDBInput(databaseName: "Resume_Counter", containerName: "counter", Connection="AzureResumeConnectionString", Id="1", PartitionKey="1")] Counter counter
        )
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        counter.Count += 1;
        _logger.LogInformation($"Counter updated to {counter.Count}");

        await _container.PatchItemAsync<Counter>(
            id: counter.Id,
            partitionKey: new PartitionKey(counter.Id),
            patchOperations: new[]
            {
                PatchOperation.Replace("/count", counter.Count)
            }
        );

        return new OkObjectResult(counter);
        // return new IActionResult(System.Net.HttpStatusCode.OK)
        // {
        //     Content = new StringContent(JsonConvert.SerializeObject(counter), Encoding.UTF8, "application/json")
        // };
    }
}