using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Models;
using System.Text;

namespace SocialMediaBackend.API.BackgroundServices
{
    public class CommentConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ServiceBusProcessor _processor;

        public CommentConsumer(IServiceScopeFactory serviceScopeFactory, ServiceBusProcessor processor)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _processor = processor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _processor.ProcessMessageAsync += async args =>
            {
                var body = args.Message.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var comment = JsonConvert.DeserializeObject<Comment>(messageJson);

                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                context.Comments.Add(comment);
                await context.SaveChangesAsync();

                await args.CompleteMessageAsync(args.Message);
            };

            _processor.ProcessErrorAsync += async args =>
            {
                Console.WriteLine($"Error processing message: {args.Exception}");
            };

            await _processor.StartProcessingAsync();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync();
            await _processor.DisposeAsync();
        }
    }
}
