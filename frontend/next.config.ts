import type { NextConfig } from "next";

export default {
	reactCompiler: true,
	// Default to export to enable serving as static assets
	output: process.env.OUTPUT_MODE === "standalone" ? "standalone" : "export",
	devIndicators: { position: "bottom-right" },
} satisfies NextConfig;
