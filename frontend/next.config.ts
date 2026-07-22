import type { NextConfig } from "next";

const nextConfig: NextConfig = {
	reactCompiler: true,
	output: "export", // Default to export to enable serving as static assets
};

export default nextConfig;
