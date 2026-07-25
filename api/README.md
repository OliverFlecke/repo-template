# API Template

This is a template for a simple API project using C#.

Note that it is using Native AOT by default, which not all NuGet packages are
compatible with, but it is enabled by default to allow utilizing the extra
performance. It can be disabled by setting `PublishAot` to `false` in `.csproj`.

## Development

To run the API, run `dotnet run` in the root directory of the project. Use
`dotnet watch run` to run the API in watch mode to automatically recompile on
changes.
