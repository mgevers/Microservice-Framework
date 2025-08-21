using Microsoft.Extensions.Logging;
using Moq;

namespace Common.Testing.Logging;

public static class MockLoggerAssertExtensions
{
    public static void AssertLogs<T>(Mock<ILogger<T>> mockLogger, params LogEntry[] logs)
    {
        foreach (var logInfo in logs)
        {
            mockLogger.Verify(x => x.Log(
                logInfo.LogLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) =>
                    string.IsNullOrEmpty(logInfo.Message) || string.Equals(logInfo.Message, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
        }
    }
}
