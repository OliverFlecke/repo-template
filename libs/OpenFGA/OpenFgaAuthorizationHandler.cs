using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.OpenFGA;

/// <summary>
/// An authorization requirement for OpenFGA authorization. This can be added on any
/// API endpoint to verify the user has the specified relationship on the specified object.
/// The user is extracted from the current authentication context.
/// </summary>
/// <param name="relation">The relationship to check.</param>
/// <param name="object">The object to check.</param>
/// <param name="objectIdKey">The key in the route values to use for the object ID.</param>
public sealed class OpenFgaAuthorizationRequirement(
	string relation,
	string @object,
	string objectIdKey = "id")
	: AuthorizeAttribute, IAuthorizationRequirement, IAuthorizationRequirementData
{
	public string Relation { get; } = relation;
	public string Object { get; } = @object;
	public string ObjectIdKey { get; } = objectIdKey;

	public IEnumerable<IAuthorizationRequirement> GetRequirements() { yield return this; }
}

/// <summary>
/// An authorization handler for OpenFGA authorization. This can be added to the
/// authorization pipeline to verify the user has the specified relationship on the specified object.
/// </summary>
sealed class OpenFgaAuthorizationHandler(
	ILogger<OpenFgaAuthorizationHandler> logger,
	OpenFgaApiClient fga,
	IHttpContextAccessor httpContextAccessor
) : AuthorizationHandler<OpenFgaAuthorizationRequirement>
{
	/// <inheritdoc />
	protected override async Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		OpenFgaAuthorizationRequirement req
	)
	{
		var user = context.User.Identity?.Name;
		logger.LogDebug("Checking authorization for user {User}. Requirement: {Relation} -> {ObjectType}",
			user, req.Relation, req.Object);

		var http = httpContextAccessor.HttpContext;
		if (user is null
			|| http is null
			|| !http.Request.RouteValues.TryGetValue(req.ObjectIdKey, out var objectKey)
		)
		{
			return;
		}
		var objectId = objectKey?.ToString();
		if (objectId is null) { return; }

		logger.LogDebug("Checking authorization for user {User} has {Relation} with {ObjectType}:{ObjectId}",
			user, req.Relation, req.Object, objectId);

		var result = await fga.Check(user, req.Relation, req.Object, objectId.ToString());
		if (result?.Allowed != true)
		{
			logger.LogWarning("User {UserId} does not have relation {Relation} with {Object}:{ObjectId}",
				user, req.Relation, req.Object, objectId);
			return;
		}

		logger.LogTrace("User {UserId} has relation {Relation} with {Object}:{ObjectId}",
				user, req.Relation, req.Object, objectId);

		context.Succeed(req);
	}
}