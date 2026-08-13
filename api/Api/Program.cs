using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

using Api.Account.Endpoint;
using Api.Auth;
using Api.Common;
using Api.Config;
using Api.OpenApi;
using Api.OpenFGA;
using Api.Org.Endpoint;
using Api.Org.Response;

using Auth0;

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
builder.SetupAuth0();
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

// JasperFx otherwise determines its "application assembly" by walking the call stack
// (Assembly.GetReferencedAssemblies), which isn't supported under Native AOT. Pinning it
// explicitly short-circuits that fallback. See JasperFx.AotSmoke/Program.cs upstream.
JasperFxOptions.RememberedApplicationAssembly = typeof(Program).Assembly;

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
			opts.Projections.Snapshot<Api.Org.Model.OrganizationInvite>(SnapshotLifecycle.Async);
		})
		.UseNpgsqlDataSource()
		.AddAsyncDaemon(DaemonMode.HotCold)
		.IntegrateWithWolverine()
		.UseLightweightSessions();
}

builder.Host.UseWolverine(opts =>
{
	opts.ApplicationAssembly = typeof(Program).Assembly;
	opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
	// OpenFgaApiClient is registered via AddHttpClient<T>, an opaque factory Wolverine's
	// codegen can't inline-construct, so it must be resolved via service location instead.
	opts.CodeGeneration.AlwaysUseServiceLocationFor<OpenFgaApiClient>();
	opts.Policies.UseDurableInboxOnAllListeners();
	opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
});

builder.Services
	.AddOptions<CorsConfig>()
	.BindConfiguration(CorsConfig.SectionKey);
builder.Services.AddCors();

// Build and run the application
var app = builder.Build();
// app.Logger.LogInformation("{}", ((IConfigurationRoot)app.Configuration).GetDebugView());

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi().AllowAnonymous();
}

var corsConfig = app.Services.GetRequiredService<IOptions<CorsConfig>>().Value;
if (corsConfig.AllowedOrigins.Length > 0 || app.Environment.IsDevelopment())
{
	app.UseCors(opts =>
	{
		if (corsConfig.AllowedOrigins.Length > 0)
		{
			opts.WithOrigins(corsConfig.AllowedOrigins);
		}
		else
		{
			opts.AllowAnyOrigin();
		}

		opts.AllowAnyHeader().AllowAnyMethod();
	});
}

app.UseAuthentication();
app.UseAuthorization();

// Prefixed so a reverse proxy can route it to the same origin as the frontend
// (see Caddyfile) - keeps browser calls same-origin, no CORS needed.
var api = app.MapGroup("/api");

var adminEndpoints = api.MapGroup("v1/admin")
	.RequireAuthorization(new OpenFgaAuthorizationRequirement("admin", "system", "core"));

adminEndpoints.MapGroup("organization").MapOrganizationEndpoints();

var organizationEndpoints = api.MapGroup("v1/organization");
organizationEndpoints.MapOrganizationMemberEndpoints();

api.MapGroup("v1/account").MapAccountEndpoints();

var inviteEndpoints = api.MapGroup("v1/invite");
inviteEndpoints.MapInviteEndpoints();

app.MapHealthChecks("/healthz").AllowAnonymous();

app.Logger.LogInformation("Starting API in environment {Environment}", app.Environment.EnvironmentName);

// JasperFx's CLI command dispatch (db-apply, codegen, describe, etc.) uses reflection
// (Assembly.GetReferencedAssemblies) that isn't supported on Native AOT. The published
// container always runs with no arguments, so only reach for it when CLI args are passed,
// i.e. dev-time tooling invocations like `dotnet run -- codegen write`.
if (args.Length > 0)
{
	await RunJasperFxCommandsAotSafe(app, args);
}
else
{
	await app.RunAsync();
}

[UnconditionalSuppressMessage("Trimming", "IL2026",
	Justification = "Reflective JasperFx CLI command dispatch (db-apply, codegen, describe, etc.) is dev-time tooling only, gated behind non-empty CLI args, so it never runs in the published container.")]
[UnconditionalSuppressMessage("AOT", "IL3050",
	Justification = "Same as IL2026: only reachable via dev-time CLI args, never in the published AOT binary's actual run path.")]
static Task RunJasperFxCommandsAotSafe(WebApplication app, string[] args) => app.RunJasperFxCommands(args);

[JsonSerializable(typeof(Organization))]
[JsonSerializable(typeof(PagedResponse<Organization>))]
[JsonSerializable(typeof(OrganizationMembership))]
[JsonSerializable(typeof(IReadOnlyList<OrganizationMembership>))]
[JsonSerializable(typeof(OrganizationDetails))]
[JsonSerializable(typeof(OrganizationMemberInfo))]
[JsonSerializable(typeof(IReadOnlyList<OrganizationMemberInfo>))]
[JsonSerializable(typeof(OrganizationInvite))]
[JsonSerializable(typeof(InviteDetails))]
[JsonSerializable(typeof(Api.Org.Model.OrganizationRole))]
[JsonSerializable(typeof(CreateOrganizationRequest))]
[JsonSerializable(typeof(UpdateOrganizationRequest))]
[JsonSerializable(typeof(AddMemberRequest))]
[JsonSerializable(typeof(UpdateNameRequest))]
[JsonSerializable(typeof(UpdateEmailRequest))]
[JsonSerializable(typeof(UpdatePasswordRequest))]
[JsonSerializable(typeof(CreateInviteRequest))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool?))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}