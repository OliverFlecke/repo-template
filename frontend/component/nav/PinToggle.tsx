import { ChevronRight } from "lucide-react";
import { IconButton } from "@/ui/IconButton/IconButton";
import { cx } from "@/ui/util/cx";
import styles from "./Nav.module.css";

interface PinToggleProps {
	pinned: boolean;
	onToggle: () => void;
}

/** Pins the sidebar expanded (vs. only expanding on hover). */
export function PinToggle({ pinned, onToggle }: PinToggleProps) {
	return (
		<IconButton
			size="sm"
			className={cx(styles.pinButton, pinned && styles.pinButtonActive)}
			aria-pressed={pinned}
			aria-label={pinned ? "Collapse sidebar" : "Keep sidebar expanded"}
			title={pinned ? "Collapse sidebar" : "Keep sidebar expanded"}
			onClick={onToggle}
		>
			<ChevronRight size={16} aria-hidden />
		</IconButton>
	);
}
