using Serilog.Events;

namespace Pullaroo.Server.Configuration;

internal class TelemetrySettings
{
    public string Environment { get; set; } = "dev";

    public string? ElasticsearchAddress { get; set; }
    public string? ElasticApmAddress { get; set; }
    public string? ElasticApmBearerToken { get; set; }
    public string? JaegerAddress { get; set; }

    public LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Information;
}
