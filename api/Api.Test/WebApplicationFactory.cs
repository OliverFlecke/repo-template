using Marten;
using Marten.Events.Daemon.Coordination;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Testcontainers.PostgreSql;

using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

using Wolverine;

namespace Api.Test;

public class WebApplicationFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
	[ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
	public required InMemoryDatabase Database { get; init; } = null!;

	[ClassDataSource<OpenFgaMock>(Shared = SharedType.PerTestSession)]
	public required OpenFgaMock OpenFga { get; init; } = null!;

	public async Task InitializeAsync()
	{
		_ = Server;

		// MartenDaemonModeIsSolo() (below) swaps in ExplicitProjectionCoordinator, which — unlike
		// the normal auto-starting coordinator — never starts a database's daemon on its own.
		// DaemonForMainDatabase() only constructs the (not-yet-running) daemon; StartAllAsync()
		// is what actually kicks off its shards. Without this, every test races an unstarted
		// daemon and any wait for non-stale projection data times out.
		var daemon = Services.GetRequiredService<IProjectionCoordinator>().DaemonForMainDatabase();
		await daemon.StartAllAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.ConfigureAppConfiguration((host, config) =>
		{
			config.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "DOTNET_ENVIRONMENT", "Production"},
				{ "ASPNET_ENVIRONMENT", "Production"},
				{ "Database:Host", Database.Container.Hostname },
				{ "Database:Port", Database.Container.GetMappedPublicPort(5432).ToString() },
				{ "OpenFga:Host", OpenFga.Host.ToString() },
				{ "OpenFga:StoreId", "test-store" },
				{ "OpenFga:ModelId", "test-model" },
			});
		});

		builder.ConfigureServices((host, services) =>
		{
			services.UseWolverineSoloMode();
			services.DisableAllExternalWolverineTransports();
			services.MartenDaemonModeIsSolo();

			// Bypass the configured Authority so tests can validate tokens against a fixed key
			// instead of needing a live IdP.
			services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
			{
				options.RequireHttpsMetadata = false;
				options.Authority = AuthFactory.Issuer;
				options.Audience = AuthFactory.Audience;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = AuthFactory.TestSigningKey,
					ValidIssuer = AuthFactory.Issuer,
					ValidAudience = AuthFactory.Audience,
				};
			});
		});
	}
}

public class InMemoryDatabase : IAsyncInitializer, IAsyncDisposable
{
	public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:18-alpine")
		.WithTmpfsMount("/var/lib/pg/data")
		.Build();

	public async Task InitializeAsync() => await Container.StartAsync();

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		await Container.DisposeAsync();
	}
}