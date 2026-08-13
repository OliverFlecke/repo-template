"use client";

import Link from "next/link";
import { useAuth } from "react-oidc-context";
import { useLogout } from "@/component/auth";
import { Avatar } from "@/ui";
import styles from "./UserMenu.module.css";

export default function UserMenu() {
	const { user } = useAuth();
	const logout = useLogout();

	if (!user) return null;

	const name = user.profile.name ?? user.profile.email ?? "Account";

	return (
		<div className={styles.wrapper}>
			<button type="button" className={styles.trigger} aria-haspopup="menu">
				<Avatar src={user.profile.picture} name={name} size="sm" />
				<span className={styles.name}>{name}</span>
			</button>

			<div className={styles.menu} role="menu">
				<Link href="/account" className={styles.menuItem} role="menuitem">
					My account
				</Link>
				<button
					type="button"
					className={styles.menuItem}
					role="menuitem"
					onClick={logout}
				>
					Sign out
				</button>
			</div>
		</div>
	);
}
