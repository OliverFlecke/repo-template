using System.Text.Json.Serialization;
using Api.Config;
using Api.Endpoint;
using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Events.Daemon;
using Marten;
using Marten.NodaTimePlugin;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Wolverine;
using Wolverine.Marten;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.ConfigureKestrel(x => x.AddServerHeader = false);

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

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddCors();
}

var databaseConfig = builder.Configuration
	.GetRequiredSection(DatabaseConfig.SectionKey)
	.Get<DatabaseConfig>()!;
builder.Services.AddNpgsqlDataSource(databaseConfig.ConnectionString);

builder.Services.CritterStackDefaults(options =>
{
	options.Production.AssertAllPreGeneratedTypesExist = true;
	options.Production.ResourceAutoCreate = AutoCreate.None;
	options.Production.GeneratedCodeMode = TypeLoadMode.Static;

	options.Development.ResourceAutoCreate = AutoCreate.All;
	options.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
});

builder.Services.AddMarten(opts =>
	{
		opts.UseNodaTime();
	})
	.UseNpgsqlDataSource()
	.AddAsyncDaemon(DaemonMode.HotCold)
	.UseLightweightSessions()
	.IntegrateWithWolverine();

builder.Host.UseWolverine(opts =>
	{
		opts.Policies.UseDurableInboxOnAllListeners();
		opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
	});

// Build and run the application
var app = builder.Build();

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

await app.RunAsync();

[JsonSerializable(typeof(Organization))]
[JsonSerializable(typeof(Organization[]))]
[JsonSerializable(typeof(List<Organization>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}