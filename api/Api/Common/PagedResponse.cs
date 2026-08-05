namespace Api.Common;

/// <summary>A page of <typeparamref name="T"/>, along with metadata about the overall result set.</summary>
public sealed record PagedResponse<T>
{
	public required IReadOnlyList<T> Data { get; init; }

	/// <summary>Current page number, starting at 1.</summary>
	/// <example>1</example>
	public required long Page { get; init; }

	/// <summary>Number of items per page.</summary>
	/// <example>100</example>
	public required long PageSize { get; init; }

	/// <summary>Total number of pages.</summary>
	/// <example>10</example>
	public required long PageCount { get; init; }

	/// <summary>Total number of items in the result set.</summary>
	/// <example>1000</example>
	public required long Total { get; init; }
}