"use client";

import { useRouter } from "next/navigation";
import type { User } from "oidc-client-ts";
import type { PropsWithChildren } from "react";
import { AuthProvider, useAuth } from "react-oidc-context";
import oidcConfig from "./config";

/**
 * Wraps the children in the `AuthProvider`.
 */
export default function Provider({ children }: Readonly<PropsWithChildren>) {
	const router = useRouter();

	function onSigninCallback(user: User | void) {
		const returnTo = (user?.state as { returnTo?: string } | undefined)
			?.returnTo;
		if (returnTo) {
			router.replace(returnTo);
			return;
		}

		const query = new URLSearchParams(window.location.search);
		query.delete("state");
		query.delete("code");

		window.history.replaceState(
			{},
			document.title,
			`${window.location.pathname}?${query.toString()}`,
		);
	}

	return (
		<AuthProvider {...oidcConfig} onSigninCallback={onSigninCallback}>
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
		return (
			<div>
				Oops... {error.source} caused {error.message}
			</div>
		);
	}

	return children;
}
