using MassTransit;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.ServiceBus.MassTransit.Observers
{
    public class ConsumeObserver : IConsumeObserver
    {
        private readonly ILogger<ConsumeObserver> logger;

        public ConsumeObserver(ILogger<ConsumeObserver> logger)
        {
            this.logger = logger;
        }

        public Task PreConsume<T>(ConsumeContext<T> context) where T : class
        {
            logger.LogInformation("pre consume for {messageType} {messageId}", typeof(T).Name, context.MessageId);
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context) where T : class
        {
            logger.LogInformation("post consume for {messageType} {messageId}", typeof(T).Name, context.MessageId);
            return Task.CompletedTask;
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            logger.LogError("consume fault for {messageType} {messageId} - {exceptionType}", typeof(T).Name, context.MessageId, exception.GetType().Name);
            return Task.CompletedTask;
        }
    }
}
