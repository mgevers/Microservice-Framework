using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Common.LanguageExtensions.Telemetry;

public static class TraceActivityScope
{
    private static readonly ActivitySource _activitySource = new("MyVector.EBoss.Lambda.Etl");

    public static Activity? Start<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<TraceActivityAttribute>();
        var name = attribute?.Name ?? typeof(T).Name;
        var kind = attribute?.Kind ?? ActivityKind.Internal;

        var activity = _activitySource.StartActivity(name, kind);
        activity?.SetTag("code.namespace", typeof(T).Namespace);
        activity?.SetTag("code.function", typeof(T).Name);

        return activity;
    }

    public static Activity? Start(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource.StartActivity(name, kind);
    }

    public static Activity? StartFromCaller(
        ActivityKind kind = ActivityKind.Internal,
        [CallerMemberName] string callerName = "")
    {
        return _activitySource.StartActivity(callerName, kind);
    }

    public static void Execute<T>(Action action)
    {
        using var activity = Start<T>();
        action();
    }

    public static async Task ExecuteAsync<T>(Func<Task> action)
    {
        using var activity = Start<T>();
        await action();
    }

    public static TResult Execute<T, TResult>(Func<TResult> func)
    {
        using var activity = Start<T>();
        return func();
    }

    public static async Task<TResult> ExecuteAsync<T, TResult>(Func<Task<TResult>> func)
    {
        using var activity = Start<T>();
        return await func();
    }
}
