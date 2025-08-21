using Common.LanguageExtensions.Utilities;
using System.Text.RegularExpressions;

namespace Common.Infrastructure.ServiceBus.NServiceBus.Configuration
{
    internal static class NServiceBusNameShortener
    {
        public static string Shorten(Type messageType)
        {
            const int maxLength = 50;
            string messageTypeFullName = messageType.FullName!;

            string messageTypeNameWithoutEventSuffix = Regex.Replace(
                input: messageTypeFullName,
                pattern: "Event$",
                replacement: string.Empty);

            if (messageTypeNameWithoutEventSuffix.Length <= maxLength)
            {
                return messageTypeNameWithoutEventSuffix;
            }
            else
            {
                const int hashLegnth = 5;

                string messageTypeFullNameHash = HashUtilities.ComputeSHA256Hash(messageTypeFullName);

                string tuncatedMessageType = messageTypeFullName.Substring(startIndex: 0, length: maxLength - hashLegnth - 1);
                string trucatedMessageHash = messageTypeFullNameHash.Substring(startIndex: 0, length: hashLegnth);

                return $"{tuncatedMessageType}-{trucatedMessageHash}";
            }
        }
    }
}
