import type { User } from "oidc-client-ts";
import { getUser } from "./config";

/**
 * Check if the current user has a specific role.
 **/
function userHasRole(...roles: string[]): boolean {
	const user = getUser();
	if (!user) {
		return false;
	}

	const userRoles = getRoles(user);
	return roles.some((r) => userRoles.includes(r));
}

/**
 * Higher-order component that only renders the wrapped component if the
 * current user has the specified role(s).
 **/
export const withRequiredRole = <P extends object>(
	Component: React.ComponentType<P>,
	...roles: string[]
): React.FC<P> => {
	const C: React.FC<P> = (props) => {
		return userHasRole("admin", ...roles) ? (
			<Component {...props} />
		) : (
			<div>You do not have access to this page.</div>
		);
	};

	C.displayName = `withRequiredRole(${Component.displayName || Component.name})`;

	return C;
};

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
