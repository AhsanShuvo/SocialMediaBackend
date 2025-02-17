using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using SocialMediaBackend.API.Interfaces;
using SocialMediaBackend.API.Models;
using System.Text;

namespace SocialMediaBackend.API.Services
{
    public class CommentService : ICommentService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusSender _sender;

        public CommentService(ServiceBusClient serviceBusClient, IConfiguration configuration)
        {
            _serviceBusClient = serviceBusClient;
            _sender = _serviceBusClient.CreateSender(configuration["AzureServiceBus:QueueName"]);
        }

        [Authorize]
        public async Task<bool> CreateCommentAsync(Comment comment)
        {
            var messageBody = JsonConvert.SerializeObject(comment);

            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));
            await _sender.SendMessageAsync(message);
            return true;
        }
    }

}
