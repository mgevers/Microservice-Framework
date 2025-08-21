using CSharpFunctionalExtensions;

namespace Common.Infrastructure.ServiceBus.NServiceBus.RequestResponse;

public class CommandResult : IMessage
{
    public CommandResult(Result result)
    {
        Result = result;
    }

    public Result Result { get; private set; }
}

public class CommandResult<T> : IMessage
{
    public CommandResult(Result<T> result)
    {
        Result = result;
    }

    public Result<T> Result { get; private set; }
}
