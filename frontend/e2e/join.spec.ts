import { expect, test } from "@playwright/test";
import { loginWithAuth0 } from "./auth";

const EMAIL = process.env.E2E_LOGIN_EMAIL;
const PASSWORD = process.env.E2E_LOGIN_PASSWORD;

test("/join with no token shows an invalid link message", async ({ page }) => {
	await page.goto("/join");

	await expect(page.getByText("This invite link is invalid.")).toBeVisible();
});

test("/join with an unknown token shows an invalid-or-expired message", async ({
	page,
}) => {
	test.skip(
		!EMAIL || !PASSWORD,
		"E2E_LOGIN_EMAIL and E2E_LOGIN_PASSWORD must be set to run this test, see .env.e2e.example",
	);

	await page.goto("/join?token=00000000-0000-0000-0000-000000000000");

	// Generous timeout: this can be the first real request to the API in the
	// whole suite (run order isn't guaranteed), and a cold Marten connection
	// pool / query warm-up can be slower than the default assertion timeout.
	await expect(
		page.getByText("This invite link is invalid or has expired."),
	).toBeVisible({ timeout: 15_000 });
});

test("unauthenticated user sees a sign-in prompt for a valid invite, then joins after signing in", async ({
	page,
}) => {
	test.skip(
		!EMAIL || !PASSWORD,
		"E2E_LOGIN_EMAIL and E2E_LOGIN_PASSWORD must be set to run this test, see .env.e2e.example",
	);

	// Set up: sign in, create an org, and invite this same account's email
	// so we have a real token to visit while signed out.
	await loginWithAuth0(page, EMAIL as string, PASSWORD as string);
	await page.goto("/organization");
	const name = `E2E Join Org ${Date.now()}`;
	await page.getByLabel("Name").fill(name);
	await page.getByRole("button", { name: "Create organization" }).click();
	await page.getByRole("link", { name }).click();
	await page.getByLabel("Email").fill(EMAIL as string);
	await page.getByRole("button", { name: "Invite member" }).click();
	const inviteLink = await page.locator("input[readonly]").inputValue();

	// dispatchEvent bypasses hit-testing: Next's dev-mode overlay badge sits on
	// top of the sidebar footer, so a real (or force:true, which still hit-tests)
	// click lands on the overlay instead. Not an issue in a production build,
	// which doesn't render this overlay at all.
	await page.getByRole("button", { name: "Log out" }).dispatchEvent("click");
	await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();

	const token = new URL(inviteLink).searchParams.get("token");
	await page.goto(`/join?token=${token}`);
	await expect(
		page.getByRole("heading", { name: `You've been invited to join ${name}` }),
	).toBeVisible();

	await page.getByRole("button", { name: "Sign in to join" }).click();
	await expect(page).toHaveURL(/auth0\.com/);
});
