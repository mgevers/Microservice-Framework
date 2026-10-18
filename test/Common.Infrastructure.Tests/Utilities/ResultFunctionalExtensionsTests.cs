using Xunit;
using Ardalis.Result;
using Common.LanguageExtensions.Utilities;

namespace Common.Infrastructure.Tests.Utilities;

public class ResultFunctionalExtensionsTests
{
    [Fact]
    public async Task Tap_WaitsForAsyncDelegate_WhenResultIsSuccess()
    {
        var completed = false;

        await Task.FromResult(Result<int>.Success(1))
            .Tap(async value =>
            {
                await Task.Delay(50);
                completed = true;
            });

        Assert.True(completed);
    }

    [Fact]
    public async Task Tap_DoesNotRunAsyncDelegate_WhenResultIsFailure()
    {
        var ran = false;

        await Task.FromResult(Result<int>.NotFound())
            .Tap(async value =>
            {
                await Task.Delay(1);
                ran = true;
            });

        Assert.False(ran);
    }

    [Fact]
    public async Task TapError_WaitsForAsyncDelegate_WhenResultIsFailure()
    {
        var completed = false;

        await Task.FromResult(Result<int>.NotFound())
            .TapError(async error =>
            {
                await Task.Delay(50);
                completed = true;
            });

        Assert.True(completed);
    }

    [Fact]
    public async Task TapError_DoesNotRunAsyncDelegate_WhenResultIsSuccess()
    {
        var ran = false;

        await Task.FromResult(Result<int>.Success(1))
            .TapError(async error =>
            {
                await Task.Delay(1);
                ran = true;
            });

        Assert.False(ran);
    }
}
