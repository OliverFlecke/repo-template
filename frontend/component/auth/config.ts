import { AuthProviderProps } from "react-oidc-context";
import { User, WebStorageStateStore } from "oidc-client-ts";

const oidcConfig = {
	authority: process.env.NEXT_PUBLIC_AUTHORITY!,
	client_id: process.env.NEXT_PUBLIC_CLIENT_ID!,
	scope: process.env.NEXT_PUBLIC_OAUTH_SCOPES!,

	redirect_uri: typeof window !== "undefined" ? window.location.origin : "",
	automaticSilentRenew: true,
	revokeTokensOnSignout: true,

	stateStore:
		typeof window !== "undefined"
			? new WebStorageStateStore({ store: window.localStorage })
			: undefined,
	userStore:
		typeof window !== "undefined"
			? new WebStorageStateStore({ store: window.localStorage })
			: undefined,

	onSigninCallback: () => {
		const query = new URLSearchParams(window.location.search);
		query.delete("state");
		query.delete("code");

		window.history.replaceState(
			{},
			document.title,
			`${window.location.pathname}?${query.toString()}`,
		);
	},
	onRemoveUser: () => {
		window.location.href = "/";
	},
} satisfies AuthProviderProps;

export default oidcConfig;

/**
 * Get the current authorized user, if any, outside of the React context tree.
 */
export function getUser(): User | null {
	const oidcStorage = localStorage.getItem(
		`oidc.user:${oidcConfig.authority}:${oidcConfig.client_id}`,
	);

	if (!oidcStorage) {
		return null;
	}

	return User.fromStorageString(oidcStorage);
}
