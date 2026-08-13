"use client";

import {
	ChevronRight,
	LayoutDashboard,
	LogIn,
	LogOut,
	Menu as MenuIcon,
	Shield,
	X,
} from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { type ReactNode, useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { useLogout } from "@/component/auth";
import { getRoles } from "@/component/auth/withRequiredRole";
import { Avatar } from "@/ui";
import { IconButton } from "@/ui/IconButton/IconButton";
import { cx } from "@/ui/util/cx";
import styles from "./Nav.module.css";

const PINNED_KEY = "nav-pinned";

export default function Nav() {
	const pathname = usePathname();
	const { user, signinRedirect } = useAuth();
	const logout = useLogout();
	const isAdmin = getRoles(user).includes("admin");
	const name = user?.profile.name ?? user?.profile.email ?? "Account";

	const [pinned, setPinned] = useState(false);
	const [mobileOpen, setMobileOpen] = useState(false);

	// Read the persisted preference after mount to avoid an SSR/client mismatch.
	useEffect(() => {
		try {
			setPinned(localStorage.getItem(PINNED_KEY) === "true");
		} catch {
			// Storage may be unavailable (e.g. sandboxed iframes); fall back to unpinned.
		}
	}, []);

	const togglePinned = () => {
		const next = !pinned;
		try {
			localStorage.setItem(PINNED_KEY, String(next));
		} catch {
			// Storage may be unavailable; the preference just won't persist.
		}
		setPinned(next);
	};

	// biome-ignore lint/correctness/useExhaustiveDependencies: only re-run on navigation
	useEffect(() => {
		setMobileOpen(false);
	}, [pathname]);

	useEffect(() => {
		if (!mobileOpen) return;

		document.body.style.overflow = "hidden";
		const onKeyDown = (e: KeyboardEvent) => {
			if (e.key === "Escape") setMobileOpen(false);
		};
		window.addEventListener("keydown", onKeyDown);

		return () => {
			document.body.style.overflow = "";
			window.removeEventListener("keydown", onKeyDown);
		};
	}, [mobileOpen]);

	return (
		<>
			<IconButton
				className={styles.hamburger}
				aria-label={mobileOpen ? "Close navigation" : "Open navigation"}
				aria-expanded={mobileOpen}
				aria-controls="app-nav"
				onClick={() => setMobileOpen((open) => !open)}
			>
				{mobileOpen ? <X aria-hidden /> : <MenuIcon aria-hidden />}
			</IconButton>

			{mobileOpen && (
				<div
					className={styles.backdrop}
					onClick={() => setMobileOpen(false)}
					aria-hidden="true"
				/>
			)}

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
					<IconButton
						size="sm"
						className={cx(styles.pinButton, pinned && styles.pinButtonActive)}
						aria-pressed={pinned}
						aria-label={pinned ? "Collapse sidebar" : "Keep sidebar expanded"}
						title={pinned ? "Collapse sidebar" : "Keep sidebar expanded"}
						onClick={togglePinned}
					>
						<ChevronRight size={16} aria-hidden />
					</IconButton>
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

				<div className={styles.footer}>
					{user ? (
						<>
							<NavItem
								href="/account"
								label={name}
								icon={
									<Avatar src={user.profile.picture} name={name} size="sm" />
								}
								active={pathname === "/account"}
							/>
							<NavItem
								label="Log out"
								icon={<LogOut size={20} aria-hidden />}
								onClick={logout}
							/>
						</>
					) : (
						<NavItem
							label="Sign in"
							icon={<LogIn size={20} aria-hidden />}
							onClick={() => signinRedirect()}
						/>
					)}
				</div>
			</nav>
		</>
	);
}

interface NavItemProps {
	icon: ReactNode;
	label: string;
	href?: string;
	onClick?: () => void;
	active?: boolean;
}

function NavItem({ icon, label, href, onClick, active }: NavItemProps) {
	const className = cx(styles.link, active && styles.active);
	const content = (
		<>
			<span className={styles.icon}>{icon}</span>
			<span className={styles.label}>{label}</span>
		</>
	);

	return href ? (
		<Link
			href={href}
			className={className}
			aria-current={active ? "page" : undefined}
		>
			{content}
		</Link>
	) : (
		<button type="button" className={className} onClick={onClick}>
			{content}
		</button>
	);
}
