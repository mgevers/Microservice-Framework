using Ardalis.Result;

namespace Common.LanguageExtensions.Utilities.ResultExtensions;

public static partial class ResultFunctionalExtensions
{
    public static async Task<Result> TapError(this Task<Result> resultTask, Action<Result> action)
    {
        var result = await resultTask;

        if (result.IsSuccess == false)
        {
            action(result);
        }

        return result;
    }

    public static async Task<Result<T>> TapError<T>(this Task<Result<T>> resultTask, Action<Result<T>> action)
    {
        var result = await resultTask;

        if (result.IsSuccess == false)
        {
            action(result);
        }

        return result;
    }
}
