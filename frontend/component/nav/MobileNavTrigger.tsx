import { Menu as MenuIcon, X } from "lucide-react";
import { IconButton } from "@/ui/IconButton/IconButton";
import styles from "./Nav.module.css";

interface MobileNavTriggerProps {
	open: boolean;
	onToggle: () => void;
}

/** Hamburger button plus the backdrop shown while the mobile overlay is open. */
export function MobileNavTrigger({ open, onToggle }: MobileNavTriggerProps) {
	return (
		<>
			<IconButton
				className={styles.hamburger}
				aria-label={open ? "Close navigation" : "Open navigation"}
				aria-expanded={open}
				aria-controls="app-nav"
				onClick={onToggle}
			>
				{open ? <X aria-hidden /> : <MenuIcon aria-hidden />}
			</IconButton>

			{open && (
				<div
					className={styles.backdrop}
					onClick={onToggle}
					aria-hidden="true"
				/>
			)}
		</>
	);
}
