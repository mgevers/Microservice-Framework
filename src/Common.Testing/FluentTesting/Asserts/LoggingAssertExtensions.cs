using Common.LanguageExtensions.Utilities;
using Common.Testing.Assert;
using Common.Testing.Logging;
using Microsoft.Extensions.Logging;

namespace Common.Testing.FluentTesting.Asserts;

public static class LoggingAssertExtensions
{
    public static async Task<T> AssertExactLogs<T>(this Task<T> resultTask, params LogEntry[] logs)
         where T : ILoggingTestResult
    {
        var result = await resultTask;

        if (logs.Length != result.Logs.Count)
        {
            throw new Exception($"assert logs failed: expected {logs.Length} logs, actually found {result.Logs.Count}");
        }

        var expectedLogs = logs.OrderBy(e => e.GetHashCode()).ToList();
        var actualLogs = result.Logs.OrderBy(e => e.GetHashCode()).ToList();

        for (var i = 0; i < expectedLogs.Count; i++)
        {
            AssertExtensions.DeepEqual(expectedLogs[i], actualLogs[i]);
        }

        return result;
    }

    public static Task<T> AssertLog<T>(this Task<T> resultTask, LogEntry log)
         where T : ILoggingTestResult
    {
        return resultTask.AssertLog(log.LogLevel, log.Message);
    }

    public static async Task<T> AssertLog<T>(this Task<T> resultTask, LogLevel logLevel, string message)
         where T : ILoggingTestResult
    {
        var result = await resultTask;

        var actualLogs = result.Logs.OrderBy(e => e.GetHashCode()).ToList();

        var matchingLog = actualLogs.SingleOrDefault(l => l.LogLevel == logLevel && l.Message == message);
        Xunit.Assert.NotNull(matchingLog);

        return result;
    }

    public static Task<T> AssertStructuredLog<T>(this Task<T> resultTask, LogEntry log)
         where T : ILoggingTestResult
    {
        Xunit.Assert.NotNull(log.Payload);
        return resultTask.AssertStructuredLog(log.LogLevel, log.Template, log.Payload);
    }

    public static async Task<T> AssertStructuredLog<T>(
        this Task<T> resultTask,
        LogLevel logLevel,
        string template,
        object structuredData)
        where T : ILoggingTestResult
    {
        var structuredDataDict = GetStructuredDataDictionary(structuredData);

        return await resultTask.AssertStructuredLog(logLevel, template, structuredDataDict);
    }

    public static Task<T> AssertStructuredLog<T>(
        this Task<T> resultTask,
        LogLevel logLevel,
        string template,
        IDictionary<string, string> structuredDataDict)
        where T : ILoggingTestResult
    {
        return resultTask.AssertStructuredLogInternal(logLevel, template, structuredDataDict);
    }

    public static Task<T> AssertStructuredLog<T>(
        this Task<T> resultTask,
        LogLevel logLevel,
        object structuredData)
        where T : ILoggingTestResult
    {
        var structuredDataDict = GetStructuredDataDictionary(structuredData);

        return resultTask.AssertStructuredLogInternal(logLevel, null, structuredDataDict);
    }

    public static Task<T> AssertStructuredLog<T>(
       this Task<T> resultTask,
       LogLevel logLevel,
       IDictionary<string, string> structuredDataDict)
       where T : ILoggingTestResult
    {
        return resultTask.AssertStructuredLogInternal(logLevel, null, structuredDataDict);
    }

    private static IDictionary<string, string> GetStructuredDataDictionary(object structuredData)
    {
        var dict = new Dictionary<string, string>();
        var props = structuredData.GetType().GetProperties()
            .Select(property => property.Name)
            .ToList();

        var payloads = structuredData.GetType().GetProperties()
            .Select(property => property.GetValue(structuredData)!)
            .ToArray();

        return LogEntry.ConvertToDictionary(props, payloads);
    }

    private static async Task<T> AssertStructuredLogInternal<T>(
       this Task<T> resultTask,
       LogLevel logLevel,
       string? template,
       IDictionary<string, string> structuredDataDict)
       where T : ILoggingTestResult
    {
        var result = await resultTask;

        var actualLogs = result.Logs.OrderBy(e => e.GetHashCode()).ToList();

        var matchingLog = actualLogs.SingleOrDefault(l => l.LogLevel == logLevel
            && (template == null || l.Template == template)
            && l.Payload != null
            && structuredDataDict.Keys.All(key => BooleanUtilities.IsDeepEqual(l.Payload[key], structuredDataDict[key])));

        Xunit.Assert.NotNull(matchingLog);

        return result;
    }
}
