"use client";

import { useAuth, withAuthenticationRequired } from "react-oidc-context";
import { getRoles } from "@/component/auth/config";
import OrganizationsPage from "@/component/admin/OrganizationsPage";

function AdminGuard() {
	const { user } = useAuth();

	if (!getRoles(user).includes("admin")) {
		return <div>You don&apos;t have access to this page.</div>;
	}

	return <OrganizationsPage />;
}

export default withAuthenticationRequired(AdminGuard, {
	OnRedirecting: () => <div>Redirecting to the login page...</div>,
});
