"use client";

import { withAuthenticationRequired } from "react-oidc-context";
import OrganizationsPage from "@/component/organization/OrganizationsPage";

export default withAuthenticationRequired(OrganizationsPage, {
	OnRedirecting: () => <div>Redirecting to the login page...</div>,
});
