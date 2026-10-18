using Microsoft.Extensions.Logging;

namespace Common.Testing.Logging;

public class FakeLoggerFactory : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FakeLogger();
    }

    public void Dispose()
    {
    }
}
