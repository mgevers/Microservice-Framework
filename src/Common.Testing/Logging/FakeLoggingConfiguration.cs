using Microsoft.Extensions.Logging;

namespace Common.Testing.Logging;

public class FakeLoggingConfiguration
{
    private readonly Dictionary<string, LogLevel> _categoryLogLevels = [];

    public FakeLoggingConfiguration(LogLevel defaultLogLevel = LogLevel.Error)
    {
        DefaultLogLevel = defaultLogLevel;
        _categoryLogLevels.Add("MyVector", LogLevel.Trace);
    }

    public LogLevel DefaultLogLevel { get; init; }

    public void OverrideLogLevel(string categoryName, LogLevel logLevel)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Category name cannot be null or whitespace.", nameof(categoryName));
        }

        if (!_categoryLogLevels.TryAdd(categoryName, logLevel))
        {
            _categoryLogLevels[categoryName] = logLevel;
        }
    }

    public LogLevel GetLogLevel(string categoryName)
    {
        var logLevel = _categoryLogLevels.Keys.FirstOrDefault(category =>
            categoryName.Contains(category, StringComparison.OrdinalIgnoreCase));

        return logLevel == null ? DefaultLogLevel : _categoryLogLevels[logLevel];
    }
}
