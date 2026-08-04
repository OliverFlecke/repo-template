"use client";

import { PropsWithChildren } from "react";
import { AuthProvider, useAuth } from "react-oidc-context";
import oidcConfig from "./config";

/**
 * Wraps the children in the `AuthProvider`.
 */
export default function Provider({ children }: Readonly<PropsWithChildren>) {
	return (
		<AuthProvider {...oidcConfig}>
			<Loader>{children}</Loader>
		</AuthProvider>
	);
}

function Loader({ children }: Readonly<PropsWithChildren>) {
	const { activeNavigator, isLoading, error, ...auth } = useAuth();
	switch (activeNavigator) {
		case "signinSilent":
			return <div>Signing you in...</div>;
		case "signoutRedirect":
			return <div>Signing you out...</div>;
	}

	if (isLoading) {
		return <div>Loading...</div>;
	}

	if (error) {
		console.debug("error auth", error.message, auth);
		return (
			<div>
				Oops... {error.source} caused {error.message}
			</div>
		);
	}

	console.debug("logged in", auth);

	return children;
}
