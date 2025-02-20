using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Services;
using SocialMediaBackend.API.Settings;
using StackExchange.Redis;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<AzureStorageSettings>(builder.Configuration.GetSection("AzureStorage"));

builder.Services.AddSingleton<BlobServiceClient>(bs =>
{
    var storageSettings = builder.Configuration.GetSection("AzureStorage").Get<AzureStorageSettings>();

    return new BlobServiceClient(storageSettings.ConnectionString);
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration["SQLConnectionString"]);
});

builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["RedisConnectionString"])
);

builder.Build().Run();
