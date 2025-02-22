using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace SocialMediaBackend.API.Helper
{
    public static class RetryPolicies
    {
        public static AsyncRetryPolicy CreateRetryPolicy(ILogger logger) =>
        Policy
            .Handle<DbUpdateException>()  // Retry on EF Core update failure
            .Or<TimeoutException>()       // Handle timeout failures
            .Or<InvalidOperationException>() // Handles transient failures
            .Or<RedisException>() // Retry for Redis failures
            .Or<ServiceBusException>() // Retry for Service Bus failures
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning("Retry {RetryCount} due to: {ExceptionMessage}. Retrying in {RetryDelay}ms",
                        retryCount, exception.Message, timeSpan.TotalMilliseconds);
                });
    }
}
