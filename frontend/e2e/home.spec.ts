import { expect, test } from "@playwright/test";

test("main page loads", async ({ page }) => {
	await page.goto("/");

	await expect(
		page.getByRole("heading", { name: "Template App" }),
	).toBeVisible();
	await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
});

test("unauthenticated user is redirected to login when visiting the dashboard", async ({
	page,
}) => {
	await page.goto("/dashboard");

	await expect(page).toHaveURL(/auth0\.com/);
});
