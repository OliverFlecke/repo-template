using Marten;

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

	public Task InitializeAsync()
	{
		_ = Server;

		return Task.CompletedTask;
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
			});
		});

		builder.ConfigureServices((host, services) =>
		{
			services.UseWolverineSoloMode();
			services.DisableAllWolverineMessagePersistence();
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