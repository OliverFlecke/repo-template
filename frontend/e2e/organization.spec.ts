import { expect, test } from "@playwright/test";
import { loginWithAuth0 } from "./auth";

test("unauthenticated user is redirected to login when visiting /organization", async ({
	page,
}) => {
	await page.goto("/organization");

	await expect(page).toHaveURL(/auth0\.com/);
});

test("unauthenticated user is redirected to login when visiting an organization's detail page", async ({
	page,
}) => {
	await page.goto(
		"/organization/detail?id=00000000-0000-0000-0000-000000000000",
	);

	await expect(page).toHaveURL(/auth0\.com/);
});

test("user can create an organization and it becomes the current one", async ({
	page,
}) => {
	await loginWithAuth0(page);

	await page.goto("/organization");
	const name = `E2E Org ${Date.now()}`;
	await page.getByLabel("Name").fill(name);
	await page.getByRole("button", { name: "Create organization" }).click();

	const row = page.getByRole("listitem").filter({ hasText: name });
	await expect(row).toBeVisible();
	await expect(row.getByText("Current")).toBeVisible();
});

test("user can view an organization's members and invite a new one", async ({
	page,
}) => {
	await loginWithAuth0(page);

	await page.goto("/organization");
	const name = `E2E Detail Org ${Date.now()}`;
	await page.getByLabel("Name").fill(name);
	await page.getByRole("button", { name: "Create organization" }).click();
	await page.getByRole("link", { name }).click();

	await expect(page.getByRole("heading", { name })).toBeVisible();
	await expect(
		page.getByRole("listitem").filter({ hasText: "Admin" }),
	).toBeVisible();

	await page.getByLabel("Email").fill("invitee@example.com");
	await page.getByRole("button", { name: "Invite member" }).click();

	const inviteLink = page.locator("input[readonly]");
	await expect(inviteLink).toHaveValue(/\/join\?token=/);
});
