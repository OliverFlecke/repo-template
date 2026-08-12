import { forwardRef, type ReactNode } from "react";
import { Input, type InputProps } from "@/ui/Input/Input";
import { cx } from "@/ui/util/cx";
import styles from "./AdornedInput.module.css";

export interface AdornedInputProps extends InputProps {
	/** Icon (or other small element) overlaid at the end of the field, e.g. lucide-react icon. */
	icon: ReactNode;
	/** Text shown in a CSS-only popup when the icon is hovered or focused. */
	tooltip?: string;
}

export const AdornedInput = forwardRef<HTMLInputElement, AdornedInputProps>(
	({ icon, tooltip, className, ...props }, ref) => {
		return (
			<span className={styles.wrapper}>
				<Input ref={ref} className={cx(styles.input, className)} {...props} />
				<span className={styles.adornment} tabIndex={tooltip ? 0 : undefined}>
					{icon}
					{tooltip && (
						<span role="tooltip" className={styles.tooltip}>
							{tooltip}
						</span>
					)}
				</span>
			</span>
		);
	},
);

AdornedInput.displayName = "AdornedInput";
