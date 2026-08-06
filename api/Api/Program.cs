using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

using Api.Auth;
using Api.Common;
using Api.Config;
using Api.OpenApi;
using Api.OpenFGA;
using Api.Org.Endpoint;
using Api.Org.Response;

using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;

using Marten;
using Marten.NodaTimePlugin;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
	options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();
});
builder.Services.AddProblemDetails();

builder.SetupAuthentication();
builder.SetupOpenFga();
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
		// ApplicationAssembly is a process-wide static pinned by whichever Wolverine host starts
		// first in the process (see Wolverine's ApplicationAssemblyReuseWarning / GH-3521). Set it
		// explicitly rather than relying on calling-assembly inference, since the test host builds
		// this same Program via reflection (WebApplicationFactory<Program>) and can otherwise pin
		// the wrong assembly, causing handler discovery to silently find nothing ("No routes can
		// be determined for Envelope ...").
		opts.ApplicationAssembly = typeof(Program).Assembly;
		opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
		// OpenFgaApiClient is registered via AddHttpClient<T>, an opaque factory Wolverine's
		// codegen can't inline-construct, so it must be resolved via service location instead.
		opts.CodeGeneration.AlwaysUseServiceLocationFor<OpenFgaApiClient>();
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
	app.MapOpenApi().AllowAnonymous();
	app.UseCors(opts => opts
		.AllowAnyHeader()
		.AllowAnyOrigin()
		.AllowAnyMethod());
}

app.UseAuthentication();
app.UseAuthorization();

var adminEndpoints = app.MapGroup("v1/admin")
	.RequireAuthorization(new OpenFgaAuthorizationRequirement("admin", "system", "core"))
	;

adminEndpoints.MapGroup("organization").MapOrganizationEndpoints();

var organizationEndpoints = app.MapGroup("v1/organization");
organizationEndpoints.MapOrganizationMemberEndpoints();

app.MapHealthChecks("/healthz").AllowAnonymous();

await RunJasperFxCommandsAotSafe(app, args);

[UnconditionalSuppressMessage("Trimming", "IL2026",
	Justification = "Reflective JasperFx CLI command dispatch (db-apply, codegen, describe, etc.) is dev-time tooling only. The published container always runs with no arguments, so this branch just starts the host and never exercises the reflective path.")]
[UnconditionalSuppressMessage("AOT", "IL3050",
	Justification = "Same as IL2026: only reachable via dev-time CLI args, never in the published AOT binary's actual run path.")]
static Task RunJasperFxCommandsAotSafe(WebApplication app, string[] args) => app.RunJasperFxCommands(args);

[JsonSerializable(typeof(Organization))]
[JsonSerializable(typeof(PagedResponse<Organization>))]
[JsonSerializable(typeof(OrganizationMembership))]
[JsonSerializable(typeof(IReadOnlyList<OrganizationMembership>))]
[JsonSerializable(typeof(Api.Org.Model.OrganizationRole))]
[JsonSerializable(typeof(CreateOrganizationRequest))]
[JsonSerializable(typeof(UpdateOrganizationRequest))]
[JsonSerializable(typeof(AddMemberRequest))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool?))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}