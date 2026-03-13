using Common.Testing.Assert;
using Common.Testing.Logging;
using Common.Testing.Persistence;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public static class AssertFluentExtensions
{
    public static async Task<T> AssertDatabase<T>(this Task<T> resultTask, DatabaseState databaseState)
        where T : IRepsitoryTestResult
    {
        var result = await resultTask;
        AssertExtensions.EqualDatabaseStates(databaseState, result.DatabaseState);
        return result;
    }

    public static async Task<T> AssertPublishedEvents<T>(this Task<T> resultTask, IReadOnlyCollection<object> expectedPublishedEvents)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(expectedPublishedEvents, result.ServiceBusState.PublishedMessages);
        return result;
    }

    public static async Task<T> AssertPublishedEvent<T>(this Task<T> resultTask, object expectedPublishedEvent)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(new[] { expectedPublishedEvent }, result.ServiceBusState.PublishedMessages);
        return result;
    }

    public static async Task<T> AssertRepliedMessages<T>(this Task<T> resultTask, IReadOnlyCollection<IMessage> expectedRepliedMessages)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(expectedRepliedMessages, result.ServiceBusState.RepliedMessages);
        return result;
    }

    public static async Task<T> AssertRepliedMessage<T>(this Task<T> resultTask, IMessage expectedRepliedEvent)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(new[] { expectedRepliedEvent }, result.ServiceBusState.RepliedMessages);
        return result;
    }

    public static async Task<T> AssertNoPublishedEvents<T>(this Task<T> resultTask)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(Array.Empty<IMessage>(), result.ServiceBusState.PublishedMessages);
        return result;
    }

    public static async Task<T> AssertOutput<T>(this Task<T> resultTask, Result expectedResult)
        where T : ITestOutput<Result>
    {
        var result = await resultTask;

        AssertExtensions.EqualResults(expectedResult, result.Output);

        return result;
    }

    public static async Task<T> AssertOutput<T, TOutput>(this Task<T> resultTask, TOutput expectedResult)
        where T : ITestOutput<TOutput>
    {
        var result = await resultTask;

        AssertExtensions.DeepEqual(expectedResult, result.Output);

        return result;
    }

    public static async Task<T> AssertHttpResponse<T>(this Task<T> resultTask, Func<HttpResponseMessage, bool> func)
        where T : ITestOutput<HttpResponseMessage>
    {
        var result = await resultTask;

        var output = func(result.Output);

        Xunit.Assert.True(output);

        return result;
    }

    public static async Task<T> AssertMocker<T>(this Task<T> resultTask, Action<AutoMocker> action)
        where T : IAutoMockerTestResult
    {
        var result = await resultTask;

        action(result.AutoMocker);

        return result;
    }

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

        for (var i = 0; i < expectedLogs.Count; i++ )
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

    public static async Task<T> AssertExceptionThrown<T>(this Task<T> resultTask, Type? exceptionType = null)
        where T : IExceptionTestResult
    {
        exceptionType ??= typeof(Exception);
        var result = await resultTask;

        var exception = result.ExceptionThrown;

        Xunit.Assert.NotNull(exception);
        Xunit.Assert.True(
            exceptionType.IsAssignableFrom(exception.GetType()),
            $"{exceptionType.Name} not assignable from {exception.GetType().Name}");

        return result;
    }

    private static void AssertMessages(
        IReadOnlyCollection<object> expectedMessages,
        IReadOnlyCollection<object> actualMessages)
    {
        AssertExtensions.DeepEqual(
            expected: expectedMessages.Select(GetMessageAndType),
            actual: actualMessages.Select(GetMessageAndType));
    }

    private static object GetMessageAndType(object message)
    {
        return new
        {
            TypeFullName = message.GetType().FullName,
            Message = message,
        };
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
            && structuredDataDict.Keys.All(key => AssertExtensions.IsDeepEqual(l.Payload[key], structuredDataDict[key])));

        Xunit.Assert.NotNull(matchingLog);

        return result;
    }
}
