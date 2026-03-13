using System.Linq.Expressions;

namespace Common.LanguageExtensions.Utilities;

public static class BooleanUtilities
{
    public static bool IsDeepEqual<T>(
    T expected,
    T actual,
    bool includeNonPublicProperties = false)
    {
        string expectedJson = DeepEqualSerializer.SerializeObject(
            value: expected,
            blacklistProperties: [],
            includeNonPublicProperties: includeNonPublicProperties);

        string actualJson = DeepEqualSerializer.SerializeObject(
            value: actual,
            blacklistProperties: [],
            includeNonPublicProperties: includeNonPublicProperties);

        return expectedJson == actualJson;
    }

    public static bool IsDeepEqualWithBlacklist<T>(
        T expected,
        T actual,
        bool includeNonPublicProperties,
        params Expression<Func<T, object>>[] blacklistProperties)
    {
        string expectedJson = DeepEqualSerializer.SerializeObject(
            value: expected,
            blacklistProperties: blacklistProperties,
            includeNonPublicProperties: includeNonPublicProperties);

        string actualJson = DeepEqualSerializer.SerializeObject(
            value: actual,
            blacklistProperties: blacklistProperties,
            includeNonPublicProperties: includeNonPublicProperties);

        return expectedJson == actualJson;
    }
}
