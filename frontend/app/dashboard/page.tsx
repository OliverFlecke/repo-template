"use client";

import { withAuthenticationRequired } from "react-oidc-context";

export default withAuthenticationRequired(Dashboard, {
	OnRedirecting: () => <div>Redirecting to the login page...</div>,
});

function Dashboard() {
	return <div>Dashboard</div>;
}
