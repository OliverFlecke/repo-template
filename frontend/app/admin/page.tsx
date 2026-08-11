"use client";

import { withAuthenticationRequired } from "react-oidc-context";
import OrganizationsPage from "@/component/admin/OrganizationsPage";
import { withRequiredRole } from "@/component/auth/withRequiredRole";

export default withAuthenticationRequired(
	withRequiredRole(OrganizationsPage, "admin"),
	{
		OnRedirecting: () => <div>Redirecting to the login page...</div>,
	},
);
