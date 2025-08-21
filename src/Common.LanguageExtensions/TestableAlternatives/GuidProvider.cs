namespace Common.LanguageExtensions.TestableAlternatives;

public sealed class GuidProvider : IDisposable
{
    private static readonly AsyncLocal<Stack<Guid>?> _mockGuids = new();

    private GuidProvider(IReadOnlyCollection<Guid> guids)
    {
        _mockGuids.Value = new Stack<Guid>(guids.Reverse());
    }

    /// <inheritdoc />
    public static Guid NewGuid()
    {
        if (_mockGuids.Value == null)
        {
            return Guid.NewGuid();
        }

        if (_mockGuids.Value.Count == 0)
        {
            throw new ArgumentException("ran out of mock guids");
        }

        return _mockGuids.Value.Pop();
    }

    /// <inheritdoc />
    public static GuidProvider UseMockGuids(IReadOnlyCollection<Guid> guids)
    {
        if (guids == null || guids.Count == 0)
        {
            throw new ArgumentException("cannot use null or empty array of guids");
        }

        return new GuidProvider(guids);
    }

    /// <inheritdoc />
    public static GuidProvider UseMockGuid(Guid guid)
    {
        return new GuidProvider(new[] { guid });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mockGuids.Value = null;
    }
}
