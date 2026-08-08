"use client";

import Link from "next/link";
import { useAuth } from "react-oidc-context";
import { useLogout } from "@/component/auth";
import { Button } from "@/ui/Button/Button";
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
					<Button variant="text" onClick={logout}>
						Log out
					</Button>
				</div>
			) : (
				<div>
					<Button variant="text" onClick={() => signinRedirect()}>
						Sign in
					</Button>
				</div>
			)}
		</div>
	);
}
