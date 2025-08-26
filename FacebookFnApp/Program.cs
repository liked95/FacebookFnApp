using Azure.Storage.Blobs;
using FacebookFnApp.Data;
using FacebookFnApp.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Azure blob storage string
string blobConnectionString = builder.Configuration.GetValue<string>("AzureBlobStorage")
    ?? throw new InvalidOperationException("AzureBlobStorage ConnectionString not found!");

builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

// SQL connection string
string sqlConnectionString = builder.Configuration.GetValue<string>("SqlConnection")
    ?? throw new InvalidOperationException("SqlConnection not found!");

builder.Services.AddSingleton(new SqlConnectionFactory(sqlConnectionString));


// Register services
builder.Services.AddScoped<IMediaProcessingService, MediaProcessingService>();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
