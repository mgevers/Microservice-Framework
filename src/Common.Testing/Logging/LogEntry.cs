using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common.Testing.Logging;

public class LogEntry(LogLevel logLevel, string template, IDictionary<string, string>? payload)
{
    private readonly static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    public LogEntry(LogLevel logLevel, string template, params object[] payload)
        : this(logLevel, template, ConvertToDictionary(GetTemplateVariables(template), payload))
    {
    }

    public LogLevel LogLevel { get; } = logLevel;

    public string Template { get; } = template;

    public string Message => GetMessageFromTemplateAndPayload(Template, Payload);

    public IDictionary<string, string>? Payload { get; } = payload;

    public static IDictionary<string, string> ConvertToDictionary(IReadOnlyList<string> templateVariables, object[] payloadObjects)
    {
        if (templateVariables.Count != payloadObjects.Length)
        {
            throw new ArgumentException($"payload variable length: {payloadObjects.Length}, must equal template variable count: {templateVariables.Count}");
        }

        var dict = new Dictionary<string, string>();

        for (int i = 0; i < templateVariables.Count; i++)
        {
            var variable = templateVariables[i];
            var payload = payloadObjects[i];

            if (payload is string stringPaylaod)
            {
                dict.Add(variable, stringPaylaod);
            }
            else if (payload is Guid guidPayload)
            {
                dict.Add(variable, guidPayload.ToString());
            }
            else
            {
                dict.Add(variable, JsonConvert.SerializeObject(payload, SerializerSettings));
            }
        }

        return dict;
    }

    public static IReadOnlyList<string> GetTemplateVariables(string template, List<string>? foundVariables = null)
    {
        if (foundVariables == null)
        {
            foundVariables = [];
        }

        var openBraceIndex = template.IndexOf('{');
        var closeBraceIndex = template.IndexOf('}');

        if (openBraceIndex == -1 || closeBraceIndex == -1)
        {
            return foundVariables;
        }

        var variable = template.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1);
        foundVariables.Add(variable);
        var templateWithoutFirstCloseBrace = template.Remove(closeBraceIndex, 1);
        var templateWithoutFirstSetOfBraces = templateWithoutFirstCloseBrace.Remove(openBraceIndex, 1);

        return GetTemplateVariables(templateWithoutFirstSetOfBraces, foundVariables);
    }

    private static string GetMessageFromTemplateAndPayload(string template, IDictionary<string, string>? payload)
    {
        if (payload == null)
        {
            return template;
        }

        var templateVariables = GetTemplateVariables(template);
        var message = template;

        foreach (var variable in templateVariables)
        {
            var toReplace = $"{{{variable}}}";
            var replaceValue = payload[variable];
            message = message.Replace(toReplace, replaceValue);
        }

        return message;
    }
}
