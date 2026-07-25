using System.Text.Json.Serialization;
using Api.Endpoint;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.ConfigureKestrel(x => x.AddServerHeader = false);

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddOpenApi("v1", options =>
{
	options.ShouldInclude = _ => true;
});

builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddCors();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseCors(opts => opts
		.AllowAnyHeader()
		.AllowAnyOrigin()
		.AllowAnyMethod());
}

app.MapGroup("organization").MapOrganizationEndpoints();
app.MapHealthChecks("/healthz");

app.Run();

[JsonSerializable(typeof(Organization))]
[JsonSerializable(typeof(Organization[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}