using Microsoft.EntityFrameworkCore;
using SocialMediaBackend.API.BackgroundServices;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Extensions;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Services;

namespace SocialMediaBackend.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var services = builder.Services;

            services.AddConfigurationSettings(builder.Configuration);

            services.ConfigureJwtToken(builder.Configuration);

            services.AddAzureStorage(builder.Configuration);
            services.AddRedisCache(builder.Configuration);
            services.AddServiceBus(builder.Configuration);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddHostedService<CommentConsumer>();
            services.AddHostedService<ImageProcessingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<JwtService>();

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            var app = builder.Build();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Application is starting up...");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Social Media Service");
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
