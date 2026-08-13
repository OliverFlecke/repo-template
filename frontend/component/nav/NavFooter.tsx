import { LogIn, LogOut } from "lucide-react";
import type { User } from "oidc-client-ts";
import { Avatar } from "@/ui";
import styles from "./Nav.module.css";
import { NavItem } from "./NavItem";

interface NavFooterProps {
	user: User | null | undefined;
	pathname: string;
	onSignIn: () => void;
	onLogout: () => void;
}

export function NavFooter({
	user,
	pathname,
	onSignIn,
	onLogout,
}: NavFooterProps) {
	if (!user) {
		return (
			<div className={styles.footer}>
				<NavItem
					label="Sign in"
					icon={<LogIn size={20} aria-hidden />}
					onClick={onSignIn}
				/>
			</div>
		);
	}

	const name = user.profile.name ?? user.profile.email ?? "Account";

	return (
		<div className={styles.footer}>
			<NavItem
				href="/account"
				label={name}
				icon={<Avatar src={user.profile.picture} name={name} size="sm" />}
				active={pathname === "/account"}
			/>
			<NavItem
				label="Log out"
				icon={<LogOut size={20} aria-hidden />}
				onClick={onLogout}
			/>
		</div>
	);
}
