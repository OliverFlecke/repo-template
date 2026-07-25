export default {
	// Can be extended with multiple OpenAPI spec files to mere into one client.
	input: ["../api/Api/openapi.json"],
	output: {
		entryFile: false,
		path: "api/",
	},
	plugins: [
		{
			dates: "temporal",
			name: "@hey-api/transformers",
		},
		"@tanstack/react-query",
	],
};
