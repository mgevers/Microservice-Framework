using Ardalis.Result;
using Common.Testing.Logging;
using Common.Testing.Persistence;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public class HandlerTestSetup
{
    public HandlerTestSetup(
        DatabaseState databaseState,
        Result? databaseError = null,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<AutoMocker>? configureMocker = null) 
    {
        DatabaseState = databaseState;
        DatabaseError = databaseError;
        LoggingConfiguration = loggingConfiguration;
        ConfigureMocker = configureMocker;
    }

    public DatabaseState DatabaseState { get; }
    public Result? DatabaseError { get; }
    public FakeLoggingConfiguration? LoggingConfiguration { get; }
    public Action<AutoMocker>? ConfigureMocker { get; }
}

public class HandlerTestSetup<THandler> : HandlerTestSetup
{
    public HandlerTestSetup(
        DatabaseState databaseState,
        Result? databaseError = null,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<AutoMocker>? configureMocker = null)
        : base(databaseState, databaseError, loggingConfiguration, configureMocker)
    {
    }
}

public class HandlerTestSetup<THandler, TResult> : HandlerTestSetup
{
    public HandlerTestSetup(
        DatabaseState databaseState,
        Result? databaseError = null,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<AutoMocker>? configureMocker = null)
        : base(databaseState, databaseError, loggingConfiguration, configureMocker)
    {
    }
}
