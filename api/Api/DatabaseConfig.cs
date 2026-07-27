namespace Api.Config;

/// <summary>Configuration for connecting to a Postgress database.</summary>
public sealed record DatabaseConfig
{
	/// <summary>Key to use in appsettings.json</summary>
	public const string SectionKey = "Database";

	/// <summary>Host name of the postgres server.</summary>
	/// <example>localhost</example>
	public required string Host { get; init; }

	/// <summary>Port of the postgres server.</summary>
	/// <example>5432</example>
	public required int Port { get; init; }

	/// <summary>Username to connect to the postgres server.</summary>
	/// <example>postgres</example>
	public required string Username { get; init; }

	/// <summary>Password to connect to the postgres server.</summary>
	/// <example>postgres</example>
	public required string Password { get; init; }

	/// <summary>Name of the database to connect to.</summary>
	/// <example>postgres</example>
	public required string Database { get; init; }

	/// <summary>Connection string to connect to a postgres server</summary>
	public string ConnectionString => $"Host={Host};Port={Port};Username={Username};Password={Password};Database={Database}";
}