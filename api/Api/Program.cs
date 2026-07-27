using System.Reflection;
using System.Text.Json.Serialization;

using Api.Config;
using Api.Org.Endpoint;
using Api.Org.Response;

using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;

using Marten;
using Marten.NodaTimePlugin;

using Microsoft.Extensions.Options;

using NodaTime;
using NodaTime.Serialization.SystemTextJson;

using Npgsql;

using Serilog;
using Serilog.Formatting.Compact;

using Wolverine;
using Wolverine.Marten;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.ConfigureKestrel(x => x.AddServerHeader = false);

builder.Services.AddSerilog(opts =>
	{
		opts.WriteTo.Console();
		opts.WriteTo.File(
			formatter: new CompactJsonFormatter(),
			Path.Combine("logs", "api.log"),
			rollingInterval: RollingInterval.Day,
			fileSizeLimitBytes: 10 * 1024 * 1024,
			retainedFileCountLimit: 2,
			rollOnFileSizeLimit: true,
			shared: true,
			flushToDiskInterval: TimeSpan.FromSeconds(1));
	});

builder.Services.ConfigureHttpJsonOptions(opts =>
{
	opts.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
	opts.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddOpenApi("v1", options =>
{
	options.ShouldInclude = _ => true;
});

builder.Services.AddHealthChecks();

builder.Services
	.AddOptions<DatabaseConfig>()
	.BindConfiguration(DatabaseConfig.SectionKey)
	.ValidateOnStart();
builder.Services.AddSingleton(sp =>
{
	var config = sp.GetRequiredService<IOptions<DatabaseConfig>>();
	return new NpgsqlDataSourceBuilder(config.Value.ConnectionString).Build();
});

builder.Services.CritterStackDefaults(options =>
{
	options.Production.AssertAllPreGeneratedTypesExist = true;
	options.Production.ResourceAutoCreate = AutoCreate.None;
	options.Production.GeneratedCodeMode = TypeLoadMode.Static;

	options.Development.ResourceAutoCreate = AutoCreate.All;
	options.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
});

var generatingOpenApi = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (generatingOpenApi)
{
	builder.Services
		.AddMarten(opts => opts.Connection("Server=.;Database=Foo"))
		.AddAsyncDaemon(DaemonMode.Disabled)
		.UseLightweightSessions();
}
else
{
	builder.Services.AddMarten(opts =>
		{
			opts.UseNodaTime();

			opts.Projections.Snapshot<Api.Org.Model.Organization>(SnapshotLifecycle.Async);
		})
		.UseNpgsqlDataSource()
		.AddAsyncDaemon(DaemonMode.HotCold)
		.IntegrateWithWolverine()
		.UseLightweightSessions();
}

builder.Host.UseWolverine(opts =>
	{
		opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
		opts.Policies.UseDurableInboxOnAllListeners();
		opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
	});

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddCors();
}

// Build and run the application
var app = builder.Build();
// app.Logger.LogInformation("{}", ((IConfigurationRoot)app.Configuration).GetDebugView());

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseCors(opts => opts
		.AllowAnyHeader()
		.AllowAnyOrigin()
		.AllowAnyMethod());
}

app.MapGroup("organization").MapOrganizationEndpoints();
app.MapHealthChecks("/healthz");

await app.RunJasperFxCommands(args);

[JsonSerializable(typeof(Organization))]
[JsonSerializable(typeof(List<Organization>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}