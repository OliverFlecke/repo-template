import { expect, test } from "@playwright/test";

const EMAIL = process.env.E2E_LOGIN_EMAIL;
const PASSWORD = process.env.E2E_LOGIN_PASSWORD;

test("user can log in through Auth0 and see their account", async ({
	page,
}) => {
	test.skip(
		!EMAIL || !PASSWORD,
		"E2E_LOGIN_EMAIL and E2E_LOGIN_PASSWORD must be set to run this test, see .env.e2e.example",
	);
	const email = EMAIL as string;
	const password = PASSWORD as string;

	await page.goto("/");
	await page.getByRole("button", { name: "Sign in" }).click();

	// Auth0's "New Universal Login" is a two-step flow: email first, then password
	// on a separate page.
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
	const userMenu = page.getByRole("button", { name: email });
	await expect(userMenu).toBeVisible();

	await userMenu.hover();
	await expect(page.getByRole("menuitem", { name: "Sign out" })).toBeVisible();
});
