import { expect, test } from "@playwright/test";
import { loginWithAuth0 } from "./auth";

const EMAIL = process.env.E2E_LOGIN_EMAIL;
const PASSWORD = process.env.E2E_LOGIN_PASSWORD;

test("unauthenticated user is redirected to login when visiting /organization", async ({
	page,
}) => {
	await page.goto("/organization");

	await expect(page).toHaveURL(/auth0\.com/);
});

test("user can create an organization and it becomes the current one", async ({
	page,
}) => {
	test.skip(
		!EMAIL || !PASSWORD,
		"E2E_LOGIN_EMAIL and E2E_LOGIN_PASSWORD must be set to run this test, see .env.e2e.example",
	);

	await loginWithAuth0(page, EMAIL as string, PASSWORD as string);

	await page.goto("/organization");
	const name = `E2E Org ${Date.now()}`;
	await page.getByLabel("Name").fill(name);
	await page.getByRole("button", { name: "Create organization" }).click();

	const row = page.getByRole("listitem").filter({ hasText: name });
	await expect(row).toBeVisible();
	await expect(row.getByText("Current")).toBeVisible();
});
