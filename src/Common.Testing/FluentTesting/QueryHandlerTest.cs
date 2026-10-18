using Common.Testing.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public class QueryHandlerTestResult<THandler, TOutput> :
    IRepsitoryTestResult,
    IAutoMockerTestResult,
    ITestOutput<TOutput>,
    IExceptionTestResult
{
    public QueryHandlerTestResult(
        DatabaseState databaseState,
        AutoMocker autoMocker,
        TOutput output,
        Exception? exceptionThrown = null)
    {
        DatabaseState = databaseState;
        AutoMocker = autoMocker;
        Output = output;
        ExceptionThrown = exceptionThrown;
    }

    public DatabaseState DatabaseState { get; }
    public AutoMocker AutoMocker { get; }
    public TOutput Output { get; }
    public Exception? ExceptionThrown { get; }
}

public static class QueryHandlerTest
{
    /// <summary>
    /// Runs a MediatR handler for a request with any response type, such as <c>Result&lt;T&gt;</c>.
    /// The response type comes from the request, so a test writes <c>.Handle(new GetQuery(id))</c> and nothing more.
    /// </summary>
    public static async Task<QueryHandlerTestResult<THandler, TOutput>> Handle<THandler, TOutput>(
        this HandlerTestSetup<THandler> testSetup,
        IRequest<TOutput> request)
        where THandler : class
    {
        var mocker = new AutoMocker();
        testSetup.ConfigureMocker?.Invoke(mocker);

        using (FakeDatabase.SeedData(testSetup.DatabaseState, testSetup.IsReadOnlyDatabase))
        {
            var handler = mocker.CreateInstance<THandler>();
            var handle = typeof(THandler).GetMethod("Handle", [request.GetType(), typeof(CancellationToken)])
                ?? throw new InvalidOperationException($"{typeof(THandler).Name} has no Handle method for {request.GetType().Name}.");

            var output = await (Task<TOutput>)handle.Invoke(handler, [request, CancellationToken.None])!;

            return new QueryHandlerTestResult<THandler, TOutput>(FakeDatabase.DatabaseState, mocker, output);
        }
    }
}
