using Azure.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly.Retry;
using SocialMediaBackend.API.Helper;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using SocialMediaBackend.API.Models.Requests;
using SocialMediaBackend.API.Settings;
using System.Text;

namespace SocialMediaBackend.API.Services
{
    public class CommentService : ICommentService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusSender _sender;
        private readonly ServiceBusSettings _settings;
        private readonly ILogger<CommentService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ICacheService _cacheService;

        public CommentService(ServiceBusClient serviceBusClient, IOptions<ServiceBusSettings> options, ILogger<CommentService> logger, ICacheService cacheService)
        {
            _serviceBusClient = serviceBusClient;
            _settings = options.Value;
            _sender = _serviceBusClient.CreateSender(_settings.QueueName);
            _logger = logger;
            _retryPolicy = RetryPolicies.CreateRetryPolicy(_logger);
            _cacheService = cacheService;
        }

        public async Task<bool> CreateCommentAsync(CommentRequest request)
        {
            try
            {
                var comment = new Comment
                {
                    Id = Guid.NewGuid(),
                    Content = request.Content,
                    CreatorId = request.CreatorId,
                    PostId = request.PostId
                };

                var messageBody = JsonConvert.SerializeObject(comment);

                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));

                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _sender.SendMessageAsync(message);
                    await _cacheService.AddCommentToPostAsync(comment.PostId.ToString(), comment.Id.ToString(), comment);
                });

                _logger.LogInformation("Successully sent message to service bus for Creator Id: {CreatorId}", comment.CreatorId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw;
            }
        }
    }

}
