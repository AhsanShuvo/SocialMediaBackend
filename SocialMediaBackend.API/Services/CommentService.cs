using Azure.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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

        public CommentService(ServiceBusClient serviceBusClient, IOptions<ServiceBusSettings> options, ILogger<CommentService> logger)
        {
            _serviceBusClient = serviceBusClient;
            _settings = options.Value;
            _sender = _serviceBusClient.CreateSender(_settings.QueueName);
            _logger = logger;
        }

        public async Task<bool> CreateCommentAsync(CommentRequest request)
        {
            try
            {
                var comment = new Comment
                {
                    Content = request.Content,
                    CreatorId = request.CreatorId,
                    PostId = request.PostId
                };

                var messageBody = JsonConvert.SerializeObject(comment);

                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));
                await _sender.SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return false;
            }
        }
    }

}
