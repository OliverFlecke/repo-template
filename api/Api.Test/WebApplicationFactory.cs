using Marten;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

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
		});
	}
}

public class InMemoryDatabase : IAsyncInitializer, IAsyncDisposable
{
	public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:18-alpine")
		.WithTmpfsMount("/var/lib/pg/data")
		.Build();

	public async Task InitializeAsync() => await Container.StartAsync();
	public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}