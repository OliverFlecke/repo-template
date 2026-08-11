import { AuthProviderProps } from "react-oidc-context";
import { User, WebStorageStateStore } from "oidc-client-ts";

const oidcConfig = {
	authority: process.env.NEXT_PUBLIC_AUTHORITY!,
	client_id: process.env.NEXT_PUBLIC_CLIENT_ID!,
	scope: process.env.NEXT_PUBLIC_OAUTH_SCOPES!,
	extraQueryParams: { audience: process.env.NEXT_PUBLIC_AUTH_AUDIENCE! },

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

const ROLES_CLAIM = `${process.env.NEXT_PUBLIC_AUTH_ROLES_NAMESPACE}/roles`;

/**
 * Roles from the access token's custom `<namespace>/roles` claim (set by an
 * Auth0 Action - see repo docs). UI convenience only, not a security
 * boundary: the API independently enforces access via OpenFGA regardless of
 * what this returns.
 */
export function getRoles(user: User | null | undefined): string[] {
	if (!user?.access_token) {
		return [];
	}

	try {
		const [, payload] = user.access_token.split(".");
		const claims = JSON.parse(
			atob(payload.replace(/-/g, "+").replace(/_/g, "/")),
		);
		return claims[ROLES_CLAIM] ?? [];
	} catch {
		return [];
	}
}
