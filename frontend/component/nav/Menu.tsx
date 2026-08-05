"use client";

import Link from "next/link";
import { useAuth } from "react-oidc-context";
import { useLogout } from "@/component/auth";
import styles from "./Menu.module.css";

export default function Menu() {
	const { user, signinRedirect } = useAuth();
	const logout = useLogout();

	return (
		<div className={styles.container}>
			<h1>App name</h1>

			{user ? (
				<div>
					<Link href="/admin">Admin</Link>
					<div>{user.profile.name}</div>
					<button onClick={logout}>Log out</button>
				</div>
			) : (
				<div>
					<button onClick={() => signinRedirect()}>Sign in</button>
				</div>
			)}
		</div>
	);
}
