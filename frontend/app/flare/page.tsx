"use client";

import { withAuthenticationRequired } from "react-oidc-context";
import { withRequiredRole } from "@/component/auth/withRequiredRole";
import FlareStatusPage from "@/component/flare/FlareStatusPage";

export default withAuthenticationRequired(
	withRequiredRole(FlareStatusPage, "admin"),
	{
		OnRedirecting: () => <div>Redirecting to the login page...</div>,
	},
);
