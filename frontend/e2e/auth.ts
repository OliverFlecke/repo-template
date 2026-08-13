import { expect, type Page } from "@playwright/test";

/** Drives Auth0's "New Universal Login" flow (email step, then password step
 * on a separate page) starting from the app's sign-in button. Returns early
 * if the user is already signed in (e.g. an earlier step in the same test
 * already did it) rather than getting stuck waiting for a "Sign in" button
 * that will never appear. */
export async function loginWithAuth0(
	page: Page,
	email: string,
	password: string,
) {
	await page.goto("/");

	const alreadySignedIn = await page
		.getByRole("button", { name: "Log out" })
		.isVisible({ timeout: 5_000 })
		.catch(() => false);
	if (alreadySignedIn) {
		return;
	}

	await page.getByRole("button", { name: "Sign in" }).click();

	await expect(page).toHaveURL(/auth0\.com\/u\/login\/identifier/);
	await page.getByRole("textbox", { name: "Email address" }).fill(email);
	await page.getByRole("button", { name: "Continue", exact: true }).click();

	await expect(page).toHaveURL(/auth0\.com\/u\/login\/password/);
	await page
		.getByRole("textbox", { name: "Password", exact: true })
		.fill(password);
	await page.getByRole("button", { name: "Continue", exact: true }).click();

	// Auth0 may interstitial with a passkey-enrollment prompt after a successful
	// password login; skip it if it shows up.
	const skipPasskeys = page.getByRole("button", {
		name: "Continue without passkeys",
	});
	if (await skipPasskeys.isVisible({ timeout: 5_000 }).catch(() => false)) {
		await skipPasskeys.click();
	}

	await expect(page).toHaveURL("/");
}
