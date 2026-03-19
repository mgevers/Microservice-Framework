using Ardalis.Result;

namespace Common.LanguageExtensions.Utilities.ResultExtensions;

public static partial class ResultFunctionalExtensions
{
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> resultTask, Action<T> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> resultTask, Action action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    public static async Task<Result> Tap(this Task<Result> resultTask, Action action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    public static async Task<Result> Tap(this Task<Result> resultTask, Func<Task> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            await action();
        }

        return result;
    }
}
