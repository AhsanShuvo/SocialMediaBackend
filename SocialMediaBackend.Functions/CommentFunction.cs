using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SocialMediaBackend.API.Data;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using System.Text;

namespace SocialMediaBackend.Functions
{
    public class CommentFunction
    {
        private readonly ILogger<CommentFunction> _logger;
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;

        public CommentFunction(ILogger<CommentFunction> logger, AppDbContext context, ICacheService cacheService)
        {
            _logger = logger;
            _context = context;
            _cacheService = cacheService;
        }

        [Function(nameof(CommentFunction))]
        public async Task RunAsync(
        [ServiceBusTrigger("%ServiceBus:QueueName%", Connection = "ServiceBus:ConnectionString")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        {
            try
            {
                string messageJson = Encoding.UTF8.GetString(message.Body);
                var comment = JsonConvert.DeserializeObject<Comment>(messageJson);

                if (comment == null)
                {
                    _logger.LogWarning("Received an empty or invalid comment message.");
                    await messageActions.DeadLetterMessageAsync(message);
                    return;
                }

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                await _cacheService.RemoveAsync("posts:");

                _logger.LogInformation($"Comment processed: {comment.Id}");
                await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing comment message: {ex.Message}");
                await messageActions.AbandonMessageAsync(message);
            }
        }
    }
}
