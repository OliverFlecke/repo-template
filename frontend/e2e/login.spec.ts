import { expect, test } from "@playwright/test";
import { loginWithAuth0 } from "./auth";

const EMAIL = process.env.E2E_LOGIN_EMAIL;

test("user can log in through Auth0 and see their account", async ({
	page,
}) => {
	await loginWithAuth0(page);

	await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
	await expect(page.getByRole("link", { name: EMAIL as string })).toBeVisible();
});
