using Acra.Cli;

using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
	config.SetApplicationName("acra");

	config.AddCommand<SearchUenCommand>("uen")
		.WithDescription("Look up a company by its exact UEN.")
		.WithExample("uen", "201912345A");

	config.AddCommand<SearchNameCommand>("name")
		.WithDescription("Search for companies by (partial) name.")
		.WithExample("name", "ACME")
		.WithExample("name", "ACME", "--limit", "5");
});

return await app.RunAsync(args);
