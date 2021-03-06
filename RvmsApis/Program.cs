using Microsoft.Azure.Cosmos;
using RvmsModels;

const string CorsAllOriginsPolicy = "allOriginsHeadersMethods";
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsAllOriginsPolicy, builder =>
        {
            builder.WithOrigins("*");
            builder.WithHeaders("*");
            builder.WithMethods("*");
        });
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGenNewtonsoftSupport();

// Add singleton for Cosmos DB service
builder.Services.AddSingleton(InitializeCosmosDbService());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseCors(CorsAllOriginsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

CosmosDbService InitializeCosmosDbService()
{
    var cosmosClient = new CosmosClient(
        builder.Configuration["CosmosDb:EndpointUri"],
        builder.Configuration["CosmosDb:Key"]
    );
    var databaseName = builder.Configuration["CosmosDb:DatabaseName"];
    var containerName = builder.Configuration["CosmosDb:ContainerName"];

    return new CosmosDbService(cosmosClient, databaseName, containerName);
}