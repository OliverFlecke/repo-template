import Link from "next/link";
import type { ReactNode } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Nav.module.css";

export interface NavItemProps {
	icon: ReactNode;
	label: string;
	href?: string;
	onClick?: () => void;
	active?: boolean;
}

export function NavItem({ icon, label, href, onClick, active }: NavItemProps) {
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
