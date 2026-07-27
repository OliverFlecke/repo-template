using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Core.Interfaces;

namespace Api.Test;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
	public Task InitializeAsync()
	{
		_ = Server;

		return Task.CompletedTask;
	}
}