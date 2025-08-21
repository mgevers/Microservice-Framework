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
            logger.LogInformation("pre consume for message {consumedMessage}", context.Message);
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context) where T : class
        {
            logger.LogInformation("post consume for message {consumedMessage}", context.Message);
            return Task.CompletedTask;
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            logger.LogError("consume fault for message {consumedMessage} - {exception}", context.Message, exception);
            return Task.CompletedTask;
        }
    }
}
