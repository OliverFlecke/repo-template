"use client";

import { Building2, LayoutDashboard, Server, Shield } from "lucide-react";
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
import { PropsWithChildren } from "react";

export default function Nav({ children }: Readonly<PropsWithChildren>) {
	const { user, signinRedirect } = useAuth();
	const logout = useLogout();
	const isAdmin = getRoles(user).includes("admin");

	const pathname = usePathname();

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
					<PinToggle pinned={pinned} onToggle={togglePinned} />

					<span className={styles.label}>
						<span className={styles.brand}>
							{process.env.NEXT_PUBLIC_APP_NAME}
						</span>
					</span>
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
					<li>
						<NavItem
							href="/organization"
							label="Organizations"
							icon={<Building2 size={20} aria-hidden />}
							active={pathname.startsWith("/organization")}
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
					{isAdmin && (
						<li>
							<NavItem
								href="/flare"
								label="Flare"
								icon={<Server size={20} aria-hidden />}
								active={pathname === "/flare"}
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

			<div className={cx(styles.content, pinned && styles.pinned)}>
				{children}
			</div>
		</>
	);
}
