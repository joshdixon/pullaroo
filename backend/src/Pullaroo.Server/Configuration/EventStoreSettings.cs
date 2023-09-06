namespace Pullaroo.Server.Configuration;

internal class EventStoreSettings
{
    public string ConnectionString { get; set; } = "esdb://eventstoredb:2113?tls=false";
}
