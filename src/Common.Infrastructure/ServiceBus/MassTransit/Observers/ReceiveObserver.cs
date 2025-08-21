using MassTransit;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.ServiceBus.MassTransit.Observers
{
    public class ReceiveObserver : IReceiveObserver
    {
        private readonly ILogger<ReceiveObserver> logger;

        public ReceiveObserver(ILogger<ReceiveObserver> logger)
        {
            this.logger = logger;
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
        {
            logger.LogError("consume fault for message {consumedMessage} - {exception}", context.Message, exception);
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class
        {
            logger.LogInformation("post consume for message {consumedMessage}", context.Message);
            return Task.CompletedTask;
        }

        public Task PostReceive(ReceiveContext context)
        {
            logger.LogInformation("post receive for message {consumedMessage}", context.Body.GetString());
            return Task.CompletedTask;
        }

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            logger.LogError("receive fault for message {consumedMessage} - {exception}", context.Body.GetString(), exception);
            return Task.CompletedTask;
        }

        public Task PreReceive(ReceiveContext context)
        {
            logger.LogInformation("pre receive for message {consumedMessage}", context.Body.GetString());
            return Task.CompletedTask;
        }
    }
}
