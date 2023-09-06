using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

using Eventuous;
using Eventuous.Diagnostics;
using Eventuous.Diagnostics.OpenTelemetry;
using Eventuous.EventStore;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Filters;
using Eventuous.Subscriptions.Registrations;

using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.Elasticsearch;

using Pullaroo.Contracts.StronglyTypedIds;
using Pullaroo.Server.Configuration;

namespace Pullaroo.Server;

public static class Registrations
{
    public static void AddEventuous(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<EventStoreSettings>(builder.Configuration.GetSection(nameof(EventStoreSettings)));
        builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection(nameof(MongoSettings)));

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        DefaultEventSerializer.SetDefaultSerializer(new DefaultEventSerializer(jsonOptions));

        builder.Services.AddEventStoreClient(builder.Configuration.GetSection(nameof(EventStoreSettings)).Get<EventStoreSettings>().ConnectionString);
        builder.Services.AddAggregateStore<EsdbEventStore>();

        var mongoSettings = builder.Configuration.GetSection(nameof(MongoSettings)).Get<MongoSettings>();
        RegisterStronglyTypedIdSerializers();
        var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoSettings.ConnectionString);
        mongoClientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        var mongoClient = new MongoClient(mongoClientSettings).GetDatabase(mongoSettings.Database);
        builder.Services.AddSingleton(mongoClient);
        builder.Services.AddCheckpointStore<MongoCheckpointStore>();

        builder.Services.AddSubscription<AllStreamSubscription, AllStreamSubscriptionOptions>(
            "PullarooProjections",
            builder => builder
                .UseCheckpointStore<MongoCheckpointStore>()
                // .AddEventHandler<BookingStateProjection>()
                // .AddEventHandler<ProxySummaryProjection>()
                // .AddEventHandler<MyTeamsProjection>()
                // .AddEventHandler<JamsProjection>()
                // .AddEventHandler<TeamsProjection>()
                // .AddEventHandler<Features.Jams.Projections.UsersProjection>()
                // .AddEventHandler<MyJamsProjection>()
                // .AddEventHandler<Features.Teams.Projections.UsersProjection>()
                // .AddEventHandler<MyTeamsProjection>()

        );
    }
    
    public static void RegisterStronglyTypedIdSerializers()
    {
        var aggregateIdTypes = new[] { typeof(UserId).Assembly, Assembly.GetExecutingAssembly()}
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(StronglyTypedId)))
            .ToList();

        foreach (var type in aggregateIdTypes)
        {
            var serializerType = typeof(StronglyTypedIdBsonSerializer<>).MakeGenericType(type);
            var serializerInstance = (IBsonSerializer)Activator.CreateInstance(serializerType);

            BsonSerializer.RegisterSerializer(type, serializerInstance);
        }
    }

    public static void AddTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<TelemetrySettings>(builder.Configuration.GetSection(nameof(TelemetrySettings)));

        builder.Host.UseSerilog(ConfigureLogging);

        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        TelemetrySettings? telemetrySettings = builder.Configuration
            .GetSection(nameof(TelemetrySettings))
            .Get<TelemetrySettings>();

        ResourceBuilder resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddAttributes(new Dictionary<string, object>()
            {
                ["service.name"] = Assembly.GetEntryAssembly()?.GetName()?.Name?.Replace(".", "_") ?? "Unknown",
                ["deployment.environment"] = telemetrySettings?.Environment ?? "dev",
                ["service.version"] = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "Unknown",
            })
            .AddService(ServiceActivitySource.ActivitySource.Name);

        void ConfigureOtlpExporter(OtlpExporterOptions options)
        {
            options.Endpoint = new Uri(telemetrySettings.ElasticApmAddress);
            options.Headers = $"Authorization=Bearer {telemetrySettings.ElasticApmBearerToken}";
            options.Protocol = OtlpExportProtocol.Grpc;
        }

        builder.Services.AddOpenTelemetry()
            .WithMetrics(
                builder =>
                {
                    builder
                        .SetResourceBuilder(resourceBuilder)
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEventuous()
                        .AddEventuousSubscriptions();

                    if (telemetrySettings?.ElasticApmAddress is not null)
                        builder.AddOtlpExporter((exporter, reader) =>
                        {
                            ConfigureOtlpExporter(exporter);
                            reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                        });
                }
            );

        builder.Services.AddOpenTelemetry()
            .WithTracing(
                builder =>
                {
                    builder
                        .SetSampler(new AlwaysOnSampler())
                        .SetResourceBuilder(resourceBuilder)
                        .AddSource(ServiceActivitySource.ActivitySource.Name)
                        .AddSource(EventuousDiagnostics.ActivitySource.Name)
                        .AddAspNetCoreInstrumentation()
                        .AddGrpcClientInstrumentation()
                        .AddEventuousTracing()
                        .AddMongoDBInstrumentation()
                        .AddHttpClientInstrumentation();

                    if (telemetrySettings?.ElasticApmAddress != null)
                    {
                        builder.AddOtlpExporter(ConfigureOtlpExporter);
                        Console.WriteLine("Using OTLP Exporter.");
                    }

                    if (telemetrySettings?.JaegerAddress != null)
                    {
                        builder.AddJaegerExporter(configure =>
                        {
                            configure.AgentHost = telemetrySettings.JaegerAddress;
                            configure.AgentPort = 6831;
                        });
                    }
                }
            );
    }
    
    public static Serilog.Core.LoggingLevelSwitch LogLevel { get; set; } = new Serilog.Core.LoggingLevelSwitch() { MinimumLevel = Serilog.Events.LogEventLevel.Information };

    private static void ConfigureLogging(HostBuilderContext hostContext, LoggerConfiguration loggerConfiguration)
    {
        Uri? elasticsearchAddress = null;

        TelemetrySettings? telemetrySettings = hostContext.Configuration
            .GetSection(nameof(TelemetrySettings))
            .Get<TelemetrySettings>();
        
        if (!string.IsNullOrWhiteSpace(telemetrySettings?.ElasticsearchAddress))
        {
            elasticsearchAddress = new Uri(telemetrySettings.ElasticsearchAddress);
        }

        if (telemetrySettings is not null)
        {
            LogLevel.MinimumLevel = telemetrySettings.MinimumLogLevel;
        }

        loggerConfiguration
          .Enrich.WithProperty("ServiceName", Assembly.GetEntryAssembly()?.FullName ?? "Unknown")
          .Enrich.FromLogContext()
          .Enrich.WithSpan()
          .Enrich.With<OpenTelemetryEnricher>()
          .MinimumLevel.ControlledBy(LogLevel)
          .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning) // Db command executions are Information level, therefore only include warnings
          .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning) // Context init is Information level
          .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
          .Filter.ByExcluding(Matching.WithProperty("RequestPath", "/health"))
          .Filter.ByExcluding(logEvent => logEvent.Exception is TaskCanceledException)
          .WriteTo.Debug()
          .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug);

        // Setup elasticsearch logging if configured
        if (elasticsearchAddress is not null)
        {
            var sinkOptions = new ElasticsearchSinkOptions(elasticsearchAddress)
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                IndexFormat = "Pullaroo-{0:yyyy.MM}",
                BatchAction = ElasticOpType.Create,
                TypeName = null,
            };
            loggerConfiguration.WriteTo.Elasticsearch(sinkOptions);

            Log.Information("Sending Logs to Elasticsearch");
            Console.WriteLine("Sending Logs to Elasticsearch");
        }
    }
}
