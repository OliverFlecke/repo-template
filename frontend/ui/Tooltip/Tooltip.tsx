import {
	type CSSProperties,
	cloneElement,
	isValidElement,
	type ReactElement,
	useId,
} from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Tooltip.module.css";

export interface TooltipProps {
	/** Text shown in the popup. */
	text: string;
	/** The single element the tooltip is anchored to; shown on its hover/focus. */
	children: ReactElement<{
		className?: string;
		style?: CSSProperties;
		tabIndex?: number;
	}>;
}

/** Shows `text` in a popup anchored to `children` on hover/focus, positioned via CSS
 * anchor positioning. Falls back to positioning against the nearest positioned ancestor
 * (see AdornedInput) in browsers that don't support it yet. */
export function Tooltip({ text, children }: TooltipProps) {
	const anchorName = `--tooltip-${useId().replace(/[^a-zA-Z0-9]/g, "")}`;

	const trigger = isValidElement(children)
		? cloneElement(children, {
				className: cx(styles.trigger, children.props.className),
				style: { ...children.props.style, anchorName },
				tabIndex: children.props.tabIndex ?? 0,
			})
		: children;

	return (
		<>
			{trigger}
			<span
				role="tooltip"
				className={styles.tooltip}
				style={{ positionAnchor: anchorName }}
			>
				{text}
				<span className={styles.arrow} aria-hidden="true" />
			</span>
		</>
	);
}
