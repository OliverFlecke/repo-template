import { useAuth } from "react-oidc-context";
import oidcConfig from "./config";

export function useLogout() {
	const { removeUser } = useAuth();

	return async () => {
		await removeUser();

		window.location.href = `${oidcConfig.authority}/v2/logout?client_id=${oidcConfig.client_id}&returnTo=${encodeURIComponent(window.location.origin)}`;
	};
}