using Ardalis.Result;

namespace Common.LanguageExtensions.Utilities.ResultExtensions;

public static partial class ResultFunctionalExtensions
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
            case ResultStatus.Conflict:
                return errors.Count == 1
                    ? Result.Conflict(errors.Single())
                    : Result.Conflict(string.Concat(errors, ","));
            case ResultStatus.Error:
                return errors.Count == 1
                    ? Result.Error(errors.Single())
                    : Result.Error(new ErrorList(errors));
            case ResultStatus.Forbidden:
                return errors.Count == 1
                    ? Result.Forbidden(errors.Single())
                    : Result.Forbidden(string.Concat(errors, ","));
            case ResultStatus.Invalid:
                return errors.Count == 1
                    ? Result.Invalid(new ValidationError(errors.Single()))
                    : Result.Invalid(new ValidationError(string.Concat(errors, ",")));
            case ResultStatus.NotFound:
                return errors.Count == 1
                    ? Result.NotFound(errors.Single())
                    : Result.NotFound(string.Concat(errors, ","));
            case ResultStatus.Unauthorized:
                return errors.Count == 1
                    ? Result.Unauthorized(errors.Single())
                    : Result.Unauthorized(string.Concat(errors, ","));
            case ResultStatus.Unavailable:
                return errors.Count == 1
                    ? Result.Unavailable(errors.Single())
                    : Result.Unavailable([.. errors]);
            default:
                return errors.Count == 1
                    ? Result.CriticalError(errors.Single())
                    : Result.CriticalError([.. errors]);
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
            case ResultStatus.Conflict:
                return errors.Count == 1
                    ? Result.Conflict(errors.Single())
                    : Result.Conflict(string.Concat(errors, ","));
            case ResultStatus.Error:
                return errors.Count == 1
                    ? Result.Error(errors.Single())
                    : Result.Error(new ErrorList(errors));
            case ResultStatus.Forbidden:
                return errors.Count == 1
                    ? Result.Forbidden(errors.Single())
                    : Result.Forbidden(string.Concat(errors, ","));
            case ResultStatus.Invalid:
                return errors.Count == 1
                    ? Result.Invalid(new ValidationError(errors.Single()))
                    : Result.Invalid(new ValidationError(string.Concat(errors, ",")));
            case ResultStatus.NotFound:
                return errors.Count == 1
                    ? Result.NotFound(errors.Single())
                    : Result.NotFound(string.Concat(errors, ","));
            case ResultStatus.Unauthorized:
                return errors.Count == 1
                    ? Result.Unauthorized(errors.Single())
                    : Result.Unauthorized(string.Concat(errors, ","));
            case ResultStatus.Unavailable:
                return errors.Count == 1
                    ? Result.Unavailable(errors.Single())
                    : Result.Unavailable([.. errors]);
            default:
                return errors.Count == 1
                    ? Result.CriticalError(errors.Single())
                    : Result.CriticalError([.. errors]);
        }
    }

    public static Result AsResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Result.Success();
        }

        IReadOnlyCollection<string> errors = result.Errors
            .Concat(result.ValidationErrors.Select(e => e.ErrorMessage))
            .ToList();

        switch (result.Status)
        {
            case ResultStatus.Conflict:
                return errors.Count == 1
                    ? Result.Conflict(errors.Single())
                    : Result.Conflict(string.Concat(errors, ","));
            case ResultStatus.Error:
                return errors.Count == 1
                    ? Result.Error(errors.Single())
                    : Result.Error(new ErrorList(errors));
            case ResultStatus.Forbidden:
                return errors.Count == 1
                    ? Result.Forbidden(errors.Single())
                    : Result.Forbidden(string.Concat(errors, ","));
            case ResultStatus.Invalid:
                return errors.Count == 1
                    ? Result.Invalid(new ValidationError(errors.Single()))
                    : Result.Invalid(new ValidationError(string.Concat(errors, ",")));
            case ResultStatus.NotFound:
                return errors.Count == 1
                    ? Result.NotFound(errors.Single())
                    : Result.NotFound(string.Concat(errors, ","));
            case ResultStatus.Unauthorized:
                return errors.Count == 1
                    ? Result.Unauthorized(errors.Single())
                    : Result.Unauthorized(string.Concat(errors, ","));
            case ResultStatus.Unavailable:
                return errors.Count == 1
                    ? Result.Unavailable(errors.Single())
                    : Result.Unavailable([.. errors]);
            default:
                return errors.Count == 1
                    ? Result.CriticalError(errors.Single())
                    : Result.CriticalError([.. errors]);
        }
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
