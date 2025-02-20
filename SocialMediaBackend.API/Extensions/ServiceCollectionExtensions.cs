using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Services;
using SocialMediaBackend.API.Settings;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace SocialMediaBackend.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private const string Jwt = "Jwt";
        private const string AzureStorage = "AzureStorage";
        private const string ServiceBus = "ServiceBus";

        public static IServiceCollection AddConfigurationSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(Jwt));
            services.Configure<AzureStorageSettings>(configuration.GetSection(AzureStorage));
            services.Configure<ServiceBusSettings>(configuration.GetSection(ServiceBus));

            return services;
        }

        public static IServiceCollection ConfigureJwtToken(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection(Jwt).Get<JwtSettings>();
            
            if(string.IsNullOrEmpty(jwtSettings.SecretKey))
            {
                throw new ArgumentException("Jwt secret key is not configured properly.");
            }

            var secretKey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = false,
                        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
                    };
                });

            return services;
        }

        public static IServiceCollection AddAzureStorage(this IServiceCollection services, IConfiguration configuration)
        {
            var storageSettings = configuration.GetSection(AzureStorage).Get<AzureStorageSettings>();

            services.AddSingleton<BlobServiceClient>(
                new BlobServiceClient(storageSettings.ConnectionString)
            );
            services.AddScoped<IImageStorageService, BlobStorageService>();

            return services;
        }

        public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            var redisSettings = configuration.GetSection("Redis").Get<RedisSettings>();

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisSettings.ConnectionString)
            );

            services.AddScoped<ICacheService, CacheService>();

            return services;
        }

        public static IServiceCollection AddServiceBus(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceBusSettings = configuration.GetSection(ServiceBus).Get<ServiceBusSettings>();

            services.AddSingleton<ServiceBusClient>(
                new ServiceBusClient(serviceBusSettings.ConnectionString)
            );

            services.AddSingleton<ServiceBusProcessor>(sp =>
            {
                var client = sp.GetRequiredService<ServiceBusClient>();
                return client.CreateProcessor("test-queue", new ServiceBusProcessorOptions());
            });

            return services;
        }
    }
}
