import { expect, test } from "@playwright/test";
import { loginWithAuth0 } from "./auth";

const EMAIL = process.env.E2E_LOGIN_EMAIL;

test("/join with no token shows an invalid link message", async ({ page }) => {
	await page.goto("/join");

	await expect(page.getByText("This invite link is invalid.")).toBeVisible();
});

test("/join with an unknown token shows an invalid-or-expired message", async ({
	page,
}) => {
	await loginWithAuth0(page);
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
	// Set up: sign in, create an org, and invite this same account's email
	// so we have a real token to visit while signed out.
	await loginWithAuth0(page);
	await page.goto("/organization");
	const name = `E2E Join Org ${Date.now()}`;
	await page.getByLabel("Name").fill(name);
	await page.getByRole("button", { name: "Create organization" }).click();
	await page.getByRole("link", { name }).click();
	await page.getByLabel("Email").fill(EMAIL as string);
	await page.getByRole("button", { name: "Invite member" }).click();
	const inviteLink = await page.locator("input[readonly]").inputValue();

	// Clear the local session directly instead of clicking "Log out": that
	// goes through a real Auth0 redirect round-trip, which can race with
	// automaticSilentRenew and land back with a fresh, still-authenticated
	// session instead of a logged-out one. All this test needs is to view the
	// invite link as an unauthenticated visitor, which this achieves directly.
	const token = new URL(inviteLink).searchParams.get("token");
	await page.evaluate(() => localStorage.clear());
	await page.goto(`/join?token=${token}`);
	await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
	await expect(
		page.getByRole("heading", { name: `You've been invited to join ${name}` }),
	).toBeVisible();

	// Auth0 still has an active SSO session from loginWithAuth0 above (same
	// account, same browser), so this completes silently rather than showing
	// an interactive login page. What actually matters is the `state.returnTo`
	// wiring: without it, the fixed redirect_uri would drop the user back at
	// "/" and lose the token, instead of back on this same invite page.
	await page.getByRole("button", { name: "Sign in to join" }).click();
	await expect(page).toHaveURL(`/join?token=${token}`, { timeout: 15_000 });
	await expect(
		page.getByRole("heading", { name: `You've been invited to join ${name}` }),
	).toBeVisible();
});
