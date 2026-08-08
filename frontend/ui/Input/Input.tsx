import { forwardRef, type InputHTMLAttributes } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Input.module.css";

export type InputProps = InputHTMLAttributes<HTMLInputElement>;

export const Input = forwardRef<HTMLInputElement, InputProps>(
	({ className, ...props }, ref) => {
		return (
			<input ref={ref} className={cx(styles.input, className)} {...props} />
		);
	},
);

Input.displayName = "Input";
