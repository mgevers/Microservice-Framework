using DnsClient.Internal;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.ServiceBus.MassTransit.Observers
{
    public class SendObserver : ISendObserver
    {
        private readonly ILogger<SendObserver> logger;

        public SendObserver(ILogger<SendObserver> logger)
        {
            this.logger = logger;
        }

        public Task PostSend<T>(SendContext<T> context) where T : class
        {
            logger.LogInformation("post send for {messageType} {messageId}", typeof(T).Name, context.MessageId);
            return Task.CompletedTask;
        }

        public Task PreSend<T>(SendContext<T> context) where T : class
        {
            logger.LogInformation("pre send for {messageType} {messageId}", typeof(T).Name, context.MessageId);
            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception) where T : class
        {
            logger.LogError("send fault for {messageType} {messageId} - {exceptionType}", typeof(T).Name, context.MessageId, exception.GetType().Name);
            return Task.CompletedTask;
        }
    }
}
