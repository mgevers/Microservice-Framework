using Common.Testing.Persistence;
using CSharpFunctionalExtensions;
using NServiceBus.Testing;
using System.Linq.Expressions;

namespace Common.Testing.Assert;

public static class AssertExtensions
{
    public static void DeepEqual<T>(T expected, T actual, bool includeNonPublicProperties = false)
    {
        DeepEqualWithBlacklist(
            expected: expected,
            actual: actual,
            includeNonPublicProperties: includeNonPublicProperties);
    }

    public static void DeepEqualWithBlacklist<T>(
        T expected,
        T actual,
        params Expression<Func<T, object>>[] blacklistProperties)
    {
        DeepEqualWithBlacklist(
            expected: expected,
            actual: actual,
            includeNonPublicProperties: false,
            blacklistProperties: blacklistProperties);
    }

    public static void DeepEqualWithBlacklist<T>(
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

        Xunit.Assert.Equal(expected: expectedJson, actual: actualJson);
    }

    public static void EqualResults<T>(Result<T> expected, Result<T> actual)
    {
        Xunit.Assert.True(expected.IsSuccess == actual.IsSuccess, "results success status were different");

        if (actual.IsSuccess)
        {
            DeepEqual(expected.Value, actual.Value);
        }
        else
        {
            Xunit.Assert.Equal(expected.Error, actual.Error);
        }
    }

    public static void EqualResults(Result expected, Result actual)
    {
        Xunit.Assert.True(expected.IsSuccess == actual.IsSuccess, "results success status were different");
    }

    public static void EqualDatabaseStates(DatabaseState expected, DatabaseState actual)
    {
        var allExpectedEntities = expected.GetAllEntities();
        var allActualEntities = actual.GetAllEntities();

        if (allExpectedEntities.Count != allActualEntities.Count)
        {
            throw new Exception($"assert database state failed: expected {allExpectedEntities.Count} entities, actually found {allActualEntities.Count}");
        }

        foreach (var entityType in expected.GetEntityTypes())
        {
            var expectedEntities = expected.GetEntities(entityType);
            var actualEntities = actual.GetEntities(entityType);

            expectedEntities = expectedEntities.OrderBy(entity => entity.GetHashCode()).ToList();
            actualEntities = actualEntities.OrderBy(entity => entity.GetHashCode()).ToList();

            if (expectedEntities.Count != actualEntities.Count)
            {
                throw new Exception($"assert database state failed: expected {expectedEntities.Count} entities, actually found {actualEntities.Count}");
            }

            for (var i = 0; i < expectedEntities.Count; i++)
            {
                var expectedEntity = expectedEntities[i];
                var actualEntity = actualEntities[i];

                DeepEqual(expected: expectedEntity, actual: actualEntity);
            }
        }
    }

    public static void SuccessfulResult(Result result)
    {
        if (result.IsSuccess)
        {
            Xunit.Assert.True(result.IsSuccess);
        }
        else
        {
            Xunit.Assert.True(result.IsSuccess, result.Error);
        }
    }

    public static void FailureResult<T>(Result<T> result, string error)
    {
        Xunit.Assert.True(result.IsFailure);
        Xunit.Assert.Equal(error, result.Error);
    }

    public static void AssertSentMessages(
        this TestableMessageHandlerContext session,
        IReadOnlyCollection<IMessage> messages)
    {
        DeepEqual(
            expected: messages.Select(message => GetMessageAndType(message)),
            actual: session.SentMessages.Select(sentMessage => GetMessageAndType(sentMessage.Message)));
    }

    public static void AssertPublishedMessages(
        this TestableMessageHandlerContext context,
        IReadOnlyCollection<IMessage> messages)
    {
        DeepEqual(
            expected: messages.Select(message => GetMessageAndType(message)),
            actual: context.PublishedMessages.Select(publishedMessage => GetMessageAndType(publishedMessage.Message)));
    }

    public static void AssertRepliedMessages(
        this TestableMessageHandlerContext context,
        IReadOnlyCollection<IMessage> messages)
    {
        DeepEqual(
            expected: messages.Select(message => GetMessageAndType(message)),
            actual: context.RepliedMessages.Select(repliedMessage => GetMessageAndType(repliedMessage.Message)));
    }

    public static void AssertSentMessages(
        this TestableMessageSession session,
        IReadOnlyCollection<IMessage> messages)
    {
        DeepEqual(
            expected: messages.Select(message => GetMessageAndType(message)),
            actual: session.SentMessages.Select(sentMessage => GetMessageAndType(sentMessage.Message)));
    }

    public static void AssertPublishedMessages(
        this TestableMessageSession context,
        IReadOnlyCollection<IMessage> messages)
    {
        DeepEqual(
            expected: messages.Select(message => GetMessageAndType(message)),
            actual: context.PublishedMessages.Select(publishedMessage => GetMessageAndType(publishedMessage.Message)));
    }

    private static object GetMessageAndType(object message)
    {
        return new
        {
            TypeFullName = message.GetType().FullName,
            Message = message,
        };
    }
}
