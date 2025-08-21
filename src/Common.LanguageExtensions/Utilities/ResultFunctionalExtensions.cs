using Ardalis.Result;

namespace Common.LanguageExtensions.Utilities;

public static class ResultFunctionalExtensions
{
    public static Result<K> AsTypedError<K>(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("can only cast with error result");
        }

        IReadOnlyCollection<string> errors = result.Errors
            .Concat(result.ValidationErrors.Select(e => e.ErrorMessage))
            .ToList();

        switch (result.Status)
        {
            case ResultStatus.Error:
                return Result.Error(new ErrorList(errors));
            case ResultStatus.Forbidden:
                return Result.Forbidden(string.Concat(errors, ","));
            case ResultStatus.Invalid:
                return Result.Invalid(new ValidationError(string.Concat(errors, ",")));
            case ResultStatus.NotFound:
                return Result.NotFound(string.Concat(errors, ","));
            case ResultStatus.Unauthorized:
                return Result.Unauthorized(string.Concat(errors, ","));
            case ResultStatus.Unavailable:
                return Result.Unavailable([.. errors]);
            default:
                return Result.CriticalError([.. errors]);
        }
    }

    public static Result<K> AsTypedError<T, K>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("can only cast with error result");
        }

        IReadOnlyCollection<string> errors = result.Errors
            .Concat(result.ValidationErrors.Select(e => e.ErrorMessage))
            .ToList();

        switch (result.Status)
        {
            case ResultStatus.Error:
                return Result.Error(new ErrorList(errors));
            case ResultStatus.Forbidden:
                return Result.Forbidden(string.Concat(errors, ","));
            case ResultStatus.Invalid:
                return Result.Invalid(new ValidationError(string.Concat(errors, ",")));
            case ResultStatus.NotFound:
                return Result.NotFound(string.Concat(errors, ","));
            case ResultStatus.Unauthorized:
                return Result.Unauthorized(string.Concat(errors, ","));
            case ResultStatus.Unavailable:
                return Result.Unavailable([.. errors]);
            default:
                return Result.CriticalError([.. errors]);
        }
    }

    public static Result<KeyValuePair<T, K>> Combine<T,K>(this Result<T> result1, Func<T, Result<K>> func)
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

    public static Result Bind(this Result result, Func<Result> func)
    {
        if (!result.IsSuccess)
        {
            return result;
        }

        return func();
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

    public static Result AsResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Result.Success();
        }

        return Result.CriticalError([.. result.Errors]);
    }

    public static async Task<Result> AsResult<T>(this Task<Result<T>> resultTask)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return Result.Success();
        }

        return Result.CriticalError([.. result.Errors]);
    }
}
