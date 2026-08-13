import { ListCollapse } from "lucide-react";
import { IconButton } from "@/ui/IconButton/IconButton";
import { cx } from "@/ui/util/cx";
import styles from "./PinToggle.module.css";

interface PinToggleProps {
	pinned: boolean;
	onToggle: () => void;
}

/** Pins the sidebar expanded (vs. only expanding on hover). */
export function PinToggle({ pinned, onToggle }: PinToggleProps) {
	return (
		<IconButton
			size="sm"
			className={cx(styles.pinButton, pinned && styles.active)}
			aria-pressed={pinned}
			aria-label={pinned ? "Collapse sidebar" : "Keep sidebar expanded"}
			title={pinned ? "Collapse sidebar" : "Keep sidebar expanded"}
			onClick={onToggle}
		>
			<ListCollapse size={20} aria-hidden />
		</IconButton>
	);
}
