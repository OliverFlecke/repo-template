namespace Api.Common;

/// <summary>A page of <typeparamref name="T"/>, along with metadata about the overall result set.</summary>
public sealed record PagedResponse<T>
{
	public required IReadOnlyList<T> Data { get; init; }

	public required long PageNumber { get; init; }

	public required long PageSize { get; init; }

	public required long TotalItemCount { get; init; }

	public required long PageCount { get; init; }
}