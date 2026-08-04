"use client";

import { useAuth } from "react-oidc-context";

export default function Menu() {
	const { user, signinRedirect, signoutRedirect } = useAuth();

	return (
		<div>
			<h1>App name</h1>

			{user ? (
				<>
					<div>{user.profile.name}</div>
					<button onClick={() => signoutRedirect()}>Log out</button>
				</>
			) : (
				<div>
					<button onClick={() => signinRedirect()}>Sign in</button>
				</div>
			)}
		</div>
	);
}
