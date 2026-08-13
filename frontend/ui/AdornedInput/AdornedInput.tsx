import { forwardRef, type ReactNode } from "react";
import { Input, type InputProps } from "@/ui/Input/Input";
import { Tooltip } from "@/ui/Tooltip/Tooltip";
import { cx } from "@/ui/util/cx";
import styles from "./AdornedInput.module.css";

export interface AdornedInputProps extends InputProps {
	/** Icon (or other small element) overlaid at the end of the field, e.g. lucide-react icon. */
	icon: ReactNode;
	/** Text shown in a popup when the icon is hovered or focused. */
	tooltip?: string;
}

export const AdornedInput = forwardRef<HTMLInputElement, AdornedInputProps>(
	({ icon, tooltip, className, ...props }, ref) => {
		const adornment = <span className={styles.adornment}>{icon}</span>;

		return (
			<span className={styles.wrapper}>
				<Input ref={ref} className={cx(styles.input, className)} {...props} />
				{tooltip ? <Tooltip text={tooltip}>{adornment}</Tooltip> : adornment}
			</span>
		);
	},
);

AdornedInput.displayName = "AdornedInput";
