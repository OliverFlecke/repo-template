using System.ComponentModel;

using Acra;

using Microsoft.Extensions.Logging.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Acra.Cli;

static class AcraClientFactory
{
	/// <summary>Reads an optional API key from the ACRA_API_KEY environment variable, for higher
	/// rate limits when actually querying the real data.gov.sg API.</summary>
	public static AcraApiClient Create()
	{
		var config = new AcraConfig { ApiKey = Environment.GetEnvironmentVariable("ACRA_API_KEY") };
		var http = new HttpClient { BaseAddress = config.Host };
		if (!string.IsNullOrEmpty(config.ApiKey))
		{
			http.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
		}

		return new AcraApiClient(NullLogger<AcraApiClient>.Instance, config, http);
	}
}

static class AcraTable
{
	public static Table Build(IReadOnlyList<AcraEntity> entities)
	{
		var table = new Table();
		table.AddColumns("UEN", "Entity Name", "Type", "Status", "Issued", "Street", "Postal Code");

		foreach (var entity in entities)
		{
			table.AddRow(
				entity.Uen,
				entity.EntityName,
				entity.EntityTypeDesc ?? "-",
				entity.UenStatusDesc ?? "-",
				entity.UenIssueDate ?? "-",
				entity.RegStreetName ?? "-",
				entity.RegPostalCode ?? "-"
			);
		}

		return table;
	}
}

sealed class SearchUenSettings : CommandSettings
{
	[CommandArgument(0, "<UEN>")]
	[Description("The exact Unique Entity Number to search for.")]
	public required string Uen { get; init; }
}

sealed class SearchUenCommand : AsyncCommand<SearchUenSettings>
{
	protected override async Task<int> ExecuteAsync(CommandContext context, SearchUenSettings settings, CancellationToken cancellationToken)
	{
		var client = AcraClientFactory.Create();

		var entity = await client.SearchByUen(settings.Uen, cancellationToken);
		if (entity is null)
		{
			AnsiConsole.MarkupLineInterpolated($"[red]No entity found for UEN {settings.Uen}[/]");
			return 1;
		}

		AnsiConsole.Write(AcraTable.Build([entity]));
		return 0;
	}
}

sealed class SearchNameSettings : CommandSettings
{
	[CommandArgument(0, "<NAME>")]
	[Description("The (partial) company name to search for.")]
	public required string Name { get; init; }

	[CommandOption("-l|--limit")]
	[Description("Maximum number of rows to return.")]
	[DefaultValue(20)]
	public int Limit { get; init; } = 20;

	[CommandOption("-o|--offset")]
	[Description("Number of rows to skip, for paging.")]
	public int Offset { get; init; }

	[CommandOption("-s|--status")]
	[Description("Filter by exact registration status, e.g. \"Registered\" or \"Deregistered\".")]
	public string? Status { get; init; }
}

sealed class SearchNameCommand : AsyncCommand<SearchNameSettings>
{
	protected override async Task<int> ExecuteAsync(CommandContext context, SearchNameSettings settings, CancellationToken cancellationToken)
	{
		var client = AcraClientFactory.Create();

		var result = await client.SearchByName(settings.Name, settings.Limit, settings.Offset, settings.Status, cancellationToken);
		if (result.Records.Count == 0)
		{
			AnsiConsole.MarkupLineInterpolated($"[red]No entities found matching '{settings.Name}'[/]");
			return 1;
		}

		AnsiConsole.Write(AcraTable.Build(result.Records));
		AnsiConsole.MarkupLine($"[grey]Showing {result.Records.Count} of {result.Total} matches[/]");
		return 0;
	}
}
