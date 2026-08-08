import { defineConfig, devices } from "@playwright/test";

// Local, gitignored file for E2E_LOGIN_EMAIL / E2E_LOGIN_PASSWORD; see .env.e2e.example.
try {
	process.loadEnvFile(".env.e2e.local");
} catch {
	// Optional file; fine if it doesn't exist.
}

// Fixed at 3000: the Auth0 tenant's allowed callback URLs are pinned to
// https://localhost:3000, and react-oidc-context derives redirect_uri from
// window.location.origin, so login only completes on that exact origin.
const PORT = 3000;
const baseURL = `https://localhost:${PORT}`;

export default defineConfig({
	testDir: "./e2e",
	fullyParallel: true,
	forbidOnly: !!process.env.CI,
	retries: process.env.CI ? 2 : 0,
	workers: process.env.CI ? 1 : undefined,
	reporter: [["html", { open: "never" }]],
	timeout: 30_000,

	use: {
		baseURL,
		// The dev server uses a locally trusted (mkcert) self-signed certificate.
		ignoreHTTPSErrors: true,
		trace: "on-first-retry",
		screenshot: "only-on-failure",
	},

	projects: [
		{
			name: "chromium",
			use: { ...devices["Desktop Chrome"] },
		},
	],

	webServer: {
		command: `pnpm exec next dev --experimental-https -p ${PORT}`,
		url: baseURL,
		reuseExistingServer: !process.env.CI,
		ignoreHTTPSErrors: true,
		timeout: 60_000,
	},
});
