using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using RvmsModels;
using User = RvmsModels.User;

namespace VitalsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VitalsController : ControllerBase
{
    private readonly ILogger<VitalsController> _logger;

    private IConfiguration _configuration { get; }

    private CosmosClient _cosmosClient;

    private Database _database;

    private string _databaseName;

    private Container _container;

    private string _containerName;

    private string _partitionKey;

    public VitalsController(ILogger<VitalsController> logger, IConfiguration configuration, CosmosClient cosmosClient)
    {
        _logger = logger;
        _configuration = configuration;
        _cosmosClient = cosmosClient;

        _databaseName = _configuration["CosmosDb:DatabaseName"];
        _database = _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName).Result;

        _partitionKey = _configuration["CosmosDb:PartitionKey"];

        _containerName = _configuration["CosmosDb:ContainerName"];
        _container = _database.CreateContainerIfNotExistsAsync(_containerName, _partitionKey)
            .Result;
    }

    /// <summary>
    /// Get User Vitals
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpGet("{userId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Vitals))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Get(string userId)
    {
        try
        {
            User user = await _container.ReadItemAsync<User>(userId, new PartitionKey(userId));
            return Ok(user.Vitals);
        }
        catch (CosmosException)
        {
            return NotFound();
        }
    }

    [HttpPut("{userId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(string userId, [FromBody] Vitals vitals)
    {
        User user;
        PartitionKey partitionKey = new(userId);
        try
        {
            user = await _container.ReadItemAsync<User>(userId, partitionKey);
        }
        catch (CosmosException)
        {
            return NotFound();
        }

        user.Vitals.Ecg.AddRange(vitals.Ecg);
        user.Vitals.HeartRate.AddRange(vitals.HeartRate);
        user.Vitals.SpO2.AddRange(vitals.SpO2);
        await _container.UpsertItemAsync(user, partitionKey);
        return NoContent();
    }
}