using Common.Testing.Logging;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public interface IAutoMockerTestResult
{
    AutoMocker AutoMocker { get; }
}

public interface IRepsitoryTestResult
{
    DatabaseState DatabaseState { get; }
}

public interface IServiceBusTestResult
{
    ServiceBusState ServiceBusState { get; }
}

public interface ITestOutput<T>
{
    public T Output { get; }
}

public interface ILoggingTestResult
{
    public IReadOnlyList<LogEntry> Logs { get; }
}

public interface IExceptionTestResult
{
    public Exception? ExceptionThrown { get; }
}
