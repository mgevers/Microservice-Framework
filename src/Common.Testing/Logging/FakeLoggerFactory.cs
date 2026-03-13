using Microsoft.Extensions.Logging;

namespace Common.Testing.Logging;

public class FakeLoggerFactory : ILoggerFactory
{
    private readonly List<FakeLogger> _loggers = [];
    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FakeLogger(categoryName);
    }

    public void Dispose()
    {
        _loggers.ForEach(logger =>
        {
            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        });

        _loggers.Clear();
    }
}
