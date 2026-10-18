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
            logger.LogError("consume fault for {messageType} {messageId} - {exceptionType}", typeof(T).Name, context.MessageId, exception.GetType().Name);
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class
        {
            logger.LogInformation("post consume for {messageType} {messageId}", typeof(T).Name, context.MessageId);
            return Task.CompletedTask;
        }

        public Task PostReceive(ReceiveContext context)
        {
            logger.LogInformation("post receive from {inputAddress}", context.InputAddress);
            return Task.CompletedTask;
        }

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            logger.LogError("receive fault from {inputAddress} - {exceptionType}", context.InputAddress, exception.GetType().Name);
            return Task.CompletedTask;
        }

        public Task PreReceive(ReceiveContext context)
        {
            logger.LogInformation("pre receive from {inputAddress}", context.InputAddress);
            return Task.CompletedTask;
        }
    }
}
