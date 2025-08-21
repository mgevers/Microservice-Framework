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
            logger.LogInformation("post send for message {sentMessage}", context.Message);
            return Task.CompletedTask;
        }

        public Task PreSend<T>(SendContext<T> context) where T : class
        {
            logger.LogInformation("pre send for message {sentMessage}", context.Message);
            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception) where T : class
        {
            logger.LogError("send fault for message {sentMessage} - {exception}", context.Message, exception);
            return Task.CompletedTask;
        }
    }
}
