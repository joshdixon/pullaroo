namespace Pullaroo.Server.Configuration;

internal class MongoSettings
{
    public string? ConnectionString { get; set; } = "mongodb://mongo:27017";
    public string? Database { get; set; } = "Pullaroo";
}
