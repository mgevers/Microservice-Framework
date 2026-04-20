using System.Diagnostics;

namespace Common.LanguageExtensions.Telemetry;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class TraceActivityAttribute(string? name, ActivityKind kind) : Attribute
{
    public string? Name { get; } = name;

    public ActivityKind Kind { get; } = kind;

    public TraceActivityAttribute()
        : this(null, ActivityKind.Internal)
    {
    }

    public TraceActivityAttribute(string name)
        : this(name, ActivityKind.Internal)
    {
    }
}
