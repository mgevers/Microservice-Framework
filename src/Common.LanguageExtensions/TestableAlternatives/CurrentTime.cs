namespace Common.LanguageExtensions.TestableAlternatives
{
    public sealed class CurrentTime : IDisposable
    {
        private static readonly AsyncLocal<DateTime?> MockUtcNow = new();

        private CurrentTime(DateTime mockUtcNow) : base()
        {
            MockUtcNow.Value = mockUtcNow;
        }

        public static DateTime UtcNow => MockUtcNow.Value ?? DateTime.UtcNow;

        public static CurrentTime UseMotckUtcNow(DateTime mockUtcNow)
        {
            return new CurrentTime(mockUtcNow);
        }

        public void Dispose()
        {
            MockUtcNow.Value = null;
        }
    }

    public sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return CurrentTime.UtcNow;
        }
    }
}
