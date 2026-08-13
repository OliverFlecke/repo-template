import { expect, test } from "@playwright/test";
import { loginWithAuth0 } from "./auth";

const EMAIL = process.env.E2E_LOGIN_EMAIL;
const PASSWORD = process.env.E2E_LOGIN_PASSWORD;

test("user can log in through Auth0 and see their account", async ({
	page,
}) => {
	test.skip(
		!EMAIL || !PASSWORD,
		"E2E_LOGIN_EMAIL and E2E_LOGIN_PASSWORD must be set to run this test, see .env.e2e.example",
	);

	await loginWithAuth0(page, EMAIL as string, PASSWORD as string);

	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
	await expect(
		page.getByRole("link", { name: EMAIL as string }),
	).toBeVisible();
});
