using Microsoft.Extensions.Logging;

namespace Common.Testing.Logging;

public record LogEntry(LogLevel LogLevel, string Message)
{
}
