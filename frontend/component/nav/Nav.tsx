"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { type ReactNode, useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { useLogout } from "@/component/auth";
import { getRoles } from "@/component/auth/withRequiredRole";
import { IconButton } from "@/ui/IconButton/IconButton";
import { cx } from "@/ui/util/cx";
import styles from "./Nav.module.css";

const PINNED_KEY = "nav-pinned";

export default function Nav() {
	const pathname = usePathname();
	const { user, signinRedirect } = useAuth();
	const logout = useLogout();
	const isAdmin = getRoles(user).includes("admin");

	const [pinned, setPinned] = useState(false);
	const [mobileOpen, setMobileOpen] = useState(false);

	// Read the persisted preference after mount to avoid an SSR/client mismatch.
	useEffect(() => {
		setPinned(localStorage.getItem(PINNED_KEY) === "true");
	}, []);

	const togglePinned = () => {
		setPinned((prev) => {
			const next = !prev;
			localStorage.setItem(PINNED_KEY, String(next));
			return next;
		});
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
				{mobileOpen ? <IconClose /> : <IconHamburger />}
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
						<span className={styles.brand}>App name</span>
					</span>
					<IconButton
						size="sm"
						className={cx(styles.pinButton, pinned && styles.pinButtonActive)}
						aria-pressed={pinned}
						aria-label={pinned ? "Collapse sidebar" : "Keep sidebar expanded"}
						title={pinned ? "Collapse sidebar" : "Keep sidebar expanded"}
						onClick={togglePinned}
					>
						<IconChevron />
					</IconButton>
				</div>

				<ul className={styles.links}>
					<li>
						<NavItem
							href="/dashboard"
							label="Dashboard"
							icon={<IconGrid />}
							active={pathname === "/dashboard"}
						/>
					</li>
					{isAdmin && (
						<li>
							<NavItem
								href="/admin"
								label="Admin"
								icon={<IconShield />}
								active={pathname === "/admin"}
							/>
						</li>
					)}
				</ul>

				<div className={styles.footer}>
					{user ? (
						<>
							<div className={cx(styles.link, styles.static)}>
								<span className={styles.icon}>
									<IconUser />
								</span>
								<span className={styles.label}>{user.profile.name}</span>
							</div>
							<NavItem label="Log out" icon={<IconLogout />} onClick={logout} />
						</>
					) : (
						<NavItem
							label="Sign in"
							icon={<IconLogin />}
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
		<Link href={href} className={className} title={label}>
			{content}
		</Link>
	) : (
		<button type="button" className={className} title={label} onClick={onClick}>
			{content}
		</button>
	);
}

function IconHamburger() {
	return (
		<svg
			viewBox="0 0 24 24"
			width="20"
			height="20"
			fill="none"
			stroke="currentColor"
			strokeWidth="2"
			strokeLinecap="round"
			aria-hidden="true"
		>
			<line x1="3" y1="6" x2="21" y2="6" />
			<line x1="3" y1="12" x2="21" y2="12" />
			<line x1="3" y1="18" x2="21" y2="18" />
		</svg>
	);
}

function IconClose() {
	return (
		<svg
			viewBox="0 0 24 24"
			width="20"
			height="20"
			fill="none"
			stroke="currentColor"
			strokeWidth="2"
			strokeLinecap="round"
			aria-hidden="true"
		>
			<line x1="6" y1="6" x2="18" y2="18" />
			<line x1="18" y1="6" x2="6" y2="18" />
		</svg>
	);
}

function IconChevron() {
	return (
		<svg
			viewBox="0 0 24 24"
			width="16"
			height="16"
			fill="none"
			stroke="currentColor"
			strokeWidth="2"
			strokeLinecap="round"
			strokeLinejoin="round"
			aria-hidden="true"
		>
			<path d="M9 6l6 6-6 6" />
		</svg>
	);
}

function IconGrid() {
	return (
		<svg
			viewBox="0 0 24 24"
			width="20"
			height="20"
			fill="none"
			stroke="currentColor"
			strokeWidth="2"
			strokeLinejoin="round"
			aria-hidden="true"
		>
			<rect x="3" y="3" width="7" height="7" />
			<rect x="14" y="3" width="7" height="7" />
			<rect x="14" y="14" width="7" height="7" />
			<rect x="3" y="14" width="7" height="7" />
		</svg>
	);
}

function IconShield() {
	return (
		<svg
			viewBox="0 0 24 24"
			width="20"
			height="20"
			fill="none"
			stroke="currentColor"
			strokeWidth="2"
			strokeLinecap="round"
			strokeLinejoin="round"
			aria-hidden="true"
		>
			<path d="M12 3l7 3v6c0 4.5-3 7.5-7 9-4-1.5-7-4.5-7-9V6l7-3z" />
		</svg>
	);
}

function IconUser() {
	return (
		<svg
			viewBox="0 0 24 24"
			width="20"
			height="20"
			fill="none"
			stroke="currentColor"
			strokeWidth="2"
			strokeLinecap="round"
			strokeLinejoin="round"
			aria-hidden="true"
		>
			<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
			<circle cx="12" cy="7" r="4" />
		</svg>
	);
}

function IconLogin() {
	return (
		<svg
			viewBox="0 0 24 24"
			width="20"
			height="20"
			fill="none"
			stroke="currentColor"
			strokeWidth="2"
			strokeLinecap="round"
			strokeLinejoin="round"
			aria-hidden="true"
		>
			<path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" />
			<polyline points="10 17 15 12 10 7" />
			<line x1="15" y1="12" x2="3" y2="12" />
		</svg>
	);
}

function IconLogout() {
	return (
		<svg
			viewBox="0 0 24 24"
			width="20"
			height="20"
			fill="none"
			stroke="currentColor"
			strokeWidth="2"
			strokeLinecap="round"
			strokeLinejoin="round"
			aria-hidden="true"
		>
			<path d="M9 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h4" />
			<polyline points="14 17 19 12 14 7" />
			<line x1="19" y1="12" x2="7" y2="12" />
		</svg>
	);
}
