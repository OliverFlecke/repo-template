import { forwardRef, type InputHTMLAttributes } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Checkbox.module.css";

export type CheckboxProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type">;

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
	({ className, ...props }, ref) => {
		return (
			<input
				ref={ref}
				type="checkbox"
				className={cx(styles.checkbox, className)}
				{...props}
			/>
		);
	},
);

Checkbox.displayName = "Checkbox";
