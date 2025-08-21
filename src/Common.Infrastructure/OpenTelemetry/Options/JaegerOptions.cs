namespace Common.Infrastructure.OpenTelemetry.Options;

public class JaegerOptions
{
    public string ServiceName { get; set; } = "not specified";

    public string AgentHost { get; set; } = "jaeger";

    public int AgentPort { get; set; } = 5672;
}
