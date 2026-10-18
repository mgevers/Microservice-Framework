using Common.Infrastructure;
using Common.Infrastructure.ServiceBus.MassTransit.Observers;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog.AspNetCore;
using Xunit;

namespace Common.Infrastructure.Tests.ServiceBus;

public class SensitiveDataLoggingTests
{
    private const string Secret = "Patient.Secret.Marker";

    public record SecretMessage(string Tag);

    private class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // The formatted text and every structured value are what a log sink receives.
            var values = state is IEnumerable<KeyValuePair<string, object?>> pairs ? string.Join("|", pairs.Select(p => p.Value)) : string.Empty;
            Entries.Add($"{formatter(state, exception)}|{values}");
        }
    }

    private static readonly SecretMessage Message = new(Secret);

    [Fact]
    public async Task ConsumeObserver_DoesNotLogTheMessageContent()
    {
        var logger = new CapturingLogger<ConsumeObserver>();
        var observer = new ConsumeObserver(logger);
        var context = new Mock<ConsumeContext<SecretMessage>>();
        context.SetupGet(c => c.Message).Returns(Message);
        context.SetupGet(c => c.MessageId).Returns(Guid.NewGuid());

        await observer.PreConsume(context.Object);
        await observer.PostConsume(context.Object);
        await observer.ConsumeFault(context.Object, new InvalidOperationException(Secret));

        Assert.Equal(3, logger.Entries.Count);
        Assert.All(logger.Entries, entry => Assert.DoesNotContain(Secret, entry));
        Assert.All(logger.Entries, entry => Assert.Contains(nameof(SecretMessage), entry));
    }

    [Fact]
    public async Task SendAndPublishObservers_DoNotLogTheMessageContent()
    {
        var sendLogger = new CapturingLogger<SendObserver>();
        var publishLogger = new CapturingLogger<PublishObserver>();
        var send = new Mock<SendContext<SecretMessage>>();
        send.SetupGet(c => c.Message).Returns(Message);
        var publish = new Mock<PublishContext<SecretMessage>>();
        publish.SetupGet(c => c.Message).Returns(Message);

        var sendObserver = new SendObserver(sendLogger);
        await sendObserver.PreSend(send.Object);
        await sendObserver.PostSend(send.Object);
        await sendObserver.SendFault(send.Object, new InvalidOperationException(Secret));
        var publishObserver = new PublishObserver(publishLogger);
        await publishObserver.PrePublish(publish.Object);
        await publishObserver.PostPublish(publish.Object);
        await publishObserver.PublishFault(publish.Object, new InvalidOperationException(Secret));

        Assert.All(sendLogger.Entries.Concat(publishLogger.Entries), entry => Assert.DoesNotContain(Secret, entry));
        Assert.Equal(6, sendLogger.Entries.Count + publishLogger.Entries.Count);
    }

    [Fact]
    public async Task ReceiveObserver_DoesNotLogTheBody()
    {
        var logger = new CapturingLogger<ReceiveObserver>();
        var observer = new ReceiveObserver(logger);
        var receive = new Mock<ReceiveContext>();
        receive.SetupGet(c => c.InputAddress).Returns(new Uri("rabbitmq://host/queue"));
        var consume = new Mock<ConsumeContext<SecretMessage>>();
        consume.SetupGet(c => c.Message).Returns(Message);

        await observer.PreReceive(receive.Object);
        await observer.PostReceive(receive.Object);
        await observer.ReceiveFault(receive.Object, new InvalidOperationException(Secret));
        await observer.PostConsume(consume.Object, TimeSpan.Zero, "consumer");
        await observer.ConsumeFault(consume.Object, TimeSpan.Zero, "consumer", new InvalidOperationException(Secret));

        Assert.Equal(5, logger.Entries.Count);
        Assert.All(logger.Entries, entry => Assert.DoesNotContain(Secret, entry));
        receive.Verify(c => c.Body, Times.Never);
    }

    [Fact]
    public void RequestLogging_UsesTheRouteTemplateAndNotThePath()
    {
        var options = new RequestLoggingOptions();
        WebApi.ConfigureRequestLogging(options);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = $"/api/tags/{Secret}";
        httpContext.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("api/tags/{tag}"), 0, null, null));

        var properties = options.GetMessageTemplateProperties!(httpContext, httpContext.Request.Path, 1.5, 200).ToList();

        Assert.DoesNotContain(properties, p => p.Value.ToString().Contains(Secret));
        Assert.DoesNotContain(options.MessageTemplate, "RequestPath");
        Assert.Contains(properties, p => p.Name == "RequestRoute" && p.Value.ToString().Contains("api/tags/{tag}"));
    }

    [Fact]
    public void RouteTemplate_WhenNoRouteMatched_DoesNotFallBackToThePath()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = $"/{Secret}";

        Assert.Equal("unmatched", WebApi.RouteTemplate(httpContext));
    }
}
