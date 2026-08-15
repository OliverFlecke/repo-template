import { defineConfig } from "@hey-api/openapi-ts";

const plugins = [
	{
		dates: "temporal",
		name: "@hey-api/transformers",
	},
	"@tanstack/react-query",
] as const;

// Two separate backends, so two separate clients (each keeps its own baseUrl
// via client.gen.ts) rather than merging into one. Kept as sibling output
// dirs, not nested - the two jobs run in parallel and each cleans its own
// output dir first, which would race if one were nested inside the other.
export default defineConfig([
	{
		input: ["../api/Api/openapi.json"],
		output: {
			entryFile: false,
			path: "api/",
		},
		plugins,
	},
	{
		input: ["../flare-api/openapi.json"],
		output: {
			entryFile: false,
			path: "api-flare/",
		},
		plugins,
	},
]);
