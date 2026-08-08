import { forwardRef, type InputHTMLAttributes } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Switch.module.css";

export type SwitchProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type">;

export const Switch = forwardRef<HTMLInputElement, SwitchProps>(
	({ className, ...props }, ref) => {
		return (
			<input
				ref={ref}
				type="checkbox"
				className={cx(styles.switch, className)}
				{...props}
			/>
		);
	},
);

Switch.displayName = "Switch";
