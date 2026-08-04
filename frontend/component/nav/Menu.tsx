"use client";

import { useAuth } from "react-oidc-context";
import { useLogout } from "@/component/auth";

export default function Menu() {
	const { user, signinRedirect } = useAuth();
	const logout = useLogout();

	return (
		<div>
			<h1>App name</h1>

			{user ? (
				<>
					<div>{user.profile.name}</div>
					<button onClick={logout}>Log out</button>
				</>
			) : (
				<div>
					<button onClick={() => signinRedirect()}>Sign in</button>
				</div>
			)}
		</div>
	);
}
