"use client";

import { LayoutDashboard, Shield } from "lucide-react";
import { usePathname } from "next/navigation";
import { useAuth } from "react-oidc-context";
import { useLogout } from "@/component/auth";
import { getRoles } from "@/component/auth/withRequiredRole";
import { cx } from "@/ui/util/cx";
import { MobileNavTrigger } from "./MobileNavTrigger";
import styles from "./Nav.module.css";
import { NavFooter } from "./NavFooter";
import { NavItem } from "./NavItem";
import { PinToggle } from "./PinToggle";
import { useMobileNav } from "./useMobileNav";
import { usePinned } from "./usePinned";

export default function Nav() {
	const pathname = usePathname();
	const { user, signinRedirect } = useAuth();
	const logout = useLogout();
	const isAdmin = getRoles(user).includes("admin");

	const [pinned, togglePinned] = usePinned();
	const [mobileOpen, toggleMobileOpen] = useMobileNav(pathname);

	return (
		<>
			<MobileNavTrigger open={mobileOpen} onToggle={toggleMobileOpen} />

			<nav
				id="app-nav"
				aria-label="Main navigation"
				className={cx(
					styles.nav,
					pinned && styles.pinned,
					mobileOpen && styles.mobileOpen,
				)}
			>
				<div className={styles.header}>
					<span className={styles.label}>
						<span className={styles.brand}>
							{process.env.NEXT_PUBLIC_APP_NAME}
						</span>
					</span>
					<PinToggle pinned={pinned} onToggle={togglePinned} />
				</div>

				<ul className={styles.links}>
					<li>
						<NavItem
							href="/dashboard"
							label="Dashboard"
							icon={<LayoutDashboard size={20} aria-hidden />}
							active={pathname === "/dashboard"}
						/>
					</li>
					{isAdmin && (
						<li>
							<NavItem
								href="/admin"
								label="Admin"
								icon={<Shield size={20} aria-hidden />}
								active={pathname === "/admin"}
							/>
						</li>
					)}
				</ul>

				<NavFooter
					user={user}
					pathname={pathname}
					onSignIn={() => signinRedirect()}
					onLogout={logout}
				/>
			</nav>
		</>
	);
}
