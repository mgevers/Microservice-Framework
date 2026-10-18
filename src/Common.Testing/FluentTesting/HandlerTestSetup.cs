using Common.Testing.Persistence;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public class HandlerTestSetup
{
    public HandlerTestSetup(
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        Action<AutoMocker>? configureMocker = null) 
    {
        DatabaseState = databaseState;
        IsReadOnlyDatabase = isReadOnlyDatabase;
        ConfigureMocker = configureMocker;
    }

    public DatabaseState DatabaseState { get; }
    public bool IsReadOnlyDatabase { get; }
    public Action<AutoMocker>? ConfigureMocker { get; }
}

public class HandlerTestSetup<THandler> : HandlerTestSetup
{
    public HandlerTestSetup(
        DatabaseState databaseState,
        bool isReadOnlyDatabase,
        Action<AutoMocker>? configureMocker = null)
        : base(databaseState, isReadOnlyDatabase, configureMocker)
    {
    }
}
