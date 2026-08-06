using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Api.OpenApi;

public sealed class BearerSecurityDocumentTransformer(
	IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
	/// <inheritdoc/>
	public async Task TransformAsync(
		OpenApiDocument document,
		OpenApiDocumentTransformerContext context,
		CancellationToken cancellationToken)
	{
		var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
		if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
		{
			var securityScheme = new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.Http,
				Scheme = "bearer", // "bearer" refers to the header name here
				In = ParameterLocation.Header,
				BearerFormat = "Json Web Token",
			};
			document.Components ??= new OpenApiComponents();
			document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
			document.Components.SecuritySchemes.Add("Bearer", securityScheme);

			var operations = document.Paths
				.Where(path => path.Key != "healthz")
				.SelectMany(path => (path.Value.Operations ?? []).Values);

			foreach (var operation in operations)
			{
				operation.Security ??= [];
				operation.Security.Add(new OpenApiSecurityRequirement
				{
					[new OpenApiSecuritySchemeReference("Bearer", document)] = [],
				});
			}
		}
	}
}