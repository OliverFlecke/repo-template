"use client";

import { useAuth } from "react-oidc-context";
import { Button } from "@/ui/Button/Button";
import UserMenu from "./UserMenu";
import styles from "./Menu.module.css";

export default function Menu() {
	const { user, signinRedirect } = useAuth();

	return (
		<div className={styles.container}>
			<h1>{process.env.NEXT_PUBLIC_APP_NAME}</h1>

			{user ? (
				<UserMenu />
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
