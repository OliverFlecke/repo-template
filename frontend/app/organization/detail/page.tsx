"use client";

import { withAuthenticationRequired } from "react-oidc-context";
import OrganizationDetailPage from "@/component/organization/OrganizationDetailPage";

export default withAuthenticationRequired(OrganizationDetailPage, {
	OnRedirecting: () => <div>Redirecting to the login page...</div>,
});
