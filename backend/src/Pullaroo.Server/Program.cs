using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using AgGrid.ServerSideRowModel;

using Eventuous;

using FluentValidation;
using FluentValidation.AspNetCore;

using MassTransit;

using MicroElements.Swashbuckle.FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Octokit;

using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;

using Pullaroo.Contracts.StronglyTypedIds;
using Pullaroo.Server;
using Pullaroo.Server.Configuration;

using RequestContext = Pullaroo.Common.RequestContext;

Environment.SetEnvironmentVariable("ASPNETCORE_hostBuilder:reloadConfigOnChange", "false");
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

TypeMap.RegisterKnownEventTypes();

builder.ConfigureMessaging((context, factoryConfigurator) =>
{
    factoryConfigurator.UsePublishFilter(typeof(AuthorisationPublishMiddleware<>), context);
    factoryConfigurator.UseConsumeFilter(typeof(AuthorisationConsumeMiddleware<>), context);
});
builder.Services.Configure<GitHubApiSettings>(builder.Configuration.GetSection(nameof(GitHubApiSettings)));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(nameof(JwtSettings)));
builder.Services.Configure<WebRtcSettings>(builder.Configuration.GetSection(nameof(WebRtcSettings)));
builder.Services.Configure<LiveKitSettings>(builder.Configuration.GetSection(nameof(LiveKitSettings)));

builder.Host.UseOrleans(silo =>
{
    silo.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "dev";
        options.ServiceId = "Pullaroo";
    });

    silo.Configure<EndpointOptions>(options =>
    {
        options.SiloPort = 11111;
        options.GatewayPort = 30000;
    });

    silo.Services.AddSerializer(serializerBuilder =>
    {
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        jsonOptions.IncludeFields = true;
        
        serializerBuilder.AddJsonSerializer(type => type.Namespace.StartsWith("Pullaroo"), jsonOptions);
    });

    silo.UseLocalhostClustering();

    silo.AddMemoryGrainStorage(Constants.LOG_SNAPSHOT_STORE_NAME);
    silo.AddMemoryGrainStorageAsDefault();

    silo.AddReminders();
    silo.UseInMemoryReminderService();

    silo.UseDashboard(options => { });
});

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory());
    options.JsonSerializerOptions.IncludeFields = true;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Pullaroo.Server", Version = "v1" });
    options.CustomSchemaIds(s => s.ToString().Replace("+", ".").Replace("`", "."));
    options.CustomOperationIds(e => $"{e.ActionDescriptor.RouteValues["action"]}");
    
    options.MapType<IReadOnlyCollection<ColumnV0>>(() => 
        new OpenApiSchema 
        { 
            Type = "array", 
            Items = new OpenApiSchema 
            { 
                Type = "object", 
                Properties = 
                {
                    ["id"] = new OpenApiSchema { Type = "string" },
                    ["displayName"] = new OpenApiSchema { Type = "string" },
                    ["field"] = new OpenApiSchema { Type = "string" },
                    ["aggFunc"] = new OpenApiSchema { Type = "string" },
                }
            } 
        });

    options.MapType<IReadOnlyCollection<SortModel>>(() => 
        new OpenApiSchema 
        { 
            Type = "array", 
            Items = new OpenApiSchema 
            { 
                Type = "object",
                Properties = 
                {
                    ["colId"] = new OpenApiSchema { Type = "string" },
                    ["sort"] = new OpenApiSchema { Type = "string" },
                }
            } 
        });

    options.MapType<IReadOnlyCollection<string>>(() => 
        new OpenApiSchema 
        { 
            Type = "array", 
            Items = new OpenApiSchema { Type = "string" } 
        });
    
    options.SchemaFilter<StronglyTypedIdSchemaFilter>();
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddSingleton<IGitHubClient>(_ => new GitHubClient(new Octokit.ProductHeaderValue(nameof(Pullaroo))));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
    
IdentityModelEventSource.ShowPII = true;
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwtOptions) => {

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            // ValidIssuer = jwtOptions.Value.Issuer,
            // ValidAudience = jwtOptions.Value.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.PrivateKey))
        };
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IRequestContext, RequestContext>(_ =>
{
    if (Orleans.Runtime.RequestContext.Get(nameof(RequestContext)) is RequestContext context)
        return context;

    return new RequestContext();
});

builder.AddEventuous();
builder.AddTelemetry();

// builder.Services.AddDbContext<ProjectionContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString(nameof(ProjectionContext)),
//             b => b.MigrationsAssembly(null)).UseLowerCaseNamingConvention());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithOrigins("localhost", "http://localhost:3000", "https://localhost:3000", "http://josh.internal.plexus.gg", "https://josh.internal.plexus.gg")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromDays(1));;
    });
});

WebApplication app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.Use(async (httpContext, next) =>
{
    httpContext.Request.EnableBuffering();
    
    await next(httpContext);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
