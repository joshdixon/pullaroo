using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using MassTransit;
using MassTransit.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Pullaroo.Contracts;
using Pullaroo.Contracts.StronglyTypedIds;

namespace Pullaroo.Common;

file class MassTransitSettings
{
    public string RabbitMqAddress { get; set; } = "rabbitmq://guest:guest@rabbitmq/";
    public string MongoDbAddress { get; set; } = "mongodb://mongo:27017";
    public string DatabaseName { get; set; } = "masstransit";
    public int InlineThreshold { get; set; } = 4096;
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromSeconds(60);
}

public static class MessagingConfiguration
{
    public static HashSet<Type> ConsumerMessageTypes { get; } = new();

    public static void ConfigureMessaging(this WebApplicationBuilder builder,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configure)
    {
        builder.Services.Configure<MassTransitSettings>(builder.Configuration.GetSection(nameof(MassTransitSettings)));

        // Consumers are added here 
        Type[] consumerAndDefinitions = Assembly.GetEntryAssembly()?
            .GetTypes()
            .Where(x => x.GetInterfaces().Any())
            .Where(x => x.ContainsGenericParameters == false)
            .Where(x => MassTransit.Metadata.RegistrationMetadata.IsConsumerOrDefinition(x))
            .ToArray() ?? Array.Empty<Type>();

        // Get all types that implement IRequest<T>
        Type[] requestContractTypes = typeof(IRequest<>).Assembly
            .GetTypes()
            .Where(x => x.GetInterfaces().Any())
            .Where(x => x.ContainsGenericParameters == false)
            .Where(x => x.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToArray();

        // Add MassTransit
        builder.Services.AddMassTransit(cfg =>
        {
            cfg.AddConsumers(consumerAndDefinitions);

            // Add request clients here
            foreach (Type requestContractType in requestContractTypes)
            {
                cfg.AddRequestClient(requestContractType, TimeSpan.FromMinutes(2));
            }

            // cfg.AddDelayedMessageScheduler();

            cfg.UsingRabbitMq((context, factoryConfigurator) =>
            {
                configure?.Invoke(context, factoryConfigurator);
                ConfigureRabbitMqBus(context, factoryConfigurator);
            });

            // cfg.AddRider(rider =>
            // {
            //     rider.AddConsumer<KafkaMessageConsumer>();
            //     rider.UsingKafka((context, k) =>
            //     {
            //         k.Host("localhost:9092");
            //         k.TopicEndpoint<KafkaMessage>("topic-name", "consumer-group-name", e =>
            //         {
            //             e.ConfigureConsumer<KafkaMessageConsumer>(context);
            //         });
            //     });
            // });
        });

        builder.Services.AddMediator(cfg =>
        {
            cfg.AddConsumers(consumerAndDefinitions);

            cfg.ConfigureMediator((context, mcfg) => { ConfigurePipelines(context, mcfg); });
        });

        // Add default configuration for receive endpoints. Make queues quorum and durable.
        // builder.Services.AddTransient<IConfigureReceiveEndpoint, QuorumEndpointConfigurator>();

        //builder.Services.AddScoped<ISecurityContext, SecurityContext>();

        //builder.Services.AddTransient(typeof(IValidationFailurePipe<>), typeof(ValidationFailurePipe<>));

        builder.Services.AddTransient<IMediator, Mediator>();

        // builder.Services.AddSingleton<IMessageDataRepository>(provider =>
        // {
        //     MassTransitSettings massTransitSettings =
        //         provider.GetRequiredService<IOptions<MassTransitSettings>>().Value;
        //     return new MongoDbMessageDataRepository(massTransitSettings.MongoDbAddress,
        //         massTransitSettings.DatabaseName);
        // });
    }

    private static void ConfigureRabbitMqBus(IBusRegistrationContext context,
        IRabbitMqBusFactoryConfigurator factoryConfigurator)
    {
        MassTransitSettings massTransitSettings = context.GetRequiredService<IOptions<MassTransitSettings>>().Value;
        string assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ??
                              throw new Exception("Could not get assembly name");

        factoryConfigurator.Host(new Uri(massTransitSettings.RabbitMqAddress), assemblyName, h =>
        {
            if (massTransitSettings.RabbitMqAddress.Contains("rabbitmqs"))
            {
                h.UseSsl(s => { s.Protocol = System.Security.Authentication.SslProtocols.Tls12; });
            }
        });

        factoryConfigurator.UseDelayedMessageScheduler();
        ConfigurePipelines(context, factoryConfigurator);

        factoryConfigurator.UseInstrumentation(serviceName: assemblyName);

        // factoryConfigurator.UseMessageData(context.GetRequiredService<IMessageDataRepository>());
        // MessageDataDefaults.AlwaysWriteToRepository = false;
        // MessageDataDefaults.Threshold = massTransitSettings.InlineThreshold;
        // MessageDataDefaults.TimeToLive = massTransitSettings.TimeToLive;

        factoryConfigurator.ConfigureEndpoints(context, new DefaultEndpointNameFormatter($"{assemblyName}.", true));
    }

    private static void ConfigurePipelines<T>(IServiceProvider provider, T factoryConfigurator)
        where T : IPublishPipelineConfigurator, IConsumePipeConfigurator
    {
        //factoryConfigurator.UsePublishFilter(typeof(PublishSecurityFilter<>), provider);
        //factoryConfigurator.UseConsumeFilter(typeof(ConsumeSecurityFilter<>), provider);
        // factoryConfigurator.UseConsumeFilter(typeof(FluentValidationFilter<>), provider);

        var jsonOptions = new JsonSerializerOptions(SystemTextJsonMessageSerializer.Options);
        jsonOptions.IncludeFields = true;
        jsonOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        SystemTextJsonMessageSerializer.Options = jsonOptions;
    }
}
