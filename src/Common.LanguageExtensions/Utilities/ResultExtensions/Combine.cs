using Ardalis.Result;
using static CSharpFunctionalExtensions.Result;

namespace Common.LanguageExtensions.Utilities.ResultExtensions;

public static partial class ResultFunctionalExtensions
{
    public static Result<KeyValuePair<T, K>> Combine<T, K>(this Result<T> result1, Func<T, Result<K>> func)
    {
        if (!result1.IsSuccess)
        {
            return result1.AsTypedError<T, KeyValuePair<T, K>>();
        }

        var result2 = func(result1.Value);

        if (!result2.IsSuccess)
        {
            return result2.AsTypedError<K, KeyValuePair<T, K>>();
        }

        return Result.Success(new KeyValuePair<T, K>(result1.Value, result2.Value));
    }

    public static async Task<Result<KeyValuePair<T, K>>> Combine<T, K>(this Task<Result<T>> resultTask, Func<T, Task<Result<K>>> func)
    {
        var result1 = await resultTask;

        if (!result1.IsSuccess)
        {
            return result1.AsTypedError<T, KeyValuePair<T, K>>();
        }

        var result2 = await func(result1.Value);

        if (!result2.IsSuccess)
        {
            return result2.AsTypedError<K, KeyValuePair<T, K>>();
        }

        return Result.Success(new KeyValuePair<T, K>(result1.Value, result2.Value));
    }

    public static Result Combine(this IEnumerable<Result> results, string? errorMessagesSeparator = null)
    {
        var failedResults = results.Where(x => !x.IsSuccess).ToList();

        if (failedResults.Count == 0)
        {
            return Result.Success();
        }

        if (failedResults.Count == 1)
        {
            return failedResults.Single();
        }

        string errorMessage = string.Join(
            errorMessagesSeparator ?? Configuration.ErrorMessagesSeparator,
            AggregateMessages(failedResults.SelectMany(x => x.Errors)));

        return Result.CriticalError(errorMessage);
    }

    private static IEnumerable<string> AggregateMessages(IEnumerable<string> messages)
    {
        var dict = new Dictionary<string, int>();
        foreach (var message in messages)
        {
            if (!dict.ContainsKey(message))
            {
                dict.Add(message, 0);
            }

            dict[message]++;
        }

        return dict.Select(x => x.Value == 1 ? x.Key : $"{x.Key} ({x.Value}×)");
    }

}
