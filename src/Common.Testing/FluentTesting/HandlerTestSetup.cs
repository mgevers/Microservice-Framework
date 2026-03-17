using Common.Testing.Logging;
using Common.Testing.Persistence;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public class HandlerTestSetup
{
    public HandlerTestSetup(
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<AutoMocker>? configureMocker = null) 
    {
        DatabaseState = databaseState;
        IsReadOnlyDatabase = isReadOnlyDatabase;
        LoggingConfiguration = loggingConfiguration;
        ConfigureMocker = configureMocker;
    }

    public DatabaseState DatabaseState { get; }
    public bool IsReadOnlyDatabase { get; }
    public FakeLoggingConfiguration? LoggingConfiguration { get; }
    public Action<AutoMocker>? ConfigureMocker { get; }
}

public class HandlerTestSetup<THandler> : HandlerTestSetup
{
    public HandlerTestSetup(
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<AutoMocker>? configureMocker = null)
        : base(databaseState, isReadOnlyDatabase, loggingConfiguration, configureMocker)
    {
    }
}

public class HandlerTestSetup<THandler, TResult> : HandlerTestSetup
{
    public HandlerTestSetup(
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        FakeLoggingConfiguration? loggingConfiguration = null,
        Action<AutoMocker>? configureMocker = null)
        : base(databaseState, isReadOnlyDatabase, loggingConfiguration, configureMocker)
    {
    }
}
