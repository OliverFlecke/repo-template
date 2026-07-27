using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Api.Test;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
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
			Console.WriteLine($"Attempting to set configuration: {Database.Container.GetMappedPublicPort(5432)}");
			config.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "DOTNET_ENVIRONMENT", "Production"},
				{ "ASPNET_ENVIRONMENT", "Production"},
				{ "Database:Host", Database.Container.Hostname },
				{ "Database:Port", Database.Container.GetMappedPublicPort(5432).ToString() },
			});
		});
	}
}

public class InMemoryDatabase : IAsyncInitializer, IAsyncDisposable
{
	public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:18-alpine")
		.Build();

	public async Task InitializeAsync() => await Container.StartAsync();
	public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}