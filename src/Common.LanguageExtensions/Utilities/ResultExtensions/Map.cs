using Ardalis.Result;

namespace Common.LanguageExtensions.Utilities.ResultExtensions;

public static partial class ResultFunctionalExtensions
{
    public static Result<T> Map<T>(this Result result, Func<Result<T>> action)
    {
        if (result.IsSuccess)
        {
            return action();
        }
        else
        {
            return result.AsTypedError<T>();
        }
    }

    public static async Task<Result<T>> Map<T>(this Task<Result> resultTask, Func<Result<T>> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return action();
        }
        else
        {
            return result.AsTypedError<T>();
        }
    }

    public static async Task<Result<T>> Map<T>(this Task<Result> resultTask, Func<Task<Result<T>>> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return await action();
        }
        else
        {
            return result.AsTypedError<T>();
        }
    }

    public static async Task<Result<T>> Map<T>(this Task<Result> resultTask, Func<Task<T>> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return Result.Success(await action());
        }
        else
        {
            return result.AsTypedError<T>();
        }
    }

    public static async Task<Result<T>> Map<T>(this Task<Result> resultTask, Func<T> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return Result.Success(action());
        }
        else
        {
            return result.AsTypedError<T>();
        }
    }

    public static Result<K> Map<T, K>(this Result<T> result, Func<T, Result<K>> action)
    {
        if (result.IsSuccess)
        {
            return action(result.Value);
        }
        else
        {
            return result.AsTypedError<T, K>();
        }
    }

    public static async Task<Result<K>> Map<T, K>(this Task<Result<T>> resultTask, Func<T, K> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return action(result.Value);
        }
        else
        {
            return result.AsTypedError<T, K>();
        }
    }

    public static async Task<Result<K>> Map<T, K>(this Task<Result<T>> resultTask, Func<T, Result<K>> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return action(result.Value);
        }
        else
        {
            return result.AsTypedError<T, K>();
        }
    }

    public static async Task<Result<K>> Map<T, K>(this Task<Result<T>> resultTask, Func<T, Task<Result<K>>> action)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return await action(result.Value);
        }
        else
        {
            var ret = result.AsTypedError<T, K>();
            return ret;
        }
    }
}
