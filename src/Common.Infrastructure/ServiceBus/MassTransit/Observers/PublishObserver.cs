using MassTransit;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.ServiceBus.MassTransit.Observers
{
    public class PublishObserver : IPublishObserver
    {
        private readonly ILogger<PublishObserver> logger;

        public PublishObserver(ILogger<PublishObserver> logger)
        {
            this.logger = logger;
        }

        public Task PostPublish<T>(PublishContext<T> context)
            where T : class
        {
            logger.LogInformation("post publish for message {publishedMessage}", context.Message);
            return Task.CompletedTask;
        }

        public Task PrePublish<T>(PublishContext<T> context)
            where T : class
        {
            logger.LogInformation("pre publish for message {publishedMessage}", context.Message);
            return Task.CompletedTask;
        }

        public Task PublishFault<T>(PublishContext<T> context, Exception exception)
            where T : class
        {
            logger.LogError("publish fault for message {publishedMessage} - {exception}", context.Message, exception);
            return Task.CompletedTask;
        }
    }
}
