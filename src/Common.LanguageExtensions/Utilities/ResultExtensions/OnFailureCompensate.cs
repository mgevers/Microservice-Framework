using Ardalis.Result;

namespace Common.LanguageExtensions.Utilities.ResultExtensions;

public static partial class ResultFunctionalExtensions
{
    public static async Task<Result<T>> OnFailureCompensate<T>(this Task<Result<T>> resultTask, Func<Result<T>, T> func)
    {
        var result = await resultTask;

        if (!result.IsSuccess)
        {
            return Result.Success(func(result));
        }

        return result;
    }

    public static async Task<Result<T>> OnFailureCompensate<T>(this Task<Result<T>> resultTask, Func<Result<T>, Task<T>> func)
    {
        var result = await resultTask;

        if (!result.IsSuccess)
        {
            return Result.Success(await func(result));
        }

        return result;
    }
}
