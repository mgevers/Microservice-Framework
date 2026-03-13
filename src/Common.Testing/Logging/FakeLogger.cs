using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common.Testing.Logging;

public class FakeLogger<T> : FakeLogger, ILogger<T>
{
    public FakeLogger(string categoryName)
        : base(categoryName)
    {
    }
}

public class FakeLogger(string categoryName) : ILogger
{
    private readonly static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (!FakeLoggingDatabase.IsInitialized() || FakeLoggingDatabase.LoggingConfiguration == null)
        {
            return false;
        }

        if (FakeLoggingDatabase.LoggingConfiguration == null)
        {
            return false;
        }

        var configuredLogLevel = FakeLoggingDatabase.LoggingConfiguration.GetLogLevel(categoryName);

        return logLevel >= configuredLogLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        const string originalFormat = "{OriginalFormat}";
        if (!typeof(IReadOnlyList<KeyValuePair<string, object?>>).IsAssignableFrom(typeof(TState)))
        {
            var message = formatter(state, exception);

            FakeLoggingDatabase.AddLog(new LogEntry(logLevel, message));
            return;
        }

        var dict = new Dictionary<string, string>();
        var stateValues = (IReadOnlyList<KeyValuePair<string, object?>>)state!;
        var templateKvp = stateValues.SingleOrDefault(kvp => kvp.Key == originalFormat);
        if (templateKvp.Key == null)
        {
            var message = formatter(state, exception);
            FakeLoggingDatabase.AddLog(new LogEntry(logLevel, message));
            return;
        }

        var template = (string)templateKvp.Value!;
        var templateVariables = stateValues
            .Select(kvp => kvp.Key)
            .Where(key => key != originalFormat)
            .ToList();

        foreach (var variable in templateVariables)
        {
            var kvp = stateValues.Single(kvp => kvp.Key == variable);
            var payload = kvp.Value!;

            if (payload == null)
            {
                dict.Add(variable, "null");
            }
            else if (payload is string stringValue)
            {
                dict.Add(variable, stringValue);
            }
            else if (payload is Guid guidValue)
            {
                dict.Add(variable, guidValue.ToString());
            }
            else if (payload is RouteEndpoint routeEndpoint)
            {
                dict.Add(variable, routeEndpoint.DisplayName!);
            }
            else
            {
                var json = JsonConvert.SerializeObject(payload, SerializerSettings);
                dict.Add(variable, json);
            }
        }

        var structuredLog = new LogEntry(logLevel, template, dict);

        FakeLoggingDatabase.AddLog(structuredLog);
    }
}
