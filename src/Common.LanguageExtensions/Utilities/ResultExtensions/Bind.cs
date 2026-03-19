using Ardalis.Result;

namespace Common.LanguageExtensions.Utilities.ResultExtensions;

public static partial class ResultFunctionalExtensions
{
    public static Result Bind(this Result result, Func<Result> func)
    {
        if (!result.IsSuccess)
        {
            return result;
        }

        return func();
    }

    public static async Task<Result> Bind(this Result result, Func<Task<Result>> func)
    {
        if (!result.IsSuccess)
        {
            return result;
        }

        return await func();
    }

    public static async Task<Result> Bind(this Task<Result> resultTask, Func<Task<Result>> func)
    {
        var result = await resultTask;

        if (!result.IsSuccess)
        {
            return result;
        }

        return await func();
    }

    public static Result<T> Bind<T>(this Result<T> result, Func<T, Result<T>> func)
    {
        if (!result.IsSuccess)
        {
            return result;
        }

        return func(result.Value);
    }

    public static async Task<Result<T>> Bind<T>(this Task<Result<T>> resultTask, Func<T, Result<T>> func)
    {
        var result = await resultTask;

        if (!result.IsSuccess)
        {
            return result;
        }

        return func(result.Value);
    }

    public static async Task<Result<T>> Bind<T>(this Task<Result<T>> resultTask, Func<T, Task<Result<T>>> func)
    {
        var result = await resultTask;

        if (!result.IsSuccess)
        {
            return result;
        }

        return await func(result.Value);
    }

    public static async Task<Result<T>> Bind<T>(this Task<Result<T>> resultTask, Func<T, Result> func)
    {
        var result = await resultTask;

        if (!result.IsSuccess)
        {
            return result;
        }

        return func(result.Value);
    }

    public static async Task<Result<T>> Bind<T>(this Task<Result<T>> resultTask, Func<T, Task<Result>> func)
    {
        var result = await resultTask;

        if (!result.IsSuccess)
        {
            return result;
        }

        return await func(result.Value);
    }
}
